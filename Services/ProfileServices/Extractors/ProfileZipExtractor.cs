using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using GottaManagePlus.Models;
using GottaManagePlus.Services.PlusFolderServices;
using Serilog;
using SharpCompress.Archives;

namespace GottaManagePlus.Services.ProfileServices.Extractors;

/// <summary>
/// A static class specialized in extracting the profile.
/// </summary>
public static class ProfileZipExtractor
{
    /// <summary>
    /// Extracts a profile to its designated path. Usually used to extract profile's content back to the game's folder.
    /// </summary>
    /// <param name="metadata">The metadata to be used in the process.</param>
    /// <param name="profilePath"></param>
    /// <param name="extractToPath"></param>
    /// <param name="browser"></param>
    /// <param name="progress"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    public static async Task<bool> ExtractProfile(ProfileMetadata metadata, string profilePath, string extractToPath, PlusFolderBrowser browser, IProgress<ProgressReport>? progress)
    {
        const string fileExtension = ".zip";
        // Profile Structure:
        // [ProfileName]
        //      [MetadataFile]
        //      [Profile.zip]
        
        // Where it extracts:
        // root
        //     BALDI_Data
        //            StreamingAssets
        //                  Modded/...
        // BepInEx
        //      Configs/
        //      Patches/
        //      Plugins/

        // Assuming we're on [ProfileName], we get a temporary directory to extract Profile.zip
        if (!File.GetAttributes(profilePath).HasFlag(FileAttributes.Directory))
            throw new ArgumentException("Given profile path is not a directory.");

        // Create the file info for this purpose
        var zipFile = new FileInfo(browser.SearchAbsolutePath(
            profilePath, $"{metadata.Name}{fileExtension}"));
        
        // The zip file must exist first, obviously.
        if (!zipFile.Exists)
            return false;
        
        // Make temporary directory
        DirectoryInfo? temporaryDirectory = null;

        try
        {
            temporaryDirectory = Directory.CreateTempSubdirectory($"GMP_{metadata.Name}_ProfileZipExtractor");
            
            // Make an extraction from the zip file to the temporary directory.
            
            // Create stream
            await using var file = zipFile.OpenRead();
            
            // Create zip reader
            using var reader = ArchiveFactory.OpenArchive(file);

            // Count the entries
            var entryCount = reader.Entries.Count();
            var entriesSeen = 0;
            
            // Read the entries and extract them to the temporary directory (expected directories only)
            foreach (var archiveEntry in reader.Entries)
            {
                progress?.Report(
                    new ProgressReport(entriesSeen, entryCount, 
                        "Extracting", $"\'{Path.GetFileName(archiveEntry.Key)}\'..."));
                await archiveEntry.WriteToDirectoryAsync(temporaryDirectory.FullName);
                entriesSeen++;
            }
            
            // ---- EXTRACTION TIME ----
            // Check for known directories to delete them
            TryToDeleteDirectory(browser.SearchAbsolutePath(Constants.BepInExFolderName, Constants.PluginsFolder), false);
            TryToDeleteDirectory(browser.SearchAbsolutePath(Constants.BepInExFolderName, Constants.ConfigFolder), false);
            TryToDeleteDirectory(browser.SearchAbsolutePath(Constants.BepInExFolderName, Constants.PatchersFolder), false);
            // Check for asset directories to delete them as well
            foreach (var asset in metadata.ModDataFiles.SelectMany(mod => mod.Assets))
                TryToDeleteDirectory(browser.SearchAbsolutePath(asset.MovedAsset), true);

            // Now, select the stuff inside the temporary directory and move it to the game's folder.
            // Get the directories inside only.
            var directories = temporaryDirectory.GetDirectories("*", SearchOption.TopDirectoryOnly);
            // Then, move all the profile folders to the path selected.
            for (var i = 0; i < directories.Length; i++)
            {
                var entry = directories[i];
                // Report progress
                progress?.Report(new ProgressReport(i, directories.Length, 
                    "Extracting", $"Moving \'{entry.Name}\'."));
                // Move the entry to the right place.
                entry.MoveTo(extractToPath);
            }

            return true;

            static void TryToDeleteDirectory(string dirPath, bool deleteDirItself)
            {
                if (deleteDirItself)
                {
                    // Delete the directory itself
                    if (Directory.Exists(dirPath))
                        Directory.Delete(dirPath, true);
                    return;
                }
                
                // CLear up the content of the directory
                foreach (var file in Directory.EnumerateFiles(dirPath))
                    File.Delete(file);
                foreach (var dir in Directory.EnumerateDirectories(dirPath))
                    Directory.Delete(dir, true);
            }
        }
        catch (Exception e)
        {
            Log.Logger.Error("Failed to extract the profile content.\n{exception}", e);
            return false;
        }
        finally
        {
            try
            {
                if (temporaryDirectory is { Exists: true })
                    temporaryDirectory.Delete(recursive: true);
            }
            catch 
            { 
                // suppress
            }
        }
    }
}