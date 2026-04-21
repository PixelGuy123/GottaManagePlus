using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GottaManagePlus.Models;
using GottaManagePlus.Services.GameEnvironmentServices;
using GottaManagePlus.Services.ProfileServices;
using GottaManagePlus.Utils;
using Serilog;

namespace GottaManagePlus.Services.ModServices;

/// <summary>
/// A service responsible for scanning BepInEx/Plugins folder and filling it up to the currently active profile.
/// </summary>
public sealed class ModRepositoryScanner(
    ILogger logger,
    GameEnvironmentController controller,
    ManifestLoader manifestLoader)
{
    // ---- Private API ----
    private readonly ILogger _logger = logger;
    private readonly GameEnvironmentController _controller = controller;
    private readonly ManifestLoader _manifestLoader = manifestLoader;

    // ---- Public API ----
    /// <summary>
    /// Scans <c>BepInEx/Plugins</c> folder and adds all readable mods from the folder.
    /// </summary>
    /// <param name="profileMetadata">The <see cref="ProfileMetadata"/> instance to be updated.</param>
    /// <param name="progress">The progress of the scan.</param>
    /// <param name="ct">The cancellation token in case this operation needs to be canceled.</param>
    public async Task ScanRepository(ProfileMetadata profileMetadata, IProgress<ProgressReport>? progress, CancellationToken ct = default)
    {
        // Get BepInEx/Plugins.
        var pluginsPath =
            _controller.SearchAbsolutePath(Constants.BepInExFolderName, Constants.PatchersFolder);

        // Store a backup in case of cancellation.
        var modDataFilesBackup = new List<ModManifest>(profileMetadata.ModDataFiles);

        try
        {
            // Clean up the profile's mods.
            profileMetadata.ModDataFiles.Clear();

            // If directory does not exist, then stop here.
            if (!Directory.Exists(pluginsPath))
            {
                _logger.Warning("Plugins folder is missing, skipping mod repository scan...");
                return;
            }

            // Go through each folder in the path.
            foreach (var directory in Directory.EnumerateDirectories(pluginsPath))
            {
                // Attempt to load the mod's manifest if there's one.
                var manifest = await _manifestLoader.LoadMetadataAsync(directory, progress, ct);
                ct.ThrowIfCancellationRequested();

                // If the manifest exists, add it to the profile again.
                if (manifest != null)
                {
                    _logger.Information("Adding \'{Name}\' to the profile.", manifest.Name);
                    profileMetadata.ModDataFiles.Add(manifest);
                }
                else _logger.Warning("Failed to add plugin from directory \'{dir}\'.", directory);
            }
        }
        catch (OperationCanceledException)
        {
            // Revert change here.
            profileMetadata.ModDataFiles = modDataFilesBackup;
            _logger.Warning("Mod scan cancelled!");
        }
        catch (Exception e)
        {
            _logger.Error("Something went wrong during mod scan!\n{ex}", e);
        }
    }
}