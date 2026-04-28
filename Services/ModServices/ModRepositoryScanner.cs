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
    /// <param name="profileMetadata">The <see cref="ProfileMetadata"/> instance to be updated. <see langword="null"/> can be used if the intention is a scan-only procedure.</param>
    /// <param name="progress">The progress of the scan.</param>
    /// <param name="ct">The cancellation token in case this operation needs to be canceled.</param>
    /// <returns>A <see cref="Tuple{bool, bool}"/> with two booleans containing the respective meanings:
    /// <list type="number">
    ///     <item><see langword="true"/> means the scanner managed to find, at least, one mod to be loaded in. <see langword="false"/> means no mod has been added.</item>
    ///     <item><see langword="true"/> means mods outside the specific structure expected by the mod manager have been found. <see langword="false"/> means no invalid mod found.</item>
    /// </list>
    /// </returns>
    public async Task<(bool addedAnyMods, bool invalidModsFound)> ScanRepository(ProfileMetadata? profileMetadata, IProgress<ProgressReport>? progress, CancellationToken ct = default)
    {
        // Get BepInEx/Plugins.
        var pluginsPath =
            _controller.SearchAbsolutePath(Constants.BepInExFolderName, Constants.PatchersFolder);

        // Store a backup in case of cancellation.
        var modDataFilesBackup = new List<ModManifest>(profileMetadata?.ModDataFiles ?? []);

        try
        {
            // Clean up the profile's mods.
            profileMetadata?.ModDataFiles.Clear();

            // If directory does not exist, then stop here.
            if (!Directory.Exists(pluginsPath))
            {
                _logger.Warning("Plugins folder is missing, skipping mod repository scan...");
                return (false, false);
            }

            var anyModsFound = false;

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
                    anyModsFound = true;
                    profileMetadata?.ModDataFiles.Add(manifest);
                }
                else _logger.Warning("Failed to add plugin from directory \'{dir}\'.", directory);
            }

            return (anyModsFound, Directory.EnumerateFiles(pluginsPath)
                .Any(f => Path.GetExtension(f) == ".dll")); // If any file with .dll is found, they are located outside their proper environments
        }
        catch (OperationCanceledException)
        {
            // Revert change here.
            profileMetadata?.ModDataFiles = modDataFilesBackup;
            _logger.Warning("Mod scan cancelled!");
            return (false, false);
        }
        catch (Exception e)
        {
            _logger.Error(e, "Something went wrong during mod scan!");
            return (false, false);
        }
    }
}