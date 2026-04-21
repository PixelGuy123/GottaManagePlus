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
        _logger.Information("Starting profile extraction for '{ProfileName}' from '{ProfilePath}' to '{ExtractToPath}'", metadata.Name, profilePath, extractToPath);
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
        _logger.Information("Profile path is a directory: {ProfilePath}", profilePath);

        // Create the file info for this purpose
        var zipFile = new FileInfo(controller.SearchAbsolutePath(
            profilePath, $"{metadata.Name}{fileExtension}"));
        
        // The zip file must exist first, obviously.
        if (!zipFile.Exists)
        {
            _logger.Warning("Zip file does not exist: {ZipFilePath}", zipFile.FullName);
            return false;
        }
        
        // Make temporary directory
        DirectoryInfo? temporaryDirectory = null; // for extracting content
        var backupDir = controller.CreateTempSubdirectory(_logger); // for saving previous content just in case
        _logger.Information("Created backup directory: {BackupDir}", backupDir.FullName);

        try
        {
            temporaryDirectory = controller.CreateTempSubdirectory(_logger);
            _logger.Information("Created temporary directory: {TempDir}", temporaryDirectory.FullName);
            
            // Make an extraction from the zip file to the temporary directory.
            // Create stream
            await using var file = zipFile.OpenRead();
            
            // Create zip reader
            using var reader = ArchiveFactory.OpenArchive(file);

            // Count the entries
            var entryCount = reader.Entries.Count();
            var entriesSeen = 0;
            _logger.Information("Starting extraction of {EntryCount} entries from zip to temp dir", entryCount);
            
            // Read the entries and extract them to the temporary directory (expected directories only)
            foreach (var archiveEntry in reader.Entries)
            {
                cancellationToken.ThrowIfCancellationRequested();
                progress?.Report(
                    new ProgressReport(entriesSeen, entryCount, 
                        "Extracting", $"\'{Path.GetFileName(archiveEntry.Key)}\'..."));
                _logger.Information("Extracting \'{ArchiveEntryKey}\' to \'{Combine}\'", archiveEntry.Key, Path.Combine(temporaryDirectory.FullName, Path.GetFileName(archiveEntry.Key!)));
                await archiveEntry.WriteToDirectoryAsync(temporaryDirectory.FullName, cancellationToken: cancellationToken);
                entriesSeen++;
            }
            _logger.Information("Extraction to temp dir completed");
            
            // ---- EXTRACTION TIME ----
            // Backup and delete known directories
            _logger.Information("Starting backup and delete of known directories");
            BackupAndDelete(controller.SearchAbsolutePath(Constants.BepInExFolderName, Constants.PluginsFolder), backupDir.FullName);
            BackupAndDelete(controller.SearchAbsolutePath(Constants.BepInExFolderName, Constants.ConfigFolder), backupDir.FullName);
            BackupAndDelete(controller.SearchAbsolutePath(Constants.BepInExFolderName, Constants.PatchersFolder), backupDir.FullName);
            // Backup and delete asset directories
            _logger.Information("Starting backup of asset directories");
            foreach (var asset in metadata.ModDataFiles.SelectMany(mod => mod.Assets))
            {
                cancellationToken.ThrowIfCancellationRequested();
                _logger.Information("Backing up asset: {Asset}", asset.MovedAsset);
                BackupAndDelete(controller.SearchAbsolutePath(asset.MovedAsset), backupDir.FullName);
            }

            // Now, select the stuff inside the temporary directory and move it to the game's folder.
            // Get the directories inside only.
            var directories = temporaryDirectory.GetDirectories("*", SearchOption.TopDirectoryOnly);
            _logger.Information("Starting to move {DirCount} directories from temp to extract path", directories.Length);
            // Then, move all the profile folders to the path selected.
            for (var i = 0; i < directories.Length; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var entry = directories[i];
                // Report progress
                progress?.Report(new ProgressReport(i, directories.Length, 
                    "Extracting", $"Moving \'{entry.Name}\'."));

                // Move the entry to the right place.
                var destinationPath = Path.Combine(extractToPath, entry.Name);
                _logger.Information("Moving \'{EntryFullName}\' to \'{ExtractToPath}\'", entry.FullName, destinationPath);
                entry.AtomicallyMoveTo(destinationPath);
            }
            _logger.Information("Moving completed");

            return true;
        }
        catch (Exception e)
        {
            _logger.Error("Failed to extract the profile content.\n{exception}", e);
            // Restore from backup
            RestoreFromBackup(backupDir.FullName, controller.CurrentEnvironment!.RootPath);
            _logger.Information("Restored from backup due to error");
            return false;
        }
        finally
        {
            _logger.Information("Cleaning up temporary and backup directories");
            try
            {
                if (temporaryDirectory is { Exists: true })
                    temporaryDirectory.Delete(recursive: true);
                // Clean up backup after successful extraction or after restore
                if (backupDir is { Exists: true })
                    backupDir.Delete(recursive: true);
            }
            catch 
            { 
                // suppress
            }
        }
        // Local Functions
        void BackupAndDelete(string dirPath, string backupRoot)
        {
            _logger.Information("Attempt to backup \'{DirPath}\'.'", dirPath);
            // If the directory does not exist, return then
            if (!Directory.Exists(dirPath))
            {
                _logger.Information("Directory to backup not found.");
                return;
            }
            
            // Backup path to go.
            var backupPath = Path.Combine(backupRoot, Path.GetFileName(dirPath));
            _logger.Information("Backing up into \'{DirPath}\'.'", backupPath);
            
            // If the backup path already exists, delete it.
            if (Directory.Exists(backupPath))
                Directory.Delete(backupPath, true);
            
            // Then, move the directory to this backup path.
            Directory.Move(dirPath, backupPath);
        }

        void RestoreFromBackup(string backupRoot, string rootPath)
        {
            _logger.Information("Attempt to restore from backup folder \'{backup}\'!", backupRoot);
            if (!Directory.Exists(backupRoot))
            {
                _logger.Information("Backup root does not exist.");
                return;
            }
            
            // Attempt to go into each directory and move it to the right place.
            foreach (var dir in Directory.EnumerateDirectories(backupRoot))
            {
                // Generate the right path.
                var originalPath = Path.Combine(rootPath, Path.GetFileName(dir));
                _logger.Information("Restoring directory \'{Dir}\' to \'{OriginalPath}\'...", dir, originalPath);
                
                // If the path exists, delete beforehand
                if (Directory.Exists(originalPath))
                    Directory.Delete(originalPath, true);
                
                // Then, move the directory to the destination.
                Directory.Move(dir, originalPath);
            }
        }
    }
}

