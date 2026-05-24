using Serilog;
using SharpCompress.Writers;
using SharpCompress.Common;
using System.Text.Json;
using GottaManagePlus.Models.SourceGenerators;
using System.Text;
using GottaManagePlus.Models;
using GottaManagePlus.Services.GameEnvironmentServices;
using GottaManagePlus.Utils;
using ProgressReport = GottaManagePlus.Models.ProgressReport;

namespace GottaManagePlus.Services.ModServices;

public class ModArchiveGenerator(ILogger logger, GameEnvironmentController controller)
{
    // ---- Private ----
    private readonly ILogger _logger = logger;
    private readonly GameEnvironmentController _controller = controller;

    /// <summary>
    /// Generates a compressed archive (GZip format) containing plugin files, asset directories, and a manifest file.
    /// </summary>
    /// <param name="pluginPaths">
    /// Array of absolute file paths to plugin files to be included in the archive.
    /// </param>
    /// <param name="assetPaths">
    /// Array of DestinedAsset objects containing LocalPath (source directory) and Destination (target relative path) pairs.
    /// </param>
    /// <param name="archiveDestination">
    /// Absolute file path where the generated archive will be saved.
    /// </param>
    /// <param name="progress">
    /// Progress reporter for the archive generation process.
    /// </param>
    /// <param name="cancellationToken">
    /// Cancellation token to cancel the operation.
    /// </param>
    /// <returns>
    /// <see langword="true"/> if the archive was successfully generated and written; otherwise, <see langword="false"/> if an exception occurred.
    /// </returns>
    /// <exception cref="ArgumentException">
    /// Thrown when duplicate entries exist in <paramref name="pluginPaths"/> (case-insensitive comparison)
    /// or <paramref name="assetPaths"/>.
    /// </exception>
    public async Task<bool> GenerateArchive(string[] pluginPaths, DestinedAsset[] assetPaths, string archiveDestination, IProgress<ProgressReport> progress, CancellationToken cancellationToken)
    {
        try
        {
            // Validate that there are no duplicates.
            if (pluginPaths.HasDuplicate(StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException("There is a duplicate element in the plugins array.", nameof(pluginPaths));
            if (assetPaths.HasDuplicate())
                throw new ArgumentException("There is a duplicate element in the assets array.", nameof(assetPaths));
            
            // Create a new ModManifest instance.
            var manifest = new ModManifest
            {
                Name = Path.GetFileNameWithoutExtension(pluginPaths[0]),
                Version = "0.0.0",
                Author = "Unknown",
                Description = "A manually imported plugin! What does it has? We don't know!",
                Plugins = [.. pluginPaths.Select(p => Path.GetFileName(p))],
                Assets = []
            };
            
            // Generate the assets for the manifest as well.
            foreach (var assetDir in assetPaths)
            {
                if (!Directory.Exists(assetDir.LocalPath) || !Directory.Exists(assetDir.Destination))
                {
                    _logger.Warning("'{DestinedAsset}' lacks an existent LocalPath or a Destination.", assetDir);
                    continue;
                }
                
                // Correct the localPath and destination to be relative.
                // * Assumes Destination is located at root path of the current environment.
                manifest.Assets.Add(new DestinedAsset
                {
                    LocalPath = Path.GetFileName(assetDir.LocalPath),
                    Destination = _controller.SearchRelativePath(assetDir.Destination)
                });
            }
            
            // Create lists storing [original absolute path, generated relative paths] for the zip writing.
            List<(string, DestinedAsset)> updatedPluginPairs = [
                .. pluginPaths.Select((t, i) => (t, new DestinedAsset { LocalPath = manifest.Plugins[i] })),
            ];
            List<(string, DestinedAsset)> updatedAssetPairs = [
                .. assetPaths.Select((t, i) => (t.LocalPath, manifest.Assets[i])),
            ];

            var totalTasks = updatedPluginPairs.Count + updatedAssetPairs.Count + 1; // +1 for manifest
            var completedTasks = 0;

            progress.Report(new ProgressReport(completedTasks, totalTasks, "Archive", "Starting generation"));

            await using (var stream = File.OpenWrite(archiveDestination))
            await using (var writer = await WriterFactory.OpenAsyncWriter(stream, ArchiveType.SevenZip, new WriterOptions(CompressionType.LZMA), cancellationToken))
            {
                cancellationToken.ThrowIfCancellationRequested();

                // Add plugins.
                foreach (var (pluginOriginalPath, pluginAsset) in updatedPluginPairs)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    // New Path and original path defined.
                    var pluginNewPath = pluginAsset.LocalPath;
                    
                    if (File.Exists(pluginOriginalPath))
                    {
                        await writer.WriteAsync(pluginNewPath, pluginOriginalPath, cancellationToken: cancellationToken);
                        _logger.Information("Added plugin '{plugin}' to archive.", pluginOriginalPath);
                    }
                    else
                    {
                        _logger.Warning("Plugin file '{plugin}' does not exist.", pluginOriginalPath);
                    }

                    completedTasks++;
                    progress.Report(new ProgressReport(completedTasks, totalTasks, "Archive", $"Added plugin: {Path.GetFileName(pluginOriginalPath)}"));
                }

                // Add assets.
                foreach (var (assetOriginalPath, newDestinedAsset) in updatedAssetPairs)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    if (Directory.Exists(assetOriginalPath))
                    {
                        await writer.WriteAsync(newDestinedAsset.Destination!, assetOriginalPath, cancellationToken: cancellationToken);
                        
                        _logger.Information("Added asset directory '{dir}' to archive.", assetOriginalPath);
                    }
                    else
                    {
                        _logger.Warning("Asset directory '{dir}' does not exist.", assetOriginalPath);
                    }

                    completedTasks++;
                    progress.Report(new ProgressReport(completedTasks, totalTasks, "Archive", $"Added asset: {Path.GetFileName(assetOriginalPath)}"));
                }

                cancellationToken.ThrowIfCancellationRequested();

                // Add manifest.
                var json = JsonSerializer.Serialize(manifest, ModManifestContext.Default.ModManifest);
                using (var jsonStream = new MemoryStream(Encoding.UTF8.GetBytes(json)))
                {
                    await writer.WriteAsync($"{Constants.App_SpecialFolderForMods_Name}/manifest.json", jsonStream, cancellationToken: cancellationToken);
                    _logger.Information("Added manifest to archive.");
                }
                
                // Add version metadata.
                var metadataFileName =
                    $"{Constants.ModSupportForGameVersionPreviewFilePrefixName}{_controller.CurrentEnvironment!.GameVersion}";
                
                using (var emptyStream = new MemoryStream())
                {
                    await writer.WriteAsync($"{Constants.App_SpecialFolderForMods_Name}/{metadataFileName}", emptyStream, cancellationToken: cancellationToken);
                    _logger.Information("Writing version metadata '{meta}'", metadataFileName);
                }

                completedTasks++;
                progress.Report(new ProgressReport(completedTasks, totalTasks, "Archive", "Added manifest"));
            }

            _logger.Information("Archive generated successfully at '{destination}'.", archiveDestination);
            return true;
        }
        catch (OperationCanceledException)
        {
            _logger.Information("Archive generation was cancelled.");
            throw;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to generate archive.");
            // Delete the partially created archive if it exists
            if (!File.Exists(archiveDestination)) return false;
            
            try
            {
                File.Delete(archiveDestination);
                _logger.Information("Deleted partially created archive at '{destination}'.", archiveDestination);
            }
            catch { /* Suppress */}
            return false;
        }
    }
}