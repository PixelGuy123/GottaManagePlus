using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GottaManagePlus.Models;
using GottaManagePlus.Services.GameEnvironmentServices;
using GottaManagePlus.Utils;
using Serilog;
using SharpCompress.Archives;

namespace GottaManagePlus.Services.ProfileServices.Extractors;

/// <summary>
/// A class specialized in extracting the profile.
/// </summary>
public sealed class ProfileZipExtractor(ILogger logger)
{
    private readonly ILogger _logger = logger;
    
    /// <summary>
    /// Extracts a profile to its designated path. Usually used to extract profile's content back to the game's folder.
    /// </summary>
    /// <param name="metadata">The metadata to be used in the process.</param>
    /// <param name="profilePath"></param>
    /// <param name="extractToPath"></param>
    /// <param name="controller">The environment controller for path resolution.</param>
    /// <param name="progress">The progress to be reported</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    public async Task<bool> ExtractProfile(ProfileMetadata metadata, string profilePath, string extractToPath, GameEnvironmentController controller, IProgress<ProgressReport>? progress, CancellationToken cancellationToken = default)
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
        var zipFile = new FileInfo(controller.SearchAbsolutePath(
            profilePath, $"{metadata.Name}{fileExtension}"));
        
        // The zip file must exist first, obviously.
        if (!zipFile.Exists)
            return false;
        
        // Make temporary directory
        DirectoryInfo? temporaryDirectory = null;
        var backupDir = Path.Combine(controller.CurrentEnvironment!.RootPath, Constants.BackupDir);

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
                cancellationToken.ThrowIfCancellationRequested();
                progress?.Report(
                    new ProgressReport(entriesSeen, entryCount, 
                        "Extracting", $"\'{Path.GetFileName(archiveEntry.Key)}\'..."));
                await archiveEntry.WriteToDirectoryAsync(temporaryDirectory.FullName, cancellationToken: cancellationToken);
                entriesSeen++;
            }
            
            // ---- EXTRACTION TIME ----
            // Backup and delete known directories
            BackupAndDelete(controller.SearchAbsolutePath(Constants.BepInExFolderName, Constants.PluginsFolder), backupDir);
            BackupAndDelete(controller.SearchAbsolutePath(Constants.BepInExFolderName, Constants.ConfigFolder), backupDir);
            BackupAndDelete(controller.SearchAbsolutePath(Constants.BepInExFolderName, Constants.PatchersFolder), backupDir);
            // Backup and delete asset directories
            foreach (var asset in metadata.ModDataFiles.SelectMany(mod => mod.Assets))
            {
                cancellationToken.ThrowIfCancellationRequested();
                BackupAndDelete(controller.SearchAbsolutePath(asset.MovedAsset), backupDir);
            }

            // Now, select the stuff inside the temporary directory and move it to the game's folder.
            // Get the directories inside only.
            var directories = temporaryDirectory.GetDirectories("*", SearchOption.TopDirectoryOnly);
            // Then, move all the profile folders to the path selected.
            for (var i = 0; i < directories.Length; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var entry = directories[i];
                // Report progress
                progress?.Report(new ProgressReport(i, directories.Length, 
                    "Extracting", $"Moving \'{entry.Name}\'."));
                // Move the entry to the right place.
                entry.MoveTo(extractToPath);
            }

            return true;
        }
        catch (Exception e)
        {
            _logger.Error("Failed to extract the profile content.\n{exception}", e);
            // Restore from backup
            RestoreFromBackup(backupDir, controller.CurrentEnvironment.RootPath);
            return false;
        }
        finally
        {
            try
            {
                if (temporaryDirectory is { Exists: true })
                    temporaryDirectory.Delete(recursive: true);
                // Clean up backup after successful extraction or after restore
                if (Directory.Exists(backupDir))
                    Directory.Delete(backupDir, true);
            }
            catch 
            { 
                // suppress
            }
        }
        // Local Functions
        void BackupAndDelete(string dirPath, string backupRoot)
        {
            if (!Directory.Exists(dirPath)) return;
            
            var backupPath = Path.Combine(backupRoot, Path.GetFileName(dirPath));
            if (Directory.Exists(backupPath))
                Directory.Delete(backupPath, true);
            Directory.Move(dirPath, backupPath);
        }

        void RestoreFromBackup(string backupRoot, string rootPath)
        {
            if (!Directory.Exists(backupRoot)) return;
            
            foreach (var dir in Directory.EnumerateDirectories(backupRoot))
            {
                var originalPath = Path.Combine(rootPath, Path.GetFileName(dir));
                if (Directory.Exists(originalPath))
                    Directory.Delete(originalPath, true);
                Directory.Move(dir, originalPath);
            }
        }
    }
}