using GottaManagePlus.Models;
using GottaManagePlus.Services.GameEnvironmentServices;
using GottaManagePlus.Utils;
using Serilog;

namespace GottaManagePlus.Services.ModServices;

/// <summary>
/// Manages the .index system for patchers in the BepInEx/patchers folder.
/// This allows multiple mods to share the same patcher file without conflicts.
/// </summary>
public sealed class PatcherIndexManager(ILogger logger, GameEnvironmentController controller)
{
    private readonly ILogger _logger = logger;
    private readonly GameEnvironmentController _controller = controller;

    /// <summary>
    /// Gets the path to the .index folder inside the Patchers directory.
    /// </summary>
    public string GetIndexFolderPath() =>
        _controller.SearchAbsolutePath(Constants.BepInExFolderName, Constants.PatchersFolder, Constants.PatchersIndexFolder);

    /// <summary>
    /// Ensures the .index folder exists.
    /// </summary>
    public void EnsureIndexFolderExists()
    {
        var indexFolder = GetIndexFolderPath();
        DirectoryUtils.GetOrCreate(indexFolder);
    }

    /// <summary>
    /// Generates a unique index file name for a patcher based on the manifest.
    /// </summary>
    /// <param name="manifest">The mod manifest.</param>
    /// <param name="patcherFileName">The original patcher file name.</param>
    /// <returns>The index file name (e.g., "MyMod_MyPatcher.dll.txt").</returns>
    public string GetIndexFileName(ModManifest manifest, string patcherFileName)
    {
        var safeManifestName = PathUtils.TurnFileNameLegal(manifest.ToString());
        return $"{safeManifestName}_{patcherFileName}.txt";
    }

    /// <summary>
    /// Registers a patcher by incrementing its counter in the .index folder.
    /// If the counter file doesn't exist, it starts at 1.
    /// </summary>
    /// <param name="manifest">The mod manifest requesting the patcher.</param>
    /// <param name="patcherFileName">The name of the patcher file.</param>
    /// <returns>The actual patcher file name to use in the Patchers folder.</returns>
    public string RegisterPatcher(ModManifest manifest, string patcherFileName)
    {
        EnsureIndexFolderExists();
        
        var indexFilePath = Path.Combine(GetIndexFolderPath(), GetIndexFileName(manifest, patcherFileName));
        var actualPatcherFileName = patcherFileName; // The actual file name in Patchers folder
        
        // Check if the index file already exists
        if (File.Exists(indexFilePath))
        {
            // Read current count
            var content = File.ReadAllText(indexFilePath);
            if (int.TryParse(content, out var count))
            {
                // Increment counter
                count++;
                File.WriteAllText(indexFilePath, count.ToString());
                _logger.Debug("Incremented patcher counter for '{Patcher}' to {Count}", patcherFileName, count);
            }
            else
            {
                // Invalid content, reset to 1
                File.WriteAllText(indexFilePath, "1");
                _logger.Warning("Invalid counter value for '{Patcher}', resetting to 1", patcherFileName);
            }
        }
        else
        {
            // Create new counter file with value 1
            File.WriteAllText(indexFilePath, "1");
            _logger.Debug("Created new patcher index for '{Patcher}' with count 1", patcherFileName);
        }

        return actualPatcherFileName;
    }

    /// <summary>
    /// Unregisters a patcher by decrementing its counter in the .index folder.
    /// If the counter reaches 0 or below, the patcher file can be deleted.
    /// </summary>
    /// <param name="manifest">The mod manifest releasing the patcher.</param>
    /// <param name="patcherFileName">The name of the patcher file.</param>
    /// <returns>
    /// <see langword="true"/> if the counter reached 0 or below (patcher can be deleted);
    /// <see langword="false"/> if other mods still use this patcher.
    /// </returns>
    public bool UnregisterPatcher(ModManifest manifest, string patcherFileName)
    {
        var indexFilePath = Path.Combine(GetIndexFolderPath(), GetIndexFileName(manifest, patcherFileName));
        
        if (!File.Exists(indexFilePath))
        {
            _logger.Warning("Index file for '{Patcher}' does not exist, nothing to unregister", patcherFileName);
            return true; // No index means we can delete
        }

        // Read current count
        var content = File.ReadAllText(indexFilePath);
        if (!int.TryParse(content, out var count))
        {
            _logger.Warning("Invalid counter value for '{Patcher}', deleting index file", patcherFileName);
            File.Delete(indexFilePath);
            return true;
        }

        // Decrement counter
        count--;
        
        if (count <= 0)
        {
            // Remove the index file
            File.Delete(indexFilePath);
            _logger.Information("Patcher '{Patcher}' counter reached {Count}, can be deleted", patcherFileName, count);
            return true; // Can delete the patcher file
        }
        else
        {
            // Update counter
            File.WriteAllText(indexFilePath, count.ToString());
            _logger.Debug("Decremented patcher counter for '{Patcher}' to {Count}", patcherFileName, count);
            return false; // Keep the patcher file
        }
    }

    /// <summary>
    /// Checks if a patcher is currently in use by any mod.
    /// </summary>
    /// <param name="patcherFileName">The name of the patcher file.</param>
    /// <returns><see langword="true"/> if the patcher is in use; otherwise, <see langword="false"/>.</returns>
    public bool IsPatcherInUse(string patcherFileName)
    {
        var indexFolder = GetIndexFolderPath();
        if (!Directory.Exists(indexFolder))
            return false;

        // Check if any index file references this patcher
        var searchPattern = $"*_{patcherFileName}.txt";
        return Directory.EnumerateFiles(indexFolder, searchPattern).Any();
    }

    /// <summary>
    /// Gets the total count of mods using a specific patcher.
    /// </summary>
    /// <param name="patcherFileName">The name of the patcher file.</param>
    /// <returns>The number of mods using this patcher, or 0 if not found.</returns>
    public int GetPatcherUsageCount(string patcherFileName)
    {
        var indexFolder = GetIndexFolderPath();
        if (!Directory.Exists(indexFolder))
            return 0;

        // Find all index files for this patcher (from different mods)
        var searchPattern = $"*_{patcherFileName}.txt";
        var totalCount = 0;

        foreach (var indexFile in Directory.EnumerateFiles(indexFolder, searchPattern))
        {
            var content = File.ReadAllText(indexFile);
            if (int.TryParse(content, out var count))
            {
                totalCount += count;
            }
        }

        return totalCount;
    }
}
