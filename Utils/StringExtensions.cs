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

namespace GottaManagePlus.Utils;

public static class StringExtensions
{
    /// <summary>
    /// Determines the number of consecutive matching characters from the start of two strings.
    /// </summary>
    /// <param name="s1">The first string to compare.</param>
    /// <param name="s2">The second string to compare.</param>
    /// <returns>
    /// The count of characters that match sequentially from the beginning of both strings.
    /// Returns 0 if either string is null or empty.
    /// Returns the length of the shorter string if all characters match up to that point.
    /// </returns>
    /// <example>
    /// <code>
    /// "HelloWorld".ManyStartWith("Hello") // Returns 5
    /// "Test".ManyStartWith("Testing")     // Returns 4
    /// "ABC".ManyStartWith("ABD")          // Returns 2
    /// "".ManyStartWith("Anything")        // Returns 0
    /// </code>
    /// </example>
    public static int ManyStartWith(this string s1, string s2)
    {
        if (string.IsNullOrEmpty(s1) || string.IsNullOrEmpty(s2))
            return 0;
        
        for (var i = 0; i < s1.Length && i < s2.Length; i++)
        {
            if (s1[i] != s2[i])
                return i;
        }
        return Math.Min(s1.Length, s2.Length);
    }
}