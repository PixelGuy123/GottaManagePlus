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

namespace GottaManagePlus.Styles.Converters;

public static class FontConverters
{
    /// <summary>
    /// Gets a Converter that takes a string as input and converts it into a size estimation.
    /// </summary>
    public static FuncValueConverter<string, double> Max20TextToSizeConverter { get; } = 
        new(str => 
            string.IsNullOrEmpty(str) ? 20d : 
            Math.Max(10d, 20d - (str.Length - 1) * 0.35d));
}