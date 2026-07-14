/*
This file is part of GottaManagePlus (https://github.com/PixelGuy123/GottaManagePlus)

    Copyright (C) 2026 PixelGuy123

    This program is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with this program.  If not, see <https://www.gnu.org/licenses/>.

*/

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
        _logger.Information("Starting profile extraction for '{ProfileName}' from '{ProfilePath}' to '{ExtractToPath}'", metadata.Name, profilePath, extractToPath);

        if (!File.GetAttributes(profilePath).HasFlag(FileAttributes.Directory))
            throw new ArgumentException("Given profile path is not a directory.");
        _logger.Information("Profile path is a directory: {ProfilePath}", profilePath);

        var zipFile = new FileInfo(controller.SearchAbsolutePath(
            profilePath, $"{metadata.Name}{Constants.ProfileDefaultExtension}"));

        if (!zipFile.Exists)
        {
            _logger.Warning("Zip file does not exist: {ZipFilePath}", zipFile.FullName);
            return false;
        }
        
        using var backupDir = controller.CreateTempSubdirectory(_logger);
        var invalidBackupDir = Path.Combine(backupDir.DirectoryInfo.FullName, "InvalidModsBackup");
        Directory.CreateDirectory(invalidBackupDir);
        _logger.Information("Created backup directory: {BackupDir}", backupDir.DirectoryInfo.FullName);

        // ---------- Store info about every backed‑up item ----------
        var backedUpItems = new List<(string OriginalPath, string BackupPath, bool IsDirectory)>();

        try
        {
            using var temporaryDirectory = controller.CreateTempSubdirectory(_logger);
            _logger.Information("Created temporary directory: {TempDir}", temporaryDirectory.DirectoryInfo.FullName);

            // Extract the zip content into the temporary directory.
            await using var file = zipFile.OpenRead();
            using var reader = ArchiveFactory.OpenArchive(file);

            var entryCount = reader.Entries.Count();
            var entriesSeen = 0;
            _logger.Information("Starting extraction of {EntryCount} entries from zip to temp dir", entryCount);

            foreach (var archiveEntry in reader.Entries)
            {
                cancellationToken.ThrowIfCancellationRequested();
                progress?.Report(
                    new ProgressReport(entriesSeen, entryCount,
                        "Extracting", $"'{Path.GetFileName(archiveEntry.Key)}'..."));
                _logger.Information("Extracting '{ArchiveEntryKey}' to '{Combine}'", archiveEntry.Key, (string)Path.Combine(temporaryDirectory.DirectoryInfo.FullName, archiveEntry.Key!));
                await archiveEntry.WriteToDirectoryAsync(temporaryDirectory.DirectoryInfo.FullName, cancellationToken: cancellationToken);
                entriesSeen++;
            }
            _logger.Information("Extraction to temp dir completed");

            // ----- Backup known directories (Plugins, Config, Patchers) -----
            _logger.Information("Starting backup and delete of known directories");
            BackupItem(controller.SearchAbsolutePath(Constants.BepInExFolderName, Constants.PluginsFolder), backupDir.DirectoryInfo.FullName);
            BackupItem(controller.SearchAbsolutePath(Constants.BepInExFolderName, Constants.ConfigFolder), backupDir.DirectoryInfo.FullName);
            BackupItem(controller.SearchAbsolutePath(Constants.BepInExFolderName, Constants.PatchersFolder), backupDir.DirectoryInfo.FullName);

            // ----- Backup asset directories/files -----
            _logger.Information("Starting backup of asset directories");
            foreach (var asset in metadata.ModDataFiles.SelectMany(mod => mod.Assets))
            {
                BackupItem(controller.SearchAbsolutePath(asset.MovedAsset), backupDir.DirectoryInfo.FullName);
                cancellationToken.ThrowIfCancellationRequested();
                _logger.Information("Backing up asset: {Asset}", asset.MovedAsset);
            }

            // ----- Move the extracted profile content into the game folder -----
            var directories = temporaryDirectory.DirectoryInfo.GetDirectories("*", SearchOption.TopDirectoryOnly);
            _logger.Information("Starting to move {DirCount} directories from temp to extract path", directories.Length);

            for (var i = 0; i < directories.Length; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var entry = directories[i];
                progress?.Report(new ProgressReport(i, directories.Length,
                    "Extracting", $"Moving '{entry.Name}'."));

                var destinationPath = (string)Path.Combine(extractToPath, entry.Name);
                _logger.Information("Moving '{EntryFullName}' to '{ExtractToPath}'", entry.FullName, destinationPath);
                entry.AtomicallyMoveTo(destinationPath);
            }
            _logger.Information("Moving completed");

            // ----- Restore any backed‑up item that was NOT replaced by the profile -----
            _logger.Information("Checking for missing items to restore from backup");
            foreach (var (originalPath, backupPath, isDirectory) in backedUpItems)
            {
                // If the original path already exists (because the profile provided a replacement), skip.
                if (Directory.Exists(originalPath) || File.Exists(originalPath))
                    continue;

                _logger.Information("Restoring missing item: {OriginalPath} from {BackupPath}", originalPath, backupPath);
                try
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(originalPath)!);
                    if (isDirectory)
                    {
                        // Ensure parent exists (should already, but just in case)
                        new DirectoryInfo(backupPath).AtomicallyMoveTo(originalPath);
                    }
                    else
                    {
                        File.Move(backupPath, originalPath);
                    }
                }
                catch (Exception ex)
                {
                    _logger.Warning(ex, "Failed to restore {OriginalPath} from backup", originalPath);
                }
            }

            return true;
        }
        catch (Exception e)
        {
            _logger.Error(e, "Failed to extract the profile content.");
            // Full restore from backup (all items)
            RestoreAllFromBackup(backupDir.DirectoryInfo.FullName, controller.CurrentEnvironment!.RootPath);
            _logger.Information("Restored from backup due to error.");
            return false;
        }

        // ---------- Local functions ----------

        void BackupItem(string originalPath, string backupRoot)
        {
            _logger.Information("Attempt to backup '{OriginalPath}'", originalPath);

            var isDirectory = Directory.Exists(originalPath);
            var isFile = File.Exists(originalPath);

            if (!isDirectory && !isFile)
            {
                _logger.Information("Item to backup not found: {OriginalPath}", originalPath);
                return;
            }

            var backupPath = (string)Path.Combine(backupRoot, Path.GetFileName(originalPath));
            _logger.Information("Backing up into '{BackupPath}'", backupPath);

            // Delete any previous backup with the same name (should not happen normally)
            if (Directory.Exists(backupPath))
                Directory.Delete(backupPath, true);
            if (File.Exists(backupPath))
                File.Delete(backupPath);

            if (isDirectory)
                new DirectoryInfo(originalPath).AtomicallyMoveTo(backupPath);
            else // it's a file
            {
                // Ensure the backup directory exists
                Directory.CreateDirectory(backupRoot);
                File.Move(originalPath, backupPath);
            }

            backedUpItems.Add((originalPath, backupPath, isDirectory));
        }

        void RestoreAllFromBackup(string backupRoot, string rootPath)
        {
            _logger.Information("Attempt to restore from backup folder '{BackupRoot}'", backupRoot);
            if (!Directory.Exists(backupRoot))
            {
                _logger.Information("Backup root does not exist.");
                return;
            }

            foreach (var dir in Directory.EnumerateDirectories(backupRoot))
            {
                var originalPath = (string)Path.Combine(rootPath, Path.GetFileName(dir));
                _logger.Information("Restoring directory '{Dir}' to '{OriginalPath}'...", dir, originalPath);

                if (Directory.Exists(originalPath))
                    Directory.Delete(originalPath, true);

                Directory.Move(dir, originalPath);
            }

            // Also handle any files that might have been backed up directly (e.g., asset files)
            foreach (var file in Directory.EnumerateFiles(backupRoot))
            {
                var originalPath = (string)Path.Combine(rootPath, Path.GetFileName(file));
                _logger.Information("Restoring file '{File}' to '{OriginalPath}'...", file, originalPath);

                if (File.Exists(originalPath))
                    File.Delete(originalPath);

                File.Move(file, originalPath);
            }
        }
    }
}

