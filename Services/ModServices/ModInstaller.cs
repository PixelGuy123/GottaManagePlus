using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GottaManagePlus.Models;
using GottaManagePlus.Models.UI;
using GottaManagePlus.Services.GameEnvironmentServices;
using GottaManagePlus.Services.ProfileServices;
using Serilog;

namespace GottaManagePlus.Services.ModServices;

/// <summary>
/// Coordinates the complete end-to-end process of installing a mod from a compressed archive file
/// into the target game folder.
/// </summary>
public sealed class ModInstaller(
    ILogger logger,
    ModArchiveExtractor modArchiveExtractor,
    ManifestLoader manifestLoader,
    SecurityScanner securityScanner,
    ResourceInstaller resourceInstaller,
    ProfileManager profileManager,
    GameEnvironmentController controller)
{
    // ---- Private API ----
    private readonly ILogger _logger = logger;
    private readonly ModArchiveExtractor _modArchiveExtractor = modArchiveExtractor;
    private readonly ManifestLoader _manifestLoader = manifestLoader;
    private readonly SecurityScanner _securityScanner = securityScanner;
    private readonly ResourceInstaller _resourceInstaller = resourceInstaller;
    private readonly ProfileManager _profileManager = profileManager;
    private readonly GameEnvironmentController _controller = controller;

    // ---- Public API ----
    /// <summary>
    /// Installs a mod into the respective environment's mod folder.
    /// </summary>
    /// <param name="archivePath">The path to be extracted.</param>
    /// <param name="progress">The progress report.</param>
    /// <param name="cancellationToken">The token to cancel this entire operation.</param>
    /// <returns>Returns an instance of <see cref="ModInstallationResult"/> with the report of the installation.</returns>
    public async Task<ModInstallationResult> InstallModArchiveAsync(
        string archivePath,
        IProgress<ProgressReport>? progress = null,
        CancellationToken cancellationToken = default)
    {
        // Log initiation
        _logger.Information("Initiating installation of {modName}...", 
            Path.GetFileNameWithoutExtension(archivePath));
        
        // Create a temporary dir
        string? temporaryDirectory = null;
        var results = new ModInstallationResult();
        
        try
        {
            // 2. First, we need to physically extract the archive to somewhere, so that
            // file manipulation can be performed.
            temporaryDirectory = await _modArchiveExtractor.ExtractToTempAsync(archivePath, _controller, cancellationToken);

            cancellationToken.ThrowIfCancellationRequested(); // Between each step, a cancellation token check is done.
            
            // It wasn't a success, so return earlier
            if (string.IsNullOrEmpty(temporaryDirectory))
            {
                _logger.Warning("Directory is empty! Failed to generate a directory for the archive.");
                return results;
            }

            // 3. Then, we ought to generate a manifest representation of the mod to understand its structure.
            var manifest =
                await _manifestLoader.LoadMetadataAsync(temporaryDirectory, progress, cancellationToken);

            if (manifest == null)
            {
                _logger.Warning("Manifest is null!");
                return results;
            }
            
            cancellationToken.ThrowIfCancellationRequested(); // Between each step, a cancellation token check is done
            
            // 4. After the manifest is scanned, we can start by checking the plugins and assets:
            // do they contain any suspicious files?
            // TODO: Add a global option to forcefully cancel the installation in case security scan gets something weird.
            var isSafeToLoad = await _securityScanner.ScanAsync(temporaryDirectory, _controller, results, progress, manifest, cancellationToken);

            // If the manifest is unsafe, cancel it.
            if (!isSafeToLoad)
            {
                _logger.Warning("The manifest is unsafe! Canceling installation...");
                return results;
            }

            // 5. After scanning, after getting manifest, we have everything ready;
            // now, move the files (exposed by the manifest) to the right places.
            _resourceInstaller.InstallResources(temporaryDirectory, manifest);
            
            // 6. Register the mod to the available profile.
            var currentProfile = _profileManager.ActiveProfile;
            if (currentProfile != null && results.Metadata != null)
                currentProfile.ModDataFiles.Add(results.Metadata);
            _logger.Information("Installation done with success!");
        }
        catch (OperationCanceledException)
        {
            _logger.Warning("Mod installation canceled!");
        }
        catch (Exception e)
        {
            _logger.Error("Unknown error broke the installation!\n{exception}", e);
        }
        finally // On the end, always delete the temporary directory
        {
            try
            {
                if (Directory.Exists(temporaryDirectory))
                    Directory.Delete(temporaryDirectory, true);
            }
            catch
            {
                // suppression
            }
        }
        
        return results;
    }
}