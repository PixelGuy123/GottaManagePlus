using System.Text.Json;
using GottaManagePlus.Models;

using GottaManagePlus.Services.GameEnvironmentServices;
using GottaManagePlus.Utils;
using Serilog;
using ModManifestContext = GottaManagePlus.Utils.SourceGenerators.ModManifestContext;

namespace GottaManagePlus.Services.ModServices;

/// <summary>
/// A class in charge of generating a <see cref="ModManifest"/> instance from a given path.
/// </summary>
public sealed class ManifestLoader(ILogger logger, GameEnvironmentController controller)
{
    // ---- Private -----
    private readonly ILogger _logger = logger;
    private readonly GameEnvironmentController _controller = controller;

    // ---- Public ----
    /// <summary>
    /// Loads a metadata file based on the path: <c>archiveRoot/_gmp/manifest.json</c>.
    /// </summary>
    /// <param name="modRootPath">The file to be handled.</param>
    /// <param name="progress">The progress to be reported back.</param>
    /// <param name="cancellationToken">The token, in case the process is canceled.</param>
    /// <returns>Returns a <see cref="ModManifest"/> with the manifest generated.</returns>
    public async Task<ModManifest?> LoadMetadataAsync(string modRootPath, IProgress<ProgressReport>? progress, CancellationToken cancellationToken = default)
    {
        // Locate .gmp/manifest.json
        var manifestPath = (string)Path.Combine(modRootPath, Constants.App_SpecialFolderForMods_Name, Constants.ModManifestDefaultFileName);
        if (!File.Exists(manifestPath))
        {
            _logger.Warning("Missing manifest: '{path}'", manifestPath);
            return null;
        }

        // Extract JSON data into an object
        try
        {
            // Load the manifest in memory
            progress?.Report(new ProgressReport("Reading manifest file..."));
            _logger.Information("Reading manifest ('{ManifestPath}')...", manifestPath);
            var json = await File.ReadAllTextAsync(manifestPath, cancellationToken);
            var manifest = JsonSerializer.Deserialize(json, ModManifestContext.Default.ModManifest);
            if (manifest == null)
            {
                _logger.Warning("Failed to deserialize metadata (null result).");
                return null;
            }

            // Load the metadata from disk.
            _ = await manifest.LoadMetadataFromDiskAsync(_controller, _logger, cancellationToken);

            // Look for supported versions file
            var dir = Path.GetDirectoryName(manifestPath);

            if (dir == null) return manifest;
            
            var versionFile = Directory
                .EnumerateFiles(dir, $"{Constants.ModSupportForGameVersionPreviewFilePrefixName}*").FirstOrDefault();
            if (versionFile == null) return manifest; // If there's no version file, return

            var fileName = Path.GetFileName(versionFile);
            var versionsPart = fileName[Constants.ModSupportForGameVersionPreviewFilePrefixName.Length..];
            if (!string.IsNullOrEmpty(versionsPart))
            {
                // Use '_' to split each version found
                manifest.Metadata.SupportedPlusVersions = versionsPart.Split('_', StringSplitOptions.RemoveEmptyEntries)
                    .ToList().ConvertAll(str => new WrappedGameVersion(str));
                
                // Update manifest status over Plus version
                var gameVersion = _controller.CurrentEnvironment?.GameVersion ?? new WrappedGameVersion("0.0.0");
                manifest.SupportsCurrentVersion =
                    manifest.Metadata.SupportedPlusVersions.Contains(gameVersion);
            }

            return manifest;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error loading manifest.");
            return null;
        }
    }
}
