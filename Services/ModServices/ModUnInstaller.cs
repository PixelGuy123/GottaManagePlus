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
                var backupDir = Path.Combine(tempBackup.DirectoryInfo.FullName, dirName); // Make a backup version from original path
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
            // Delete plugin directory.
            if (Directory.Exists(pluginDir))
            {
                _logger.Information("Deleted plugin directory '{dir}'.", pluginDir);
                Directory.Delete(pluginDir, true);
            }

            // Delete asset directories.
            foreach (var assetDir in assetDirs.Where(Directory.Exists))
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

                    // Ensure parent directory exists
                    Directory.CreateDirectory(Path.GetDirectoryName(original)!);

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
            var destFile = Path.Combine(dest.FullName, file.Name);
            file.CopyTo(destFile, true);
        }

        // Copy subdirectories
        foreach (var subDir in source.GetDirectories())
        {
            var destSubDir = Path.Combine(dest.FullName, subDir.Name);
            CopyDirectoryRecursively(subDir.FullName, destSubDir);
        }
    }
}