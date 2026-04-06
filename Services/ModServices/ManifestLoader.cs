using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using GottaManagePlus.Models;
using GottaManagePlus.Models.SourceGenerators;
using GottaManagePlus.Services.GameEnvironmentServices;
using Serilog;

namespace GottaManagePlus.Services.ModServices;

/// <summary>
/// A class in charge of generating a <see cref="ModManifest"/> instance from a given path.
/// </summary>
public sealed class ManifestLoader(ILogger logger, GameEnvironmentController controller)
{
    // ---- Private API -----
    private readonly ILogger _logger = logger;
    private readonly GameEnvironmentController _controller = controller;

    // ---- Public API ----
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        TypeInfoResolver = ModManifestContext.Default
    };

    /// <summary>
    /// Loads a metadata file based on the path: <c>archiveRoot/_gmp/manifest.json</c>.
    /// </summary>
    /// <param name="modRootPath">The file to be handled.</param>
    /// <param name="progress">The progress to be reported back.</param>
    /// <param name="cancellationToken">The token, in case the process is canceled.</param>
    /// <returns>Returns a <see cref="ModManifest"/> with the manifest generated.</returns>
    public async Task<ModManifest?> LoadMetadataAsync(string modRootPath, IProgress<ProgressReport>? progress, CancellationToken cancellationToken = default)
    {
        // Locate _gmp/metadata.json
        var manifestPath = Path.Combine(modRootPath, Constants.App_SpecialFolderForMods_Name, "manifest.json");
        var metadataPath = Path.Combine(modRootPath, Constants.App_SpecialFolderForMods_Name, ".metadata");
        if (!File.Exists(manifestPath))
        {
            _logger.Warning("Missing _gmp{DirectorySeparatorChar}manifest.json", Path.DirectorySeparatorChar);
            return null;
        }

        // Extract JSON data into an object
        try
        {
            progress?.Report(new ProgressReport("Reading manifest file..."));
            var json = await File.ReadAllTextAsync(manifestPath, cancellationToken);
            var manifest = JsonSerializer.Deserialize<ModManifest>(json, JsonOptions);
            if (manifest == null)
            {
                _logger.Warning("Failed to deserialize metadata (null result).");
                return null;
            }
            
            // Look for .metadata file if it is available
            if (File.Exists(metadataPath))
            {
                var newMetadata =
                    JsonSerializer.Deserialize<ModMetadata>(
                        await File.ReadAllTextAsync(metadataPath, cancellationToken), JsonOptions);
                if (newMetadata != null)
                    manifest.Metadata = newMetadata;
            }

            // Look for supported versions file
            var dir = Path.GetDirectoryName(manifestPath);
            if (dir == null) return manifest; // If directory is null, return

            var versionFile = Directory
                .EnumerateFiles(dir, $"{Constants.ModSupportForGameVersionPreviewFilePrefixName}*").FirstOrDefault();
            if (versionFile == null) return manifest; // If there's no version file, return

            var fileName = Path.GetFileName(versionFile);
            var versionsPart = fileName.Substring(Constants.ModSupportForGameVersionPreviewFilePrefixName.Length);
            if (!string.IsNullOrEmpty(versionsPart))
            {
                // Use '_' to split each version found
                manifest.Metadata.SupportedPlusVersions = versionsPart.Split('_', StringSplitOptions.RemoveEmptyEntries)
                    .ToList().ConvertAll(str => new WrappedGameVersion(str));
                
                // Update manifest status over Plus version
                var gameVersion = controller.CurrentEnvironment?.GameVersion ?? new WrappedGameVersion("0.0.0");
                manifest.SupportsCurrentVersion =
                    manifest.Metadata.SupportedPlusVersions.Contains(gameVersion);
            }

            return manifest;
        }
        catch (Exception ex)
        {
            _logger.Error("Error loading manifest.\n{Exception}", ex);
            return null;
        }
    }
}