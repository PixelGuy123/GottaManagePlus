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
using GottaManagePlus.Services.GameEnvironmentServices;
using GottaManagePlus.Utils;
using Serilog;
using SharpCompress.Common;
using SharpCompress.Compressors.Deflate;
using SharpCompress.Writers;

namespace GottaManagePlus.Services.ProfileServices.Writers;

/// <summary>
/// A <c>.gmpProfile</c> generator for the profiles.
/// </summary>
public sealed class ProfileExporter(ILogger logger)
{
    public const ArchiveType ArchiveType = SharpCompress.Common.ArchiveType.Zip;
    
    // ----- Private -----
    private readonly ILogger _logger = logger;
    
    // ----- Public -----
    /// <summary>
    /// Exports a profile in the <c>.gmpProfile</c> format.
    /// </summary>
    /// <param name="exportPath">The path to export the profile to.</param>
    /// <param name="profile">The <see cref="ProfileMetadata"/> to be exported.</param>
    /// <param name="controller">The controller to safely search the desired export path.</param>
    /// <exception cref="IOException">Throws if the directory to the profile does not exist.</exception>
    public void ExportProfileTo(string exportPath, ProfileMetadata profile, GameEnvironmentController controller)
    {
        try
        {
            // Get the path of the profile.
            var physicalPath = profile.GetPhysicalPath(controller);

            // Get the profile's directory.
            var profileDir = new DirectoryInfo(physicalPath);
            if (!profileDir.Exists) throw new IOException("Profile directory does not exist.");
            
            // If the profile's directory exists, then zip it up in a custom extension.
            using var fileStream = File.OpenWrite(
                             (string)Path.Combine(exportPath, $"{profile.Name}{Constants.ExportedProfileExtension}"));
            
            // Make the writer, then write the content to it.
            using var writer = WriterFactory.OpenWriter(fileStream, ArchiveType,
                WriterOptions.ForZip());
            
            _logger.Information("Exporting profile to '{dir}'...", profileDir.FullName);
            // Write the directory to the zip file.
            writer.WriteAll(profileDir.FullName, "*", SearchOption.AllDirectories);
            _logger.Information("Successfully exported profile to '{dir}'", profileDir.FullName);
        }
        catch (Exception e)
        {
            _logger.Error(e, "Failed to export profile '{profName}' to '{path}'.", profile.Name, exportPath);
        }
    }
}