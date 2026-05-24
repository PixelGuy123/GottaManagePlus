using GottaManagePlus.Models.Exceptions;
using GottaManagePlus.Services.GameEnvironmentServices;
using GottaManagePlus.Utils;
using Serilog;
using SharpCompress.Archives;

namespace GottaManagePlus.Services.ModServices;

/// <summary>
/// A service for extracting mod archives.
/// </summary>
public sealed class ModArchiveExtractor(ILogger logger)
{
    // ---- Private -----
    private readonly ILogger _logger = logger;
    
    // ---- Public ----
    /// <summary>
    /// Extracts an archive to a uniquely named temporary directory.
    /// </summary>
    /// <param name="archivePath">Path to the archive file.</param>
    /// <param name="controller">The controller for folder access.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>Path to the temporary directory containing extracted files.</returns>
    /// <exception cref="ArchiveExtractionException">Thrown when extraction fails.</exception>
    public async Task<string?> ExtractToTempAsync(string archivePath, GameEnvironmentController controller, CancellationToken cancellationToken = default)
    {
        try
        {
            // 1. Get a temporary directory to actually extract the archive.
            _logger.Information("Extractor - Creating sub directory...");
            using var temporaryDirectory =
                controller.CreateTempSubdirectory(_logger);
            _logger.Information("Extractor - Subdirectory created at '{TemporaryDirectoryFullName}'.",
                temporaryDirectory.DirectoryInfo.FullName);

            // 2. Extract everything to that temporary directory.
            _logger.Information("Extractor - Extracting archive to directory...");
            using var archiveExtractor = ArchiveFactory.OpenArchive(archivePath);
            foreach (var entry in archiveExtractor.Entries)
            {
                cancellationToken.ThrowIfCancellationRequested();
                // If null, skip
                if (string.IsNullOrEmpty(entry.Key))
                {
                    _logger.Warning("Extractor - Skipped an invalid entry...");
                    continue;
                }

                _logger.Information("Extractor - Extracting '{EntryKey}' to directory...", entry.Key);

                // Writes the asset into the storage
                await entry.WriteToDirectoryAsync(temporaryDirectory.DirectoryInfo.FullName, cancellationToken: cancellationToken);
            }

            _logger.Information("Extractor - Successfully extracted the assets!");
            return temporaryDirectory.DirectoryInfo.FullName;
        }
        catch (Exception e)
        {
            _logger.Error(e, "Extractor - An error occurred during the extraction.");
            return null;
        }
    }
}