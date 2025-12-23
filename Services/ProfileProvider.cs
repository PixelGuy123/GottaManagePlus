using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using GottaManagePlus.Interfaces;
using GottaManagePlus.Models;
using GottaManagePlus.Utils;
using SharpCompress.Common;
using SharpCompress.Writers;

namespace GottaManagePlus.Services;

/// <summary>
/// Manages profiles by interacting with the file system and handling metadata serialization.
/// </summary>
public class ProfileProvider : IProfileProvider
{
    // Constants
    internal const string ProfileFolderName = "profiles",
        MetaDataFileName = "metadata.json",
        ContentZipFileName = "content.zip";
    
    // Private members
    private IGameFolderViewer? _gameFolderViewer;
    public void RegisterViewer(IGameFolderViewer viewer) => _gameFolderViewer = viewer;

    private ProfileItem? _currentProfileItem;
    private readonly List<ProfileItem> _availableProfiles = [];
    
    // Public getters
    protected IGameFolderViewer GameFolderViewer => _gameFolderViewer ?? throw new NullReferenceException("GameFolderViewer is null."); 
    
    
    // Public implementation
    /// <summary>
    /// Adds a profile to the internal list if a profile with the same name does not already exist.
    /// </summary>
    /// <inheritdoc/>
    public async Task<bool> AddProfile(ProfileItemMetaData profileItem, IProgress<double>? progress = null)
    {
        // Check if there's already a profile with same name
        if (_availableProfiles.Exists(p =>
                p.ProfileName.Equals(profileItem.ProfileName, StringComparison.OrdinalIgnoreCase)))
            return false;
        
        // Add profile and update data
        await GenerateProfileFromProfileItem(profileItem.ToProfileItem(_availableProfiles, GameFolderViewer), 
            generateNewContentStorage: true, progress); // Generate active profile
        return await SetActiveProfile(_availableProfiles.Count - 1, progress); // Set as active profile to copy the data properly from the game
    }
    
    /// <summary>
    /// Deletes the profile folder and removes the item from the list.
    /// </summary>
    /// <inheritdoc/>
    /// <exception cref="UnauthorizedAccessException">Thrown if the profile path is outside the manager root.</exception>
    public async Task<bool> DeleteProfile(int profileIndex, IProgress<double>? progress = null)
    {
        if (profileIndex < 0 || profileIndex >= _availableProfiles.Count)
            throw new IndexOutOfRangeException($"profileIndex ({profileIndex}) is out of range.");

        var profile = _availableProfiles[profileIndex];
        if (string.IsNullOrEmpty(profile.FullOsPath) || !Directory.Exists(profile.FullOsPath))
        {
            Debug.WriteLine("Profile has an invalid path.", Constants.DebugError);
            return false;
        }

        try
        {
            if (!FileUtils.IsWithinManagerRootDirectory(profile.FullOsPath, GameFolderViewer))
                throw new UnauthorizedAccessException("Profiles folder is located outside the expected location!");
            
            // Delete profile folder
            Directory.Delete(profile.FullOsPath, true);
            
            // Update profile data
            await UpdateProfilesData(progress);

            return true;
        }
        catch (Exception e)
        {
            Debug.WriteLine("Failed to delete profile folder!", Constants.DebugError);
            Debug.WriteLine(e.ToString(), Constants.DebugError);
            return false;
        }
    }
    /// <summary>
    /// Returns the internal list of profiles as a read-only collection.
    /// </summary>
    /// <inheritdoc/>
    public IReadOnlyCollection<ProfileItem> GetLoadedProfiles() => _availableProfiles;

    /// <summary>
    /// Clears current data and re-populates the profile list from the "profiles" directory.
    /// </summary>
    /// <inheritdoc/>
    public async Task UpdateProfilesData(IProgress<double>? progress = null)
    {
        // Get the profile
        var profilesFolder = GetOrCreateProfilesFolder();

        if (profilesFolder == null)
        {
            Debug.WriteLine("Failed to update profiles data. Directory is null.", Constants.DebugWarning);
            return;
        }

        // Reset the profiles
        _availableProfiles.Clear();
        
        // Search up them again
        foreach (var profileDir in profilesFolder.GetDirectories())
        {
            // Get files from directory
            var files = profileDir.GetFiles();
            // identify the meta data file
            var metaDataJson = files.FirstOrDefault(file => file.Name == MetaDataFileName);
            if (metaDataJson == null)
            {
                Debug.WriteLine($"Metadata not found in folder: {profileDir.Name}", Constants.DebugWarning);
                continue;
            }
            
            // Attempt to read meta data
            try
            {
                var metaData = JsonSerializer.Deserialize<ProfileItemMetaData>(await File.ReadAllTextAsync(metaDataJson.FullName));
                if (metaData == null)
                {
                    Debug.WriteLine("Failed to deserialize MetaData file due to an unknown error!", Constants.DebugError);
                    continue;
                }

                var profileItem = metaData.ToProfileItem(_availableProfiles, GameFolderViewer); // Parse as profile item
                await GenerateProfileFromProfileItem(profileItem, generateNewContentStorage: false); // Generate profile with no storage changes
            }
            catch (Exception e)
            {
                Debug.WriteLine("Failed to deserialize MetaData file!", Constants.DebugError);
                Debug.WriteLine(e.ToString(), Constants.DebugError);
            }
        }

        // If empty, leave a default profile in
        if (_availableProfiles.Count == 0)
        {
            var defaultProfile = ProfileItem.Default;
            _availableProfiles.Add(defaultProfile);
            await GenerateProfileFromProfileItem(defaultProfile, generateNewContentStorage: true, progress: progress);
        }

        // Update active profile
        if (_currentProfileItem == null)
            await SetActiveProfile(0, progress);
        else // If the profile exists, try to still set the same profile active; otherwise, the first default
            await SetActiveProfile(
                _currentProfileItem.Id < _availableProfiles.Count - 1 ? _currentProfileItem.Id : 0, 
                progress);
    }

    /// <summary>
    /// Gets the current profile item.
    /// </summary>
    /// <inheritdoc/>
    public ProfileItem GetActiveProfile() => _currentProfileItem ?? throw new NullReferenceException("Current Profile is null!");

    /// <summary>
    /// Sets the active profile and triggers the <see cref="OnProfilesUpdate"/> event.
    /// </summary>
    /// <inheritdoc/>
    public async Task<bool> SetActiveProfile(int profileIndex, IProgress<double>? progress = null)
    {
        // Previous profile
        _currentProfileItem?.IsSelectedProfile = false;
        
        // New Profile
        _currentProfileItem = _availableProfiles[profileIndex];
        _currentProfileItem.IsSelectedProfile = true;
        
        // Invoke profiles update
        OnProfilesUpdate?.Invoke(this);
        return await UnzipAndDistributeProfileContent(_currentProfileItem, progress);
    }
    
    /// <summary>
    /// Saves the current active profile.
    /// </summary>
    /// <inheritdoc/>
    public async Task<bool> SaveActiveProfile(IProgress<double>? progress = null) => await GenerateProfileFromProfileItem(GetActiveProfile(), true, progress);
    

    // Public events
    /// <inheritdoc/>
    public event Action<IProfileProvider>? OnProfilesUpdate;
    

    // Private methods
    private DirectoryInfo? GetOrCreateProfilesFolder() // Create the profiles folder
    {
        try
        {
            return Directory.CreateDirectory(
                GameFolderViewer.SearchPath(
                    GameFolderViewer.GetPathFrom(IGameFolderViewer.CommonDirectory.ManagerRoot),
                    ProfileFolderName
                    )
                );
        }
        catch (Exception e)
        {
            Debug.WriteLine("Failed to create or get the Profiles directory!", Constants.DebugError);
            Debug.WriteLine(e.ToString(), Constants.DebugError);
            return null;
        }
    }

    // As says, literally generate a profile from the profile item
    private async Task<bool> GenerateProfileFromProfileItem(
        ProfileItem profileItem, 
        bool generateNewContentStorage = false,
        IProgress<double>? progress = null)
    {
        // Generate folder path
        var folderPath = GameFolderViewer.SearchPath(
            GameFolderViewer.GetPathFrom(IGameFolderViewer.CommonDirectory.ManagerRoot),
            ProfileFolderName,
            profileItem.ProfileName
        );
        // Try to generate profile info
        try
        {
            // Try to get profile (and create the profile folder)
            var profileDir = Directory.CreateDirectory(folderPath);

            // Update ProfileItem metadata
            profileItem.FullOsPath = folderPath;
            profileItem.RelativeOsPath = Path.GetRelativePath(GameFolderViewer.GetGameRootPath(), folderPath);
            
            profileItem.DateOfCreation = profileDir.CreationTime;
            profileItem.DateOfUpdate = DateTime.Now; // Get date from system, since things were updated

            // Get meta data
            var profileItemMetaData = profileItem.ToMetaData();

            // If no new content is required, no writing is either.
            if (!generateNewContentStorage) return true;

            // Get the zip file path
            var zipPath = GameFolderViewer.SearchPath(folderPath, ContentZipFileName);
            var success = false;

            try
            {
                // Attempts to write all the serialized info into the JSON file
                await File.WriteAllTextAsync(
                    GameFolderViewer.SearchPath(folderPath, MetaDataFileName),
                    JsonSerializer.Serialize(profileItemMetaData));

                // ** Write all the content inside the game folder to the profile
                // Get the zip file ready
                await using var zipArchiveHandler =
                    File.OpenWrite(zipPath);
                using var zipWriter = WriterFactory.Open(zipArchiveHandler, ArchiveType.Zip, CompressionType.LZMA);

                var rootPath = GameFolderViewer.GetGameRootPath();
                
                // Gather lists into their local variables
                var configs = profileItem.ConfigsMetaDataList;
                var patchers = profileItem.PatchersMetaDataList;
                var mods = profileItem.ModMetaDataList;
                
                // TODO: Once mod is properly implemented, the code should:
                // Calculate the amount of mods and all the other folders that are required by the mods
                // to be added to totalFiles and be used inside the final for loop from this method.

                var totalFiles = configs.Count + patchers.Count + mods.Count; // Calculate total
                
                // Look for configs
                for (var i = 0; i < configs.Count; i++)
                {
                    var config = configs[i];
                    var fullPath = config.FullOsPath;

                    if (string.IsNullOrEmpty(fullPath) || !File.Exists(fullPath)) continue; // Skip this file

                    var entryPathInZip = Path.GetRelativePath(rootPath, fullPath);

                    // Read file stream
                    await using var sourceFileStream = File.OpenRead(fullPath);

                    // Write file to the zip
                    await zipWriter.WriteAsync(entryPathInZip, sourceFileStream);

                    // Calculate percentage
                    var percentage = (double)(i + 1) / totalFiles;
                    progress?.Report(percentage);
                }

                totalFiles -= configs.Count; // Remove the configs from the table
                
                // Look for patchers
                for (var i = 0; i < patchers.Count; i++)
                {
                    var patcher = patchers[i];
                    var fullPath = patcher.FullOsPath;

                    if (string.IsNullOrEmpty(fullPath) || !File.Exists(fullPath)) continue; // Skip this file

                    var entryPathInZip = Path.GetRelativePath(rootPath, fullPath);

                    // Read file stream
                    await using var sourceFileStream = File.OpenRead(fullPath);

                    // Write file to the zip
                    await zipWriter.WriteAsync(entryPathInZip, sourceFileStream);

                    // Calculate percentage
                    var percentage = (double)(i + 1) / totalFiles;
                    progress?.Report(percentage);
                }
                
                totalFiles -= patchers.Count; // Remove the patchers from the table
                
                // Look for mods
                for (var i = 0; i < mods.Count; i++)
                {
                    var mod = mods[i];
                    var fullPath = mod.FullOsPath;

                    if (string.IsNullOrEmpty(fullPath) || !File.Exists(fullPath)) continue; // Skip this file

                    var entryPathInZip = Path.GetRelativePath(rootPath, fullPath);

                    // Read file stream
                    await using var sourceFileStream = File.OpenRead(fullPath);

                    // Write file to the zip
                    await zipWriter.WriteAsync(entryPathInZip, sourceFileStream);

                    // Calculate percentage
                    var percentage = (double)(i + 1) / totalFiles;
                    progress?.Report(percentage);
                }
                
                // 100% report
                progress?.Report(1.0);
                success = true;
            }
            catch (OperationCanceledException)
            {
                Debug.WriteLine("Profile generation was cancelled by the user.", Constants.DebugWarning);
                throw;
            }
            catch (Exception e)
            {
                Debug.WriteLine("Failed to create the profile content.", Constants.DebugError);
                Debug.WriteLine(e.ToString(), Constants.DebugError);
            }
            finally
            {
                // If the zip wasn't made successfully, delete it
                if (!success && File.Exists(zipPath))
                {
                    try
                    {
                        File.Delete(zipPath);
                    }
                    catch (IOException e)
                    {
                        Debug.WriteLine($"Could not delete partial ZIP.", Constants.DebugError);
                        Debug.WriteLine(e.ToString(), Constants.DebugError);
                    }
                }
            }
            return success;
        }
        catch (Exception e)
        {
            Debug.WriteLine($"Failed to create a profile directory to \"{profileItem.ProfileName}\"", Constants.DebugError);
            Debug.WriteLine(e.ToString(), Constants.DebugError);
        }

        return false;
    }

    private async Task<bool> UnzipAndDistributeProfileContent(
        ProfileItem profileItem, 
        IProgress<double>? progress = null)
    {
        throw new NotImplementedException();
    }
}