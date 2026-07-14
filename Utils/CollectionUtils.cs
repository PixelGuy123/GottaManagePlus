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

using System.Collections;

namespace GottaManagePlus.Utils;

public static class CollectionUtils
{
    /// <summary>
    /// Whether the index is not outside the collection.
    /// </summary>
    /// <param name="list">The collection to be checked.</param>
    /// <param name="index">The index to be checked.</param>
    /// <returns><see langword="true"/> if the index is in bounds; otherwise, <see langword="false"/>.</returns>
    public static bool IsIndexInBounds<T>(this IReadOnlyCollection<T> list, int index) => index >= 0 && index < list.Count;

    /// <summary>
    /// Checks whether an element in <typeparamref name="T[]"/> contains a duplicate element according to its default equality implementation.
    /// </summary>
    /// <param name="array">The array to be analyzed.</param>
    /// <typeparam name="T">The type of the array.</typeparam>
    /// <returns><see langword="true"/> if there is at least one duplicate; otherwise, <see langword="false"/>.</returns>
    public static bool HasDuplicate<T>(this T[] array)
    {
        var seen = new HashSet<T>();
        return array.Any(item => !seen.Add(item));
    }
    
    /// <summary>
    /// Checks whether an element in a <see cref="string"/> array contains a duplicate element according to its default equality implementation.
    /// </summary>
    /// <param name="array">The array to be analyzed.</param>
    /// <param name="comparison">The comparison check for strings.</param>
    /// <returns><see langword="true"/> if there is at least one duplicate; otherwise, <see langword="false"/>.</returns>
    public static bool HasDuplicate(this string[] array, StringComparison comparison)
    {
        for (var i = 0; i < array.Length; i++)
        for (var z = i + 1; z < array.Length; z++)
            if (array[i].Equals(array[z], comparison))
                return true;
        return false;
    }
}