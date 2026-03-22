using System;
using System.IO;
using System.Threading.Tasks;
using GottaManagePlus.Models;
using GottaManagePlus.Services.PlusFolderServices;
using GottaManagePlus.Services.ProfileServices.Extractors;
using GottaManagePlus.Services.ProfileServices.Writers;
using Serilog;

namespace GottaManagePlus.Services.ProfileServices;

public class ProfileStorage(PlusFolderBrowser folderBrowser, PlusFolderDb plusFolderDb)
{
    // ----- Private API -----
    private readonly PlusFolderBrowser _plusFolderBrowser = folderBrowser;
    private readonly PlusFolderDb _plusFolderDb = plusFolderDb;
    private string ProfilesFolder => _plusFolderBrowser.SearchAbsolutePath(Constants.AppRootFolder, Constants.AppProfilesFolder);
    private string GetProfilePath(ProfileMetadata metadata) => Path.Combine(ProfilesFolder, metadata.Name);
    
    // ----- Public API -----
    /// <summary>
    /// The instance of <see cref="ProfileMemoryDb"/> maintained by the storage.
    /// </summary>
    public ProfileMemoryDb ProfileMemoryDb { get; } = new();
    /// <summary>
    /// This method empties out the Profile Database to read all the profiles from the local storage again and insert back into the Db.
    /// </summary>
    public void LoadProfilesFromLocalIntoMemory()
    {
        // Clears out the database
        ProfileMemoryDb.ClearProfiles();
        
        // Get profile path
        var profilesFolder = ProfilesFolder;

        // Check if it exists
        if (!Directory.Exists(profilesFolder)) return;

        try
        {
            // Get all files in Profiles folder
            foreach (var profileDir in Directory.EnumerateDirectories(profilesFolder))
            {
                // Attempts to read metadata
                var metadata = ProfileReader.ReadProfile(profileDir);
                if (metadata == null)
                {
                    Log.Logger.Warning("ProfileMetadata from path \'{Profile}\' is null.", profileDir);
                    continue;
                }

                // If metadata exists, add it
                ProfileMemoryDb.AddProfile(metadata);
            }
        }
        catch (Exception e)
        {
            Log.Logger.Error("Failed to load all profiles from storage to memory.\n{exception}", e);
        }
    }

    /// <summary>
    /// This method will save the current environment data from game's folder to given profile.
    /// </summary>
    /// <param name="metadata">The profile to be saved.</param>
    /// <param name="progress">The progress to be made here</param>
    public async Task SaveEnvironmentDataToProfile(ProfileMetadata metadata, IProgress<ProgressReport>? progress)
    {
        // The path to save the profile
        var pathToSave = GetProfilePath(metadata);
        
        // Make a Directory info
        try
        {
            // Creates the directory
            var profileDir = new DirectoryInfo(pathToSave);
            if (!profileDir.Exists)
                profileDir.Create(); // Create if it doesn't exist
            
            // Check for known paths the profile can scan for.
            // * Why not Plugins and Assets? These have special structures that the mod system handles for profiles
            // when installing and uninstalling mods.
            // Configs
            metadata.ConfigurationFiles.Clear();
            var pathToCheckFor = _plusFolderBrowser.SearchRelativePath(Constants.BepInExFolderName, Constants.ConfigFolder);
            foreach (var config in Directory.EnumerateFiles(pathToCheckFor, "*.cfg", SearchOption.AllDirectories))
                metadata.ConfigurationFiles.Add(config);
            // Patchers
            pathToCheckFor = _plusFolderBrowser.SearchRelativePath(Constants.BepInExFolderName, Constants.PatchersFolder);
            foreach (var patcher in Directory.EnumerateFiles(pathToCheckFor, "*.dll", SearchOption.AllDirectories))
                metadata.PatcherFiles.Add(patcher);
            
            // Use the Zip writer to write the profile body
            // and move it to the directory created internally.
            var writer = new ProfileZipWriter();
            await writer.WriteProfileTo(pathToSave, metadata, _plusFolderBrowser, progress);
        }
        catch (Exception e)
        {
            Log.Logger.Error("Failed to generate a profile folder.\n{exception}", e);
        }
    }

    /// <summary>
    /// Tries to extract a profile's data from the storage directly to the game's folder.
    /// </summary>
    /// <param name="metadata">The metadata to be extracted.</param>
    /// <param name="progress">The progress to be reported during extraction.</param>
    /// <returns></returns>
    public async Task<bool> ExtractProfileToEnvironment(ProfileMetadata metadata, IProgress<ProgressReport>? progress)
    {
        // The path to save the profile.
        var pathToSave = GetProfilePath(metadata);
        
        // Makes a directory info to inspect it.
        var profileDir = new DirectoryInfo(pathToSave);
        if (!profileDir.Exists)
            return false;

        // Attempts to extract the zip file to the main directory.
        return await ProfileZipExtractor.ExtractProfile(metadata, pathToSave, _plusFolderDb.RootPath, _plusFolderBrowser, progress);
    }
}