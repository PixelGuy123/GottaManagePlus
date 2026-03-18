using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using GottaManagePlus.Models.Exceptions;
using Serilog;
using Serilog.Core;
using SharpCompress.Archives;
using ProgressReport = GottaManagePlus.Models.ProgressReport;

namespace GottaManagePlus.Services.ModServices;

/// <summary>
/// A service for extracting mod archives.
/// </summary>
public static class ModArchiveExtractor
{
    /// <summary>
    /// Extracts an archive to a uniquely named temporary directory.
    /// </summary>
    /// <param name="archivePath">Path to the archive file.</param>
    /// <param name="logger">The logger that reports back the changes in this method.</param>
    /// <param name="progress">The progress to be reported back.</param>
    /// <returns>Path to the temporary directory containing extracted files.</returns>
    /// <exception cref="ArchiveExtractionException">Thrown when extraction fails.</exception>
    public static async Task<string?> ExtractToTempAsync(string archivePath, Logger logger, IProgress<ProgressReport>? progress)
    {
        try
        {
            // 1. Get a temporary directory to actually extract the archive.
            logger.Information("Extractor - Creating sub directory...");
            var temporaryDirectory =
                Directory.CreateTempSubdirectory($"GMP_{Path.GetFileNameWithoutExtension(archivePath)}_ModExtraction");
            logger.Information("Extractor - Subdirectory created at '{TemporaryDirectoryFullName}'.",
                temporaryDirectory.FullName);

            // 2. Extract everything to that temporary directory.
            logger.Information("Extractor - Extracting archive to directory...");
            using var archiveExtractor = ArchiveFactory.OpenArchive(archivePath);
            foreach (var entry in archiveExtractor.Entries)
            {
                // If null, skip
                if (string.IsNullOrEmpty(entry.Key))
                {
                    logger.Warning("Extractor - Skipped an invalid entry...");
                    continue;
                }

                logger.Information("Extractor - Extracting '{EntryKey}' to directory...", entry.Key);

                // Writes the asset into the storage
                await entry.WriteToDirectoryAsync(temporaryDirectory.FullName);
            }

            logger.Information("Extractor - Successfully extracted the assets!");
            return temporaryDirectory.FullName;
        }
        catch (Exception e)
        {
            logger.Error("Extractor - An error occurred during the extraction: {ToString}", e.ToString());
            return null;
        }
    }
}