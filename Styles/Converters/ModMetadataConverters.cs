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

using Avalonia.Data.Converters;
using Avalonia.Media.Imaging;
using GottaManagePlus.Models;
using GottaManagePlus.Utils;

namespace GottaManagePlus.Styles.Converters;

public static class ModMetadataConverters
{
    /// <summary>
    /// Gets a Converter that turns a <see cref="ModMetadata"/> data into a stringified version.
    /// </summary>
    public static FuncValueConverter<ModMetadata, string> MetadataToStringifiedVersion { get; } = 
        new(metadata =>
        {
            if (metadata == null || metadata.SupportedPlusVersions.Count == 0)
                return "0.0.0";
            return metadata.SupportedPlusVersions[^1].ToString();
        });

    /// <summary>
    /// Gets a converter that attempts to load the thumbnail from a <see cref="ModMetadata"/>.
    /// </summary>
    public static FuncValueConverter<ModMetadata, Bitmap?> MetadataToThumbnail { get; } =
        new(metadata => metadata?.GetThumbnailImageAsBitmap());
}