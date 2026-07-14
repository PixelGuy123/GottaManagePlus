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

using System.Text.Json;

namespace GottaManagePlus.Utils;

public static class JsonDocumentUtils
{
    /// <summary>
    /// Gets a DateTime from a Unix timestamp stored in the <see cref="JsonElement"/>.
    /// </summary>
    /// <param name="rootElement">The <see cref="JsonElement"/> containing the Unix timestamp value.</param>
    /// <returns>A <see cref="DateTime"/> in UTC representing the converted Unix timestamp.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the <see cref="JsonElement"/> is not a number or cannot be converted to Int64.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the Unix timestamp is outside the valid range for <see cref="DateTimeOffset"/> (years 0001-9999).</exception>
    public static DateTime GetUnixDateTime(this JsonElement rootElement) =>
        DateTimeOffset.FromUnixTimeSeconds(rootElement.GetInt64()).UtcDateTime;
}