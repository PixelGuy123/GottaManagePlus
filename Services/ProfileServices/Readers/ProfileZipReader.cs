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

namespace GottaManagePlus.Services.ProfileServices.Readers;

public sealed class ProfileZipReader(ILogger logger)
{
    // ---- Private -----
    private readonly ILogger _logger = logger;
    
    // ---- Public ----
    /// <summary>
    /// Reads a directory of a profile and converts its metadata (if available) into a <see cref="ProfileMetadata"/>.
    /// The path shall never be prompted by a user.
    /// </summary>
    /// <param name="profileRootDirectory">The directory root to be scanned.</param>
    /// <returns>An instance of <see cref="ProfileMetadata"/> with its path defined.</returns>
    /// <exception cref="ArgumentException">If the path is not a directory, this exception is raised.</exception>
    /// <exception cref="NullReferenceException">If the metadata is null, this error is raised.</exception>
    public ProfileMetadata? ReadProfile(string profileRootDirectory)
    {
        // If the path is a file, throw an error
        if (!File.GetAttributes(profileRootDirectory).HasFlag(FileAttributes.Directory))
            throw new ArgumentException("Given path is not a directory.");

        // Directory must exist obviously
        if (!Directory.Exists(profileRootDirectory))
            throw new IOException("Directory is missing.");

        try
        {
            // Profile Structure:
            // [ProfileName] / << Method shall assume it is in here
            //      [MetadataFile]
            //      [Profile.zip]

            // Try to search for the metadata file
            var metadataFile = new FileInfo((string)Path.Combine(profileRootDirectory, Constants.ProfileMetadataFileName));

            // If the file is missing, throw an error
            return !metadataFile.Exists ? null :
                // Try to read metadata
                ProfileMetadataUtils.ReadMetadata(File.ReadAllText(metadataFile.FullName));
        }
        catch (Exception e)
        {
            _logger.Error(e, "Failed to read the profile content.");
            return null;
        }
    }
}