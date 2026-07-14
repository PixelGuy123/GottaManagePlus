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

using GottaManagePlus.Models;
using GottaManagePlus.Utils;
using Serilog;
using SharpCompress.Readers;

namespace GottaManagePlus.Services.ProfileServices.Readers;

public sealed class ProfileExportReader(ILogger logger)
{
    // ---- Private -----
    private readonly ILogger _logger = logger;
    
    // ---- Public ----
    /// <summary>
    /// Reads a <c>.gmpProfile</c> file's metadata compressed in it.
    /// </summary>
    /// <param name="compressedProfilePath">The path to the file.</param>
    /// <returns>An instance of <see cref="ProfileMetadata"/> if the metadata is available in the profile; otherwise, <see langword="null"/>.</returns>
    /// <exception cref="ArgumentException">If the file does not exist.</exception>
    public ProfileMetadata? ReadExportedProfile(string compressedProfilePath)
    {
        // If the file does not exist, throw.
        if (!File.Exists(compressedProfilePath))
            throw new ArgumentException("Compressed profile does not exist.");

        try
        {
            // Attempt to read in-memory the file.
            // If the profile's directory exists, then open it to read.
            using var fileStream = File.OpenRead(compressedProfilePath);
            
            // Make the reader, then write the content to it.
            using var reader = ReaderFactory.OpenReader(fileStream);
            
            // Search for the metadata file.
            while (reader.MoveToNextEntry())
            {
                var entry = reader.Entry;
                if (entry.Key?.Equals(Constants.ProfileMetadataFileName, StringComparison.OrdinalIgnoreCase) != true) continue;
                
                // Found entry, now use a StreamReader to unpack it.
                using var entryStream = reader.OpenEntryStream();
                using var streamReader = new StreamReader(entryStream);
                    
                // Try to make a metadata object out of it.
                return ProfileMetadataUtils.ReadMetadata(streamReader.ReadToEnd());
            }

            // No metadata found, return null.
            return null;
        }
        catch (Exception e)
        {
            _logger.Error(e, "Failed to read exported profile '{profName}'.", 
                Path.GetFileName(compressedProfilePath));
            return null;
        }
    }
}