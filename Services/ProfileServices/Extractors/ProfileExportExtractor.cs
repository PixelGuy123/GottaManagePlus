using Serilog;
using SharpCompress.Archives;
using SharpCompress.Common;

namespace GottaManagePlus.Services.ProfileServices.Extractors;

/// <summary>
/// An extractor specialized for <c>.gmpProfile</c> files.
/// </summary>
public sealed class ProfileExportExtractor(ILogger logger)
{
    // ---- Private -----
    private readonly ILogger _logger = logger;
    
    // ---- Public ----
    /// <summary>
    /// Extracts all contents from a <c>.gmpProfile</c> file to the specified destination directory.
    /// </summary>
    /// <param name="destinationPath">The directory where the profile contents will be extracted.</param>
    /// <param name="exportedProfilePath">The file path to the <c>.gmpProfile</c> archive.</param>
    /// <returns><see langword="true"/> if the extraction completes successfully; otherwise, <see langword="false"/>.</returns>
    public bool ExtractExportedProfile(string destinationPath, string exportedProfilePath)
    {
        if (string.IsNullOrWhiteSpace(exportedProfilePath) || !File.Exists(exportedProfilePath))
        {
            _logger.Warning("Exported profile path is invalid or file does not exist: {path}", exportedProfilePath);
            return false;
        }
        
        try
        {
            // Ensure the target directory exists prior to extraction.
            Directory.CreateDirectory(destinationPath);

            // Open the archive. ArchiveFactory automatically detects the compression format.
            using var archive = ArchiveFactory.OpenArchive(exportedProfilePath);
            
            foreach (var entry in archive.Entries)
            {
                // Skip directory entries; file extraction handles folder structure automatically.
                if (entry.IsDirectory) continue;
                
                // Attempt to write and overwrite
                entry.WriteToDirectory(destinationPath, new ExtractionOptions
                {
                    ExtractFullPath = true,
                    Overwrite = true
                });
            }

            return true;
        }
        catch (Exception e)
        {
            _logger.Error(e, "Failed to extract exported profile '{profName}' to '{destPath}'.", 
                Path.GetFileName(exportedProfilePath), destinationPath);
            return false;
        }
    }
}