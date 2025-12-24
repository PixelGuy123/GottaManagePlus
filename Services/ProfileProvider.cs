using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using GottaManagePlus.Interfaces;
using GottaManagePlus.Models;
using GottaManagePlus.Utils;
using SharpCompress.Archives;
using SharpCompress.Common;
using SharpCompress.Writers;

// TODO: Generate a metadata file with the same name as the zip (myProfile.zip.json) to keep record of all the files that have been stored and at what categories:
// For example, the JSON should have a "Configs" list, and all the file paths used inside the configs folder (NOT TO BE USED FOR EXTRACTION, ONLY FOR REFERENCE).
// if the ProfileItem has IsSelectedProfile on, it is literally being used to identify the path; otherwise, the system should use it to know if it's just a readonly instance with metadata contained.

namespace GottaManagePlus.Services;

/// <summary>
/// Manages profiles by interacting with the file system and handling metadata serialization.
/// </summary>
public class ProfileProvider(PlusFolderViewer viewer) : IProfileProvider
{
    // Constants
    internal const string ProfileFolderName = "profiles",
        TempContentZipFileSuffix = "_TEMP",
        ContentZipFileExtension = ".zip";
    
    // Private members
    private ProfileItem? _currentProfileItem;
    private ProfileItem? _lastUnzippedProfileItem;
    private readonly List<ProfileItem> _availableProfiles = [];
    private readonly IGameFolderViewer _gameFolderViewer = viewer;
    
    // Public getters
    protected IGameFolderViewer GameFolderViewer => _gameFolderViewer ?? throw new NullReferenceException("GameFolderViewer is null.");


    // Public implementation
    /// <summary>
    /// Adds a profile to the internal list if a profile with the same name does not already exist.
    /// </summary>
    /// <inheritdoc/>
    public async Task<bool> AddProfile(string profileName, IProgress<double>? progress = null)
    {
        // Check if there's already a profile with same name
        if (_availableProfiles.Exists(p =>
                p.ProfileName.Equals(profileName, StringComparison.OrdinalIgnoreCase)))
            return false;
        
        // Create new profile item instance
        var profileItem = new ProfileItem(
            _availableProfiles.Count,
            profileName
        );

        // Add profile to the list
        _availableProfiles.Add(profileItem);
        
        // Add profile and update data
        await GenerateProfileDataFromProfileItem(profileItem, 
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
        if (string.IsNullOrEmpty(profile.FullOsPath) || !File.Exists(profile.FullOsPath))
        {
            Debug.WriteLine("Profile has an invalid path.", Constants.DebugError);
            return false;
        }

        try
        {
            if (!FileUtils.IsWithinManagerRootDirectory(profile.FullOsPath, GameFolderViewer))
                throw new UnauthorizedAccessException("Profile path is located outside the expected location!");
            
            // Delete profile folder
            File.Delete(profile.FullOsPath);
            
            // Update profile data
            await UpdateProfilesData(progress: progress);

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
    public IReadOnlyList<ProfileItem> GetLoadedProfiles() => _availableProfiles;

    /// <summary>
    /// Clears current data and re-populates the profile list from the "profiles" directory.
    /// </summary>
    /// <inheritdoc/>
    public async Task<bool> UpdateProfilesData(int defaultSelection = -1, IProgress<double>? progress = null)
    {
        // Get the profile
        var profilesFolder = GetOrCreateProfilesFolder();

        if (profilesFolder == null)
        {
            Debug.WriteLine("Failed to update profiles data. Directory is null.", Constants.DebugWarning);
            return false;
        }

        // Reset the profiles
        _availableProfiles.Clear();
        
        // Search up them again
        foreach (var profileZip in profilesFolder
                                            .GetFiles()
                                            .OrderBy(p => p.Name)) // Ordered search
        {
            var profileZipName = Path.GetFileNameWithoutExtension(profileZip.FullName);
            
            var profileItem = new ProfileItem(_availableProfiles.Count, profileZipName);
            _availableProfiles.Add(profileItem);
            await GenerateProfileDataFromProfileItem(profileItem, generateNewContentStorage: false); // Generate profile with no storage changes
        }

        // If empty, leave a default profile in
        if (_availableProfiles.Count == 0)
        {
            var defaultProfile = ProfileItem.Default;
            _availableProfiles.Add(defaultProfile);
            if (!await GenerateProfileDataFromProfileItem(defaultProfile, generateNewContentStorage: true,
                    progress: progress))
            {
                Debug.WriteLine("The default profile failed to be generated!", Constants.DebugError);
                return false;
            }
        }

        // If the profile exists, try to still set the same profile active; otherwise, the first default
        if (_currentProfileItem != null)
            return await SetActiveProfile(
                _currentProfileItem.Id < _availableProfiles.Count ? _currentProfileItem.Id : 0,
                progress);
        
        // Update active profile
        // Get the index
        var defaultIndex = defaultSelection < 0 || defaultSelection >= _availableProfiles.Count ? 
            0 : defaultSelection;
        return await SetActiveProfile(defaultIndex, progress);
    }

    /// <summary>
    /// Gets the current profile item index.
    /// </summary>
    /// <inheritdoc/>
    public int GetActiveProfile() => _availableProfiles.IndexOf(_currentProfileItem ?? throw new NullReferenceException("Current Profile is null!"));

    /// <summary>
    /// Sets the active profile and triggers the <see cref="OnProfilesUpdate"/> event.
    /// </summary>
    /// <inheritdoc/>
    public async Task<bool> SetActiveProfile(int profileIndex, IProgress<double>? progress = null)
    {
        // Previous profile
        if (_currentProfileItem != null)
            _currentProfileItem.IsSelectedProfile = false;
        
        // New Profile
        _currentProfileItem = _availableProfiles[profileIndex];
        _currentProfileItem.IsSelectedProfile = true;
        
        // Invoke profiles update
        OnProfilesUpdate?.Invoke(this);
        return await UnzipAndDistributeProfileContent(_currentProfileItem!, progress);
    }

    /// <summary>
    /// Saves the current active profile.
    /// </summary>
    /// <inheritdoc/>
    public async Task<bool> SaveActiveProfile(IProgress<double>? progress = null) => await GenerateProfileDataFromProfileItem(this.GetInstanceActiveProfile(), true, progress);


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

    private bool GatherProfileFolderInformation(ProfileItem item)
    {
        // Record snapshots if it happens to fail
        var tempConfigs = new List<ItemWithPath>(item.ConfigsMetaDataList);
        var tempPatchers = new List<ItemWithPath>(item.PatchersMetaDataList);
        var tempMods = new List<ModItem>(item.ModMetaDataList);
        
        // Clear up the lists to be updated
        item.ConfigsMetaDataList.Clear();
        item.PatchersMetaDataList.Clear();
        item.ModMetaDataList.Clear();

        try
        {
            // Get game root path
            var rootPath = GameFolderViewer.GetGameRootPath();
            // Get the BepInEx path
            var bepInExPath = GameFolderViewer.GetPathFrom(IGameFolderViewer.CommonDirectory.BepInEx, true);

            // Configs loading
            var configsPath = GameFolderViewer.SearchPath(bepInExPath, "config");
            if (Directory.Exists(configsPath))
            {
                foreach (var config in Directory.GetFiles(configsPath))
                    item.ConfigsMetaDataList.Add(new ItemWithPath(item.ConfigsMetaDataList.Count)
                    {
                        FullOsPath = config,
                        RelativeOsPath = Path.GetRelativePath(rootPath, config)
                    });
            }

            // Patchers loading
            var patchersPath = GameFolderViewer.SearchPath(bepInExPath, "patchers");
            if (Directory.Exists(patchersPath))
            {
                foreach (var patcher in Directory.GetFiles(patchersPath))
                    item.PatchersMetaDataList.Add(new ItemWithPath(item.PatchersMetaDataList.Count)
                    {
                        FullOsPath = patcher,
                        RelativeOsPath = Path.GetRelativePath(rootPath, patcher)
                    });
            }

            // Mods loading
            var modsPath = GameFolderViewer.SearchPath(bepInExPath, "plugins");
            if (Directory.Exists(configsPath))
            {
                // TODO: Scan for mod meta data, since they have some special access inside the game
            }

            return true;
        }
        catch (Exception ex)
        {
            Debug.WriteLine("Failed to gather information for the ProfileItem!", Constants.DebugError);
            Debug.WriteLine(ex.ToString(), Constants.DebugError);
            
            // Restart their states
            item.ConfigsMetaDataList = new ObservableCollection<ItemWithPath>(tempConfigs);
            item.PatchersMetaDataList = new ObservableCollection<ItemWithPath>(tempPatchers);
            item.ModMetaDataList = new ObservableCollection<ModItem>(tempMods);
            return false;
        }
    }

    // As says, literally generate a profile from the profile item
    private async Task<bool> GenerateProfileDataFromProfileItem(
        ProfileItem profileItem, 
        bool generateNewContentStorage = false,
        IProgress<double>? progress = null)
    {
        if (!GatherProfileFolderInformation(profileItem))
            return false;
        
        // Generate folder path
        var profilesFolderPath = GameFolderViewer.SearchPath(
            GameFolderViewer.GetPathFrom(IGameFolderViewer.CommonDirectory.ManagerRoot, true),
            ProfileFolderName
        );
        // Get the zip file path
        var zipPath = GameFolderViewer.SearchPath(profilesFolderPath, profileItem.ProfileName + ContentZipFileExtension);
        
        // Try to generate profile info
        try
        {
            // Update ProfileItem metadata
            profileItem.FullOsPath = zipPath;
            profileItem.RelativeOsPath = Path.GetRelativePath(GameFolderViewer.GetGameRootPath(), zipPath);
            
            profileItem.DateOfCreation = File.GetCreationTime(zipPath);
            profileItem.DateOfUpdate = DateTime.Now; // Get date from system, since things were updated

            // If no new content is required, no writing is either.
            if (!generateNewContentStorage) return true;
            
            // Redo zip path
            var tempZipPath = GameFolderViewer.SearchPath(profilesFolderPath, profileItem.ProfileName + TempContentZipFileSuffix + ContentZipFileExtension);
            
            var success = false;
            try
            {
                // ** Write all the content inside the game folder to the profile
                // Get the zip file ready
                await using (var zipArchiveHandler =
                             File.OpenWrite(tempZipPath))
                {

                    // Create a writer
                    using var zipWriter = WriterFactory.Open(zipArchiveHandler, ArchiveType.Zip, CompressionType.LZMA);

                    // Get root path from game
                    var rootPath = GameFolderViewer.GetGameRootPath();

                    // Gather lists into their local variables
                    var configs = profileItem.ConfigsMetaDataList;
                    var patchers = profileItem.PatchersMetaDataList;
                    var mods = profileItem.ModMetaDataList;

                    // TODO: Once mod is properly implemented, the code should:
                    // Calculate the amount of mods and all the other folders that are required by the mods
                    // to be added to totalFiles and be used inside the final for loop from this method.

                    // TODO: Generate the new zip file in a temporary folder (granted by Environment)
                    // to delete the zip file if left partially completed.

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
                    
                }

                // Copy the temp one to have a different name (overwrite)
                File.Copy(tempZipPath, zipPath, true);
                
                // Delete the temporary zip file
                File.Delete(tempZipPath);
                
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
                if (!success && File.Exists(tempZipPath))
                {
                    try
                    {
                        File.Delete(tempZipPath);
                    }
                    catch (IOException e)
                    {
                        Debug.WriteLine("Could not delete partial ZIP.", Constants.DebugError);
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
        // Don't allow unzipping the same profile item twice for performance reasons
        if (_lastUnzippedProfileItem?.ProfileName == profileItem.ProfileName)
        {
            await GenerateProfileDataFromProfileItem(profileItem);
            Debug.WriteLine($"Skipped unzipping {profileItem.ProfileName}.", Constants.DebugInfo);
            return true;
        }

        // Update last unzipped profile
        _lastUnzippedProfileItem = profileItem;

        // Get the zip path and root path
        var zipPath = profileItem.FullOsPath;
        var rootPath = GameFolderViewer.GetGameRootPath();

        if (string.IsNullOrEmpty(zipPath) || !File.Exists(zipPath)) // If not found, just skip
        {
            Debug.WriteLine($"Zip file not found at: {zipPath}", Constants.DebugError);
            return false;
        }

        try
        {
            await using var stream = File.OpenRead(zipPath);
            using var archive = ArchiveFactory.Open(stream);
            
            var entriesProcessed = 0;
            var totalEntryCount = archive.Entries.Count();
            List<string> deletedDirectories = [];
            
            // To provide accurate progress, we must count entries first if the reader allows, 
            // or use a generic estimation. Here we use an iterative approach.
            foreach (var entry in archive.Entries)
            {
                if (entry.IsDirectory || string.IsNullOrEmpty(entry.Key)) continue;

                var extractionPath = GameFolderViewer.SearchPath(entry.Key);
                var directory = Path.GetDirectoryName(extractionPath);

                // Skip if invalid directory
                if (string.IsNullOrEmpty(directory)) continue;

                // Create directory in the space
                if (!deletedDirectories.Exists(p => p.StartsWith(directory))) // Avoid deleting subdirectories from other directories
                {
                    // Aggressively remove the directory, to be re-added again
                    if (Directory.Exists(directory))
                        Directory.Delete(directory, true);
                    // Clean up and add back
                    Directory.CreateDirectory(directory);
                    
                    deletedDirectories.Add(directory); // Register directory
                }
                
                // DO not use async method; it bugs out here for some reason, differently from its sync variant.
                // ReSharper disable once MethodHasAsyncOverload
                entry.WriteToDirectory(rootPath, new ExtractionOptions
                {
                    ExtractFullPath = true, Overwrite = true, PreserveAttributes = false, PreserveFileTime = false
                });
                
                // Progress update
                entriesProcessed++;
                progress?.Report((double)entriesProcessed / totalEntryCount);
            }
            
            _lastUnzippedProfileItem = profileItem;
            progress?.Report(1.0);
            Debug.WriteLine($"Successfully unzipped {profileItem.ProfileName} to {rootPath}.", Constants.DebugInfo);
            return true;
        }
        catch (Exception e)
        {
            Debug.WriteLine($"Failed to unzip profile: {profileItem.ProfileName}", Constants.DebugError);
            Debug.WriteLine(e.ToString(), Constants.DebugError);
            return false;
        }
    }
}