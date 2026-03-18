using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using GottaManagePlus.Models;
using GottaManagePlus.Models.JsonContext;
using GottaManagePlus.Models.SourceGenerators;

namespace GottaManagePlus.Services.ModServices;

/// <summary>
/// A class in charge of generating a <see cref="ModManifest"/> instance from a given path.
/// </summary>
public static class MetadataLoader
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        TypeInfoResolver = ModManifestContext.Default
    };

    /// <summary>
    /// Loads a metadata file based on the path: <c>archiveRoot/_gmp/metadata.json</c>.
    /// </summary>
    /// <param name="modRootPath">The file to be handled.</param>
    /// <param name="progress">The progress to be reported back.</param>
    /// <param name="cancellationToken">The token, in case the process is canceled.</param>
    /// <returns>Returns a <see cref="MetadataLoadResult"/> with the results of the process.</returns>
    public static async Task<MetadataLoadResult> LoadMetadataAsync(string modRootPath, InstallationProgressManager progress, CancellationToken cancellationToken = default)
    {
        var result = new MetadataLoadResult();
        
        // Make progress data
        progress.SetProgressData(InstallationProgress.Step.LoadingMetadata, "Loading metadata...");

        // Locate _gmp/metadata.json
        var metadataPath = Path.Combine(modRootPath, Constants.App_SpecialFolderForMods_Name, "metadata.json");
        if (!File.Exists(metadataPath))
        {
            result.Warnings.Add($"Missing _gmp{Path.DirectorySeparatorChar}metadata.json");
            return result;
        }

        // Extract JSON data into an object
        try
        {
            progress.Report(0.5f, metadataPath);
            var json = await File.ReadAllTextAsync(metadataPath, cancellationToken);
            var metadata = JsonSerializer.Deserialize<ModManifest>(json, JsonOptions);
            if (metadata == null)
            {
                result.Warnings.Add("Failed to deserialize metadata (null result).");
                return result;
            }

            // Do NOT modify metadata here (no destination assignment)
            result.Metadata = metadata;

            // Look for supported versions file
            var dir = Path.GetDirectoryName(metadataPath);
            if (dir == null) return result; // If directory is null, return

            var versionFile = Directory
                .EnumerateFiles(dir, $"{Constants.ModSupportForGameVersionPreviewFilePrefixName}*").FirstOrDefault();
            if (versionFile == null) return result; // If there's no version file, return

            var fileName = Path.GetFileName(versionFile);
            var versionsPart = fileName.Substring(Constants.ModSupportForGameVersionPreviewFilePrefixName.Length);
            if (!string.IsNullOrEmpty(versionsPart))
            {
                // Use '_' to split each version found
                result.SupportedVersions = versionsPart.Split('_', StringSplitOptions.RemoveEmptyEntries);
            }

            return result;
        }
        catch (Exception ex)
        {
            result.Warnings.Add($"Error loading metadata: {ex.Message}");
            return result;
        }
        finally
        {
            progress.Report(1f);
        }
    }
}