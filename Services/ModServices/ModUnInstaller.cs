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
using GottaManagePlus.Services.ProfileServices;
using GottaManagePlus.Utils;
using Serilog;

namespace GottaManagePlus.Services.ModServices;

public sealed class ModUnInstaller(ILogger logger, ProfileManager profileManager, GameEnvironmentController controller)
{
    // ---- Private ----
    private readonly ILogger _logger = logger;
    private readonly ProfileManager _profileManager = profileManager;
    private readonly GameEnvironmentController _controller = controller;
    private readonly PatcherIndexManager _patcherIndexManager = new(logger, controller);

    // ---- Public ----
    public void DeleteMod(ModManifest manifest, Action<ProfileMetadata>? afterRemovalCallback = null)
    {
        // Get the active profile.
        var profile = _profileManager.ActiveProfile;
        if (profile == null)
        {
            _logger.Warning("Active profile is null.");
            return;
        }

        // Collect all directories to delete.
        var pluginDir = manifest.GetPluginDirectoryFromManifest(_controller);
        var assetDirs = manifest.Assets.Select(asset => _controller.SearchAbsolutePath(asset.MovedAsset)).ToArray();
        List<string> allDirs = [pluginDir, .. assetDirs];

        // Create a temporary directory for backup.
        using var tempBackup = _controller.CreateTempSubdirectory(_logger);

        // === BACKUP PHASE ===
        // Copy all directories into the temporary location.
        var backupPaths = new Dictionary<string, string>(); // original path -> backup path
        try
        {
            foreach (var dirPath in allDirs)
            {
                if (!Directory.Exists(dirPath))
                {
                    _logger.Warning("Directory '{DirPath}' does not exist. Skipping backup.", dirPath);
                    continue;
                }

                var dirName = Path.GetFileName(dirPath);
                var backupDir = (string)Path.Combine(tempBackup.DirectoryInfo.FullName, dirName); // Make a backup version from original path
                Directory.CreateDirectory(backupDir); // Creates directory
                CopyDirectoryRecursively(dirPath, backupDir); // Copy the stuff from original to the backup
                backupPaths[dirPath] = backupDir;
                _logger.Debug("Backed up '{DirPath}' to '{BackupDir}'.", dirPath, backupDir);
            }
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to backup mod files before deletion. Aborting deletion.");
            return;
        }

        // Remove the manifest from the manager (tentative, will be re-added on failure).
        profile.ModDataFiles.Remove(manifest);

        // === DELETION PHASE ===
        // Attempt to delete the original directories.
        var deletionSucceeded = false;
        try
        {
            // Handle patcher files using the .index system
            var patcherDir = _controller.SearchAbsolutePath(Constants.BepInExFolderName, Constants.PatchersFolder);
            if (Directory.Exists(patcherDir) && manifest.Patchers.Count > 0)
            {
                foreach (var patcherRelativePath in manifest.Patchers)
                {
                    var patcherFileName = Path.GetFileName(patcherRelativePath);
                    var patcherFullPath = (string)Path.Combine(patcherDir, patcherFileName);
                    
                    // Unregister the patcher - returns true if counter <= 0 (can delete)
                    var canDelete = _patcherIndexManager.UnregisterPatcher(manifest, patcherFileName);
                    
                    if (canDelete && File.Exists(patcherFullPath))
                    {
                        _logger.Information("Deleted patcher file '{patcher}'", patcherFullPath);
                        File.Delete(patcherFullPath);
                    }
                    else if (!canDelete)
                    {
                        _logger.Information("Keeping patcher '{patcher}' as it is still used by other mods", patcherFileName);
                    }
                }
            }
            
            // Delete plugin directory.
            if (Directory.Exists(pluginDir))
            {
                _logger.Information("Deleted plugin directory '{dir}'.", pluginDir);
                Directory.Delete(pluginDir, true);
            }

            // Delete asset directories.
            foreach (var assetDir in assetDirs.Where(p => Directory.Exists(p)))
            {
                _logger.Information("Deleted asset directory '{dir}'.", assetDir);
                Directory.Delete(assetDir, true);
            }

            deletionSucceeded = true;
            _logger.Information("Successfully deleted mod '{ManifestName}'.", manifest.Name);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Deletion of mod '{ManifestName}' failed. Attempting restore from backup.", manifest.Name);
        }

        // === POST DELETION PHASE ===
        // If deletion failed, restore from backup and re-add manifest.
        if (!deletionSucceeded)
        {
            _logger.Information("Deletion didn't succeeded! Restoring directories...");
            try
            {
                foreach (var (original, backup) in backupPaths)
                {
                    if (!Directory.Exists(backup))
                    {
                        _logger.Warning("Backup directory '{Backup}' missing. Cannot restore '{Original}'.", backup, original);
                        continue;
                    }

                    var dirName = Path.GetDirectoryName(original);
                    if (string.IsNullOrEmpty(dirName))
                    {
                        _logger.Warning("Backup directory '{Backup}' has no directory name. Cannot restore '{Original}'.", backup, original);
                        continue;
                    }

                    // Ensure parent directory exists
                    Directory.CreateDirectory(dirName);

                    // Overwrite the original with the backup
                    CopyDirectoryRecursively(backup, original);
                    _logger.Debug("Restored '{Original}' from backup.", original);
                }

                // Re-add the manifest to profile data.
                profile.ModDataFiles.Add(manifest);
                _logger.Information("Restored mod '{ManifestName}' from backup due to deletion failure.", manifest.Name);
            }
            catch (Exception restoreEx)
            {
                _logger.Error(restoreEx, "Failed to restore mod '{ManifestName}' from backup. Data may be lost.", manifest.Name);
                // Do not call callback because mod is not successfully removed.
                return;
            }
        }

        // If we reach here, deletion succeeded (or restoration was not needed). Invoke callback.
        afterRemovalCallback?.Invoke(profile);
    }

    // ---- Private Helpers ----
    private static void CopyDirectoryRecursively(string sourceDir, string destDir)
    {
        var source = new DirectoryInfo(sourceDir);
        var dest = new DirectoryInfo(destDir);

        if (!source.Exists)
            throw new DirectoryNotFoundException($"Source directory not found: {sourceDir}");

        dest.Create();

        // Copy files
        foreach (var file in source.GetFiles())
        {
            var destFile = (string)Path.Combine(dest.FullName, file.Name);
            file.CopyTo(destFile, true);
        }

        // Copy subdirectories
        foreach (var subDir in source.GetDirectories())
        {
            var destSubDir = (string)Path.Combine(dest.FullName, subDir.Name);
            CopyDirectoryRecursively(subDir.FullName, destSubDir);
        }
    }
}