/*
This file is part of GottaManagePlus (https://github.com/PixelGuy123/GottaManagePlus)

    Copyright (C) 2026 PixelGuy123

    This program is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with this program.  If not, see <https://www.gnu.org/licenses/>.

*/

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