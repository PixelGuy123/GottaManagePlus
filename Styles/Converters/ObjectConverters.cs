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

public static class ObjectConverters
{
    public static readonly FuncValueConverter<object?, double> ObjectNotNullToOpacityIndicator =
        new(obj =>
        {
            if (obj is string str)
                return !string.IsNullOrEmpty(str) ? 1d : 0.5d;
            return obj != null ? 1d : 0.5d;
        });
}