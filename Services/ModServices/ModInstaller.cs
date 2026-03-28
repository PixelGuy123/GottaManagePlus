using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GottaManagePlus.Models;
using GottaManagePlus.Models.UI;
using GottaManagePlus.Services.PlusFolderServices;
using Serilog;

namespace GottaManagePlus.Services.ModServices;

/// <summary>
/// Coordinates the complete end-to-end process of installing a mod from a compressed archive file
/// into the target game folder.
/// </summary>
public class ModInstaller
{
    public static async Task<ModInstallationResult> InstallModAsync(
        string archivePath,
        PlusFolderBrowser browser,
        IProgress<ProgressReport>? progress = null,
        CancellationToken cancellationToken = default)
    {
        // 1. Create a logger for this phase.
        await using var modLogger = new LoggerConfiguration()
#if DEBUG
            .WriteTo.Console()
#endif
            .WriteTo.File(Path.Combine(Constants.ApplicationLocation, "Logs", "ModInstallation_" + DateTime.Now.ToLongTimeString() + ".log"))
            .CreateLogger();
        
        // Log initiation
        modLogger.Information("Initiating installation of {modName}...", 
            Path.GetFileNameWithoutExtension(archivePath));
        
        // Create a temporary dir
        string? temporaryDirectory = null;
        var results = new ModInstallationResult();
        
        try
        {
            // 2. First, we need to physically extract the archive to somewhere, so that
            // file manipulation can be performed.
            temporaryDirectory = await ModArchiveExtractor.ExtractToTempAsync(archivePath, modLogger, progress);

            cancellationToken.ThrowIfCancellationRequested(); // Between each step, a cancellation token check is done.
            
            // It wasn't a success, so return earlier
            if (string.IsNullOrEmpty(temporaryDirectory))
            {
                modLogger.Warning("Directory is empty! Failed to generate a directory for the archive.");
                return results;
            }

            // 3. Then, we ought to generate a manifest representation of the mod to understand its structure.
            var manifest =
                await ManifestLoader.LoadMetadataAsync(temporaryDirectory, modLogger, progress, cancellationToken);

            if (manifest == null)
            {
                modLogger.Warning("Manifest is null!");
                return results;
            }
            
            cancellationToken.ThrowIfCancellationRequested(); // Between each step, a cancellation token check is done
            
            // 4. After the manifest is scanned, we can start by checking the plugins and assets:
            // do they contain any suspicious files?
            // TODO: Add an option to forcefully cancel the installation in case security scan gets something weird.
            await SecurityScanner.ScanAsync(temporaryDirectory, results, modLogger, progress, manifest, cancellationToken);
            
            // 5. After scanning, after getting manifest, we have everything ready:
            // now move the files (told by the manifest) to the right places.
            ResourceInstaller.InstallResources(temporaryDirectory, modLogger, browser, manifest);
            
            // 6. Yay!
            modLogger.Information("Installation done with success!");
        }
        catch (OperationCanceledException)
        {
            modLogger.Warning("Mod installation canceled!");
        }
        catch (Exception e)
        {
            modLogger.Error("Unknown error broke the installation!\n{exception}", e);
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