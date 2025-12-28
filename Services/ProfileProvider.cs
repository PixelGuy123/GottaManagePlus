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

namespace GottaManagePlus.Services;

/// <summary>
/// Manages profiles by interacting with the file system and handling metadata serialization.
/// </summary>
public class ProfileProvider(PlusFolderViewer viewer) : IProfileProvider
{
    // Constants
    private const string ProfileFolderName = "profiles",
        TempContentZipFileSuffix = "_TEMP",
        ContentZipFileExtension = ".zip",
        ContentMetadataFileExtension = ".metadata";
    
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
    public async Task<bool> AddProfile(string profileName, bool deleteExistingStorage, IProgress<(int, int, string?)>? progress = null)
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
            generateNewContentStorage: true, 
            deleteExistingStorage: deleteExistingStorage, 
            progress); 
        return await SetActiveProfile(_availableProfiles.Count - 1, progress); // Set as active profile to copy the data properly from the game
    }
    
    /// <summary>
    /// Deletes the profile folder and removes the item from the list.
    /// </summary>
    /// <inheritdoc/>
    /// <exception cref="UnauthorizedAccessException">Thrown if the profile path is outside the manager root.</exception>
    public async Task<bool> DeleteProfile(int profileIndex, IProgress<(int, int, string?)>? progress = null)
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
            if (File.Exists(profile.FullOsPath))
                File.Delete(profile.FullOsPath);
            
            // Delete profile metadata
            var metadataPath = profile.FullOsPath + ContentMetadataFileExtension;
            if (File.Exists(metadataPath))
                File.Delete(metadataPath);
            
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
    /// Exports a profile in the default format within the manager's folder.
    /// </summary>
    /// <inheritdoc/>
    public async Task<bool> ExportProfile(int profileIndex)
    {
        var success = false;
        var profileExportTempPath = string.Empty;
        try
        {
            // Get the folder to export
            var exportFolder = GameFolderViewer.SearchPath(
                GameFolderViewer.GetPathFrom(IGameFolderViewer.CommonDirectory.ManagerRoot),
                Constants.ProfileExportFolder
            );

            // Get profile instance
            var profile = _availableProfiles[profileIndex];

            // Safe checks
            if (string.IsNullOrEmpty(profile.FullOsPath) || !File.Exists(profile.FullOsPath) ||
                profile.IsProfileMissingMetadata)
                throw new InvalidOperationException("Profile CANNOT be exported.");

            // Create directory if it doesn't exist
            if (!Directory.Exists(exportFolder))
                Directory.CreateDirectory(exportFolder);
            
            // Set the temp path
            profileExportTempPath = Path.Combine(exportFolder, profile.ProfileName + TempContentZipFileSuffix + Constants.ExportedProfileExtension);
            
            // Open the zip writer
            await using (var zipArchiveHandler = File.OpenWrite(profileExportTempPath))
            {
                // Get the zip writer for the task
                using (var writer = WriterFactory.Open(zipArchiveHandler, ArchiveType.Zip, CompressionType.BZip2))
                {
                    // Get the streams for writing
                    await using var metadataStream = File.OpenRead(profile.FullOsPath + ContentMetadataFileExtension);
                    await using var contentStream = File.OpenRead(profile.FullOsPath);

                    // Write the files to the zip
                    writer.Write(Path.GetFileName(contentStream.Name), contentStream);
                    writer.Write(Path.GetFileName(metadataStream.Name), metadataStream);
                }
            }

            // Copy the temp one to have a different name (overwrite)
            File.Copy(profileExportTempPath, Path.Combine(exportFolder, profile.ProfileName + Constants.ExportedProfileExtension), true);
            
            // Delete the temporary zip file
            File.Delete(profileExportTempPath);

            success = true;
            return true;
        }
        catch (Exception e)
        {
            success = false;
            Debug.WriteLine($"Failed to export {_availableProfiles[profileIndex].ProfileName} to {profileExportTempPath}.", Constants.DebugError);
            Debug.WriteLine(e.ToString(), Constants.DebugError);
            return false;
        }
        finally // Finally, if export wasn't successful, delete the temporary file
        {
            try
            {
                if (!success && File.Exists(profileExportTempPath))
                    File.Delete(profileExportTempPath);
            }
            catch (Exception e)
            {
                Debug.WriteLine($"Failed to delete temporary export file: {profileExportTempPath}.", Constants.DebugError);
                Debug.WriteLine(e.ToString(), Constants.DebugError);
            }
        }
    }

    /// <summary>
    /// Imports a profile in the default format within the manager's folder.
    /// </summary>
    /// <inheritdoc/>
    public async Task<bool> ImportProfile(string importPath)
    {
        var tempFiles = new List<string>();
        try
        {
            if (!File.Exists(importPath))
            {
                Debug.WriteLine($"Import file not found: {importPath}", Constants.DebugError);
                return false;
            }

            var profilesFolder = GetOrCreateProfilesFolder();
            if (profilesFolder == null)
                throw new NullReferenceException(nameof(profilesFolder));
            
            await using var stream = File.OpenRead(importPath);
            using var archive = ArchiveFactory.Open(stream);
            
            // Extract to temporary files
            foreach (var entry in archive.Entries)
            {
                if (entry.IsDirectory || string.IsNullOrEmpty(entry.Key)) continue;

                var fileName = Path.GetFileName(entry.Key);
                var finalPath = Path.Combine(profilesFolder.FullName, fileName);
                var tempPath = finalPath + TempContentZipFileSuffix;

                // Extract
                entry.WriteToFile(tempPath, new ExtractionOptions { ExtractFullPath = false, Overwrite = true });
                tempFiles.Add(tempPath);
            }

            // Check for files
            if (tempFiles.Count == 0)
            {
                Debug.WriteLine("No files found in the imported archive.", Constants.DebugError);
                return false;
            }

            // Check for conflicts and prepare for move
            var finalFiles = new List<string>();
            foreach (var tempFile in tempFiles)
            {
                var originalFinalPath = tempFile.Substring(0, tempFile.Length - TempContentZipFileSuffix.Length);
                var finalPath = originalFinalPath;
                var counter = 1;
                
                // Basically make a unique file name
                while (File.Exists(finalPath))
                {
                    counter++;
                    var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(originalFinalPath);
                    var extension = Path.GetExtension(originalFinalPath);
                    finalPath = Path.Combine(Path.GetDirectoryName(originalFinalPath)!, $"{fileNameWithoutExtension}_{counter}{extension}");
                }
                finalFiles.Add(finalPath);
            }


            // Move temp files to final destination
            for (var i = 0; i < tempFiles.Count; i++)
                File.Move(tempFiles[i], finalFiles[i], true);

            // Update profiles data
            await UpdateProfilesData();
            
            return true;
        }
        catch (Exception e)
        {
            Debug.WriteLine($"Failed to import profile from {importPath}.", Constants.DebugError);
            Debug.WriteLine(e.ToString(), Constants.DebugError);
            return false;
        }
        finally
        {
            // Cleanup temp files
            foreach (var tempFile in tempFiles)
            {
                try
                {
                    if (File.Exists(tempFile))
                        File.Delete(tempFile);
                }
                catch (Exception e)
                {
                    Debug.WriteLine($"Failed to delete temporary import file: {tempFile}.", Constants.DebugError);
                    Debug.WriteLine(e.ToString(), Constants.DebugError);
                }
            }
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
    public async Task<bool> UpdateProfilesData(int defaultSelection = -1, IProgress<(int, int, string?)>? progress = null)
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
                                            .Where(p => p.Extension == ContentZipFileExtension)
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
    public async Task<bool> SetActiveProfile(int profileIndex, IProgress<(int, int, string?)>? progress = null)
    {
        var noPrevProfileDetected = false;
        // Previous profile
        if (_currentProfileItem != null)
            _currentProfileItem.IsSelectedProfile = false;
        else
            noPrevProfileDetected = true;
        
        // New Profile
        _currentProfileItem = _availableProfiles[profileIndex];
        _currentProfileItem.IsSelectedProfile = true;
        
        // No previous profile detected means this is a first boot
        if (noPrevProfileDetected)
            await GenerateProfileDataFromProfileItem(_currentProfileItem, generateNewContentStorage: false); // Generate profile with no storage changes
        
        // Invoke profiles update
        OnProfilesUpdate?.Invoke(this);
        return await UnzipAndDistributeProfileContent(_currentProfileItem!, progress);
    }

    /// <summary>
    /// Saves the current active profile.
    /// </summary>
    /// <inheritdoc/>
    public async Task<bool> SaveActiveProfile(IProgress<(int, int, string?)>? progress = null) => await GenerateProfileDataFromProfileItem(this.GetInstanceActiveProfile(), true,  progress: progress);


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

    private void RemoveProfileStorageReference()
    {
        // Delete the common folders
        DeleteAndRegenerateFolder(GameFolderViewer.SearchPath(
            GameFolderViewer.GetPathFrom(IGameFolderViewer.CommonDirectory.BepInEx),
            Constants.ConfigFolder));
        DeleteAndRegenerateFolder(GameFolderViewer.SearchPath(
            GameFolderViewer.GetPathFrom(IGameFolderViewer.CommonDirectory.BepInEx),
            Constants.PatchersFolder));
        DeleteAndRegenerateFolder(GameFolderViewer.SearchPath(
            GameFolderViewer.GetPathFrom(IGameFolderViewer.CommonDirectory.BepInEx),
            Constants.PluginsFolder));
        return;


        static void DeleteAndRegenerateFolder(string path)
        {
            // Search configs folder and delete it to be regenerated
            if (!Directory.Exists(path)) return;
            
            Directory.Delete(path, true);
            Directory.CreateDirectory(path);
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
            // If the profile item is not the chosen one, use metadata file instead.
            if (!item.IsSelectedProfile)
            {
                var path = item.FullOsPath + ContentMetadataFileExtension;
                // If the path itself for the metadata exists, we can use it.
                if (File.Exists(path))
                {
                    // Declare the missing metadata is false
                    item.IsProfileMissingMetadata = false;
                    
                    // Get binary reader
                    Dictionary<string, string[]>? profileMetaData;
                    using (var reader = new BinaryReader(File.OpenRead(path)))
                    {
                        profileMetaData = ProfileMetadataBinaryUtils.ReadDirectoryStructure(reader);
                    }

                    if (profileMetaData == null)
                        throw new NullReferenceException(nameof(profileMetaData));
                    
                    // Configs loading
                    if (profileMetaData.TryGetValue(ProfileMetadataBinaryUtils.ConfigsPrefix, out var collection))
                    {
                        for (var i = 0; i < collection.Length; i++)
                        {
                            item.ConfigsMetaDataList.Add(new ItemWithPath(i)
                            {
                                FullOsPath = collection[i]
                            });
                        }
                    }
                        
                    // Patchers loading
                    if (profileMetaData.TryGetValue(ProfileMetadataBinaryUtils.PatchersPrefix, out collection))
                    {
                        for (var i = 0; i < collection.Length; i++)
                        {
                            item.PatchersMetaDataList.Add(new ItemWithPath(i)
                            {
                                FullOsPath = collection[i]
                            });
                        }
                    }
                        
                    // Mod list loading
                    if (profileMetaData.TryGetValue(ProfileMetadataBinaryUtils.ModsPrefix, out collection))
                    {
                        if (profileMetaData.TryGetValue(ProfileMetadataBinaryUtils.ModsNamePrefix, out var modNames))
                        {
                            if (modNames.Length != collection.Length)
                                throw new InvalidOperationException("Invalid size match from ModNames and ModPaths.");
                            for (var i = 0; i < collection.Length; i++)
                            {
                                item.ModMetaDataList.Add(new ModItem(i, modNames[i])
                                {
                                    FullOsPath = collection[i]
                                });
                            }
                        }
                    }

                    return true;
                }

                // If the file is missing, then it misses metadata and will use the current directory by default
                // It's a fail-safe that the UI may never allow, but keeping it here for logical flow
                item.IsProfileMissingMetadata = true;
            }
            else
            {
                // If it is the selected one, then the code below already generates the needed metadata
                item.IsProfileMissingMetadata = false;
            }

            // Get game root path
            var rootPath = GameFolderViewer.GetGameRootPath();
            // Get the BepInEx path
            var bepInExPath = GameFolderViewer.GetPathFrom(IGameFolderViewer.CommonDirectory.BepInEx);

            // Configs loading
            var configsPath = GameFolderViewer.SearchPath(bepInExPath, Constants.ConfigFolder);
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
            var patchersPath = GameFolderViewer.SearchPath(bepInExPath, Constants.PatchersFolder);
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
            var modsPath = GameFolderViewer.SearchPath(bepInExPath, Constants.PluginsFolder);
            if (Directory.Exists(modsPath))
            {
                // TODO: Scan for mod meta data, since they have some special access inside the game
                // For now, primitive scanning may be used
                foreach (var mod in Directory.GetFiles(modsPath))
                    item.ModMetaDataList.Add(new ModItem(item.ModMetaDataList.Count,
                        Path.GetFileNameWithoutExtension(mod))
                    {
                        FullOsPath = mod,
                        RelativeOsPath = Path.GetRelativePath(rootPath, mod)
                    });
            }

            return true;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to gather information for the {item.ProfileName}!", Constants.DebugError);
            Debug.WriteLine(ex.ToString(), Constants.DebugError);
            
            if (!item.IsSelectedProfile) // If it is still not the selected profile, then technically it is missing metadata
                item.IsProfileMissingMetadata = true;
            
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
        bool deleteExistingStorage = false,
        IProgress<(int, int, string?)>? progress = null)
    {
        // Generate folder path
        var profilesFolderPath = GameFolderViewer.SearchPath(
            GameFolderViewer.GetPathFrom(IGameFolderViewer.CommonDirectory.ManagerRoot),
            ProfileFolderName
        );
        // Get the zip file path
        var zipPath = GameFolderViewer.SearchPath(profilesFolderPath, profileItem.ProfileName + ContentZipFileExtension);
        
        // Update ProfileItem metadata
        profileItem.FullOsPath = zipPath;
        profileItem.RelativeOsPath = Path.GetRelativePath(GameFolderViewer.GetGameRootPath(), zipPath);
        
        // Try to generate profile info
        try
        {
            // Deletes everything to be clean
            if (deleteExistingStorage)
                RemoveProfileStorageReference();
            
            // Get information for the profile
            if (!GatherProfileFolderInformation(profileItem))
                return false;
            
            profileItem.DateOfCreation = File.GetCreationTime(zipPath);
            profileItem.DateOfUpdate = DateTime.Now; // Get date from system, since things were updated

            // If no new content is required, no writing is either.
            if (!generateNewContentStorage) return true;

            var zipName = profileItem.ProfileName + ContentZipFileExtension;
            
            // ****** Metadata Generation ******
            var metadataFile = GameFolderViewer.SearchPath(profilesFolderPath, zipName + ContentMetadataFileExtension);
            var tempMetadataFile = GameFolderViewer.SearchPath(profilesFolderPath, 
                profileItem.ProfileName + TempContentZipFileSuffix + 
                ContentZipFileExtension + ContentMetadataFileExtension);
            
            var success = false;
            try
            {
                await using (var writer = new BinaryWriter(File.OpenWrite(tempMetadataFile)))
                {
                    // Writes profileItem metadata
                    profileItem.WriteDirectoryStructure(writer, progress);
                }
                
                // Copy the temp one to have a different name (overwrite)
                File.Copy(tempMetadataFile, metadataFile, true);
                
                // Delete the temporary zip file
                File.Delete(tempMetadataFile);
                
                success = true;
            }
            catch (Exception e)
            {
                Debug.WriteLine("Failed to create the profile content.", Constants.DebugError);
                Debug.WriteLine(e.ToString(), Constants.DebugError);
            }
            finally
            {
                switch (success)
                {
                    // If the metadata wasn't made successfully, delete it
                    case false when File.Exists(tempMetadataFile):
                        try
                        {
                            File.Delete(tempMetadataFile);
                        }
                        catch (IOException e)
                        {
                            Debug.WriteLine("Could not delete partial metadata file.", Constants.DebugError);
                            Debug.WriteLine(e.ToString(), Constants.DebugError);
                        }

                        break;
                    // Update this in case it has metadata now
                    case true:
                        profileItem.IsProfileMissingMetadata = false;
                        break;
                }
            }

            // If the JSON wasn't a success, don't continue with the zip then!
            if (!success)
                return false;
            
            // ****** ZIP Generation *******
            // Redo zip path
            var tempZipPath = GameFolderViewer.SearchPath(profilesFolderPath, profileItem.ProfileName + TempContentZipFileSuffix + ContentZipFileExtension);
            
            success = false;
            try
            {
                // Gather lists into their local variables
                var configs = profileItem.ConfigsMetaDataList;
                var patchers = profileItem.PatchersMetaDataList;
                var mods = profileItem.ModMetaDataList;

                // TODO: Once mod is properly implemented, the code should:
                // Calculate the amount of mods and all the other folders that are required by the mods
                // to be added to totalFiles and be used inside the final for loop from this method.

                var totalFiles = configs.Count + patchers.Count + mods.Count; // Calculate total
                // ** Write all the content inside the game folder to the profile
                // Get the zip file ready
                await using (var zipArchiveHandler =
                             File.OpenWrite(tempZipPath))
                {
                    // Create a writer
                    using var zipWriter = WriterFactory.Open(zipArchiveHandler, ArchiveType.Zip, CompressionType.LZMA);

                    // Get root path from game
                    var rootPath = GameFolderViewer.GetGameRootPath();

                    var filesZipped = 0;

                    // Look for configs
                    foreach (var t in configs)
                    {
                        filesZipped++;
                        var fullPath = t.FullOsPath;

                        if (string.IsNullOrEmpty(fullPath) || !File.Exists(fullPath))
                        {
                            progress?.Report((filesZipped, totalFiles, "Skipping invalid file..."));
                            continue; // Skip this file
                        }
                        
                        // Report progress
                        progress?.Report((filesZipped, totalFiles, $"Zipping {Path.GetFileName(fullPath)}..."));

                        var entryPathInZip = Path.GetRelativePath(rootPath, fullPath);

                        // Read file stream
                        await using var sourceFileStream = File.OpenRead(fullPath);

                        // Write file to the zip
                        await zipWriter.WriteAsync(entryPathInZip, sourceFileStream);
                    }

                    // If no file found, make a stub directory in the zip
                    if (configs.Count == 0)
                        await zipWriter.WriteDirectoryAsync(Path.GetRelativePath(GameFolderViewer.GetGameRootPath(), 
                            GameFolderViewer.SearchPath(
                                GameFolderViewer.GetPathFrom(IGameFolderViewer.CommonDirectory.BepInEx),
                                Constants.ConfigFolder 
                                )));

                    // Look for patchers
                    foreach (var t in patchers)
                    {
                        filesZipped++;
                        var fullPath = t.FullOsPath;

                        if (string.IsNullOrEmpty(fullPath) || !File.Exists(fullPath))
                        {
                            progress?.Report((filesZipped, totalFiles, "Skipping invalid file..."));
                            continue; // Skip this file
                        }
                        
                        // Report progress
                        progress?.Report((filesZipped, totalFiles, $"Zipping {Path.GetFileName(fullPath)}..."));

                        var entryPathInZip = Path.GetRelativePath(rootPath, fullPath);

                        // Read file stream
                        await using var sourceFileStream = File.OpenRead(fullPath);

                        // Write file to the zip
                        await zipWriter.WriteAsync(entryPathInZip, sourceFileStream);
                    }
                    
                    // If no file found, make a stub directory in the zip
                    if (patchers.Count == 0)
                        await zipWriter.WriteDirectoryAsync(Path.GetRelativePath(GameFolderViewer.GetGameRootPath(), 
                            GameFolderViewer.SearchPath(
                                GameFolderViewer.GetPathFrom(IGameFolderViewer.CommonDirectory.BepInEx),
                                Constants.PatchersFolder
                            )));

                    // TODO: Make this loop also not use the primitive approach (Reminder for the bigger note above these loops).
                    // Look for mods
                    foreach (var t in mods)
                    {
                        filesZipped++;
                        var fullPath = t.FullOsPath;

                        if (string.IsNullOrEmpty(fullPath) || !File.Exists(fullPath))
                        {
                            progress?.Report((filesZipped, totalFiles, "Skipping invalid file..."));
                            continue; // Skip this file
                        }
                        
                        // Report progress
                        progress?.Report((filesZipped, totalFiles, $"Zipping {Path.GetFileName(fullPath)}..."));

                        var entryPathInZip = Path.GetRelativePath(rootPath, fullPath);

                        // Read file stream
                        await using var sourceFileStream = File.OpenRead(fullPath);

                        // Write file to the zip
                        await zipWriter.WriteAsync(entryPathInZip, sourceFileStream);
                    }
                    
                    // If no file found, make a stub directory in the zip
                    if (mods.Count == 0)
                        await zipWriter.WriteDirectoryAsync(Path.GetRelativePath(GameFolderViewer.GetGameRootPath(), 
                            GameFolderViewer.SearchPath(
                            GameFolderViewer.GetPathFrom(IGameFolderViewer.CommonDirectory.BepInEx),
                            Constants.PluginsFolder
                        )));
                    
                }

                // Copy the temp one to have a different name (overwrite)
                File.Copy(tempZipPath, zipPath, true);
                
                // Delete the temporary zip file
                File.Delete(tempZipPath);
                
                // 100% report
                progress?.Report((totalFiles, totalFiles, "Successfully zipped!"));
                success = true;
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
            if (success)
                Debug.WriteLine($"Successfully zipped {profileItem.ProfileName} to {zipPath}.", Constants.DebugInfo);
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
    IProgress<(int, int, string?)>? progress = null)
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
            
            // Iterate each entry
            foreach (var entry in archive.Entries)
            {
                // Add one entry
                entriesProcessed++;
                
                if (string.IsNullOrEmpty(entry.Key))
                {
                    progress?.Report((entriesProcessed, totalEntryCount, "Skipped invalid entry..."));
                    continue;
                }
                
                var entryIsDirectory = entry.IsDirectory;
                var extractionPath = GameFolderViewer.SearchPath(entry.Key);
                var directory = Path.GetDirectoryName(extractionPath);

                // Skip if invalid directory
                if (string.IsNullOrEmpty(directory))
                {
                    progress?.Report((entriesProcessed, totalEntryCount, "Skipped invalid entry..."));
                    continue;
                }

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
                // Only do the directory scan check, do not extract the directory yet
                if (entryIsDirectory)
                {
                    progress?.Report((entriesProcessed, totalEntryCount, $"Unzipping {Path.GetFileName(entry.Key)}..."));
                    continue;
                }

                // DO not use async method; it bugs out here for some reason, differently from its sync variant.
                // ReSharper disable once MethodHasAsyncOverload
                entry.WriteToDirectory(rootPath, new ExtractionOptions
                {
                    ExtractFullPath = true, Overwrite = true, PreserveAttributes = false, PreserveFileTime = false
                });
                
                // Progress update
                progress?.Report((entriesProcessed, totalEntryCount, $"Unzipping {Path.GetFileName(entry.Key)}..."));
            }
            
            _lastUnzippedProfileItem = profileItem;
            progress?.Report((totalEntryCount, totalEntryCount, "Successfully unzipped!"));
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