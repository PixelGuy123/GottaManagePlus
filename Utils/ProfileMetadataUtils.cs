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
using Tomlyn;
using ProfileMetadataContext = GottaManagePlus.Utils.SourceGenerators.ProfileMetadataContext;

namespace GottaManagePlus.Utils;

/// <summary>
/// A class with utilities for interacting with <see cref="ProfileMetadata"/>.
/// </summary>
public static class ProfileMetadataUtils
{
    /// <summary>
    /// Attempts to read the metadata content (TOML format) and return back an instance of <see cref="ProfileMetadata"/>.
    /// </summary>
    /// <param name="tomlContent">The content used by the <see cref="TomlSerializer"/>.</param>
    /// <returns>An instance of <see cref="ProfileMetadata"/>.</returns>
    public static ProfileMetadata? ReadMetadata(string tomlContent) =>
        TomlSerializer.Deserialize(tomlContent, ProfileMetadataContext.Default.ProfileMetadata);

    /// <param name="metadata">The instance to be serialized.</param>
    extension(ProfileMetadata metadata)
    {
        /// <summary>
        /// Serializes the <see cref="ProfileMetadata"/> into a TOML table.
        /// </summary>
        /// <returns>A <see cref="string"/> containing all the content of the metadata.</returns>
        public string Serialize() =>
            TomlSerializer.Serialize(metadata, ProfileMetadataContext.Default.ProfileMetadata);

        /// <summary>
        /// Returns the physical path of the <see cref="ProfileMetadata"/>.
        /// </summary>
        /// <param name="controller">The controller to indicate the correct location.</param>
        /// <returns>A <see cref="string"/> of the correct location of the <see cref="ProfileMetadata"/>.</returns>
        public string GetPhysicalPath(GameEnvironmentController controller) => 
            controller.SearchAbsolutePath(controller.GetOrCreateProfilesFolderPath(), metadata.Name);
    }
}