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

using System.Diagnostics;
using Avalonia.Data.Converters;

// ReSharper disable PossibleMultipleEnumeration

namespace GottaManagePlus.Styles.Converters;

public static class EqualityConverters
{
    /// <summary>
    /// Checks whether all arguments inside the array are equal to each other.
    /// </summary>
    public static readonly FuncMultiValueConverter<object?, bool> AllEqual =
        new(values =>
        {
            var first = values.FirstOrDefault();
            return values.Skip(1).All(v => first?.Equals(v) == true);
        });
    
    /// <summary>
    /// Checks whether at least one of the arguments inside the array are not equal to each other.
    /// </summary>
    public static readonly FuncMultiValueConverter<object?, bool> AtLeastOneNotEqual =
        new(values =>
        {
            var first = values.FirstOrDefault();
            return values.Skip(1).Any(v => first?.Equals(v) != true);
        });
    
    /// <summary>
    /// Checks whether at least one of the arguments inside the array are not equal to each other. Returns a <see langword="double"/>.
    /// </summary>
    public static readonly FuncMultiValueConverter<object?, double> AtLeastOneNotEqualDouble =
        new(values =>
        {
            var first = values.FirstOrDefault();
            return values.Skip(1).Any(v => first?.Equals(v) != true) ? 1.0 : 0.0;
        });

    /// <summary>
    /// Checks whether number is higher than one.
    /// </summary>
    public static readonly FuncValueConverter<int, bool> IsHigherThanOne =
        new(num => num > 1);
    
    /// <summary>
    /// Checks whether number is higher than one (Opacity mode).
    /// </summary>
    public static readonly FuncValueConverter<int, double> IsHigherThanOne_Opacity =
        new(num => num > 1 ? 1 : 0.5d);
}