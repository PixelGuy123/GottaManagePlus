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

namespace GottaManagePlus.Utils;

/// <summary>
/// Extension class for handling a few edge cases with <see cref="OsPath"/>
/// </summary>
public static class OsPathUtils
{
    /// <summary>
    /// Converts a <see cref="OsPath"/> array to a string array.
    /// </summary>
    /// <param name="ar">The array to be passed.</param>
    /// <param name="useNormalizedPath">If <see langword="true"/>, the paths will be normalized.</param>
    /// <returns>A string array of <see cref="OsPath"/>.</returns>
    public static string[] ToStringArray(this OsPath[] ar, bool useNormalizedPath = false)
    {
        var strArray = new string[ar.Length];
        for (var i = 0; i < ar.Length; i++)
            strArray[i] = useNormalizedPath ? ar[i].NormalizedPath : ar[i].BasePath;
        return strArray;
    }
    
    /// <summary>
    /// Converts a string array to an <see cref="OsPath"/> array.
    /// </summary>
    /// <param name="ar">The string array to be converted.</param>
    /// <returns>An <see cref="OsPath"/> array created from the string array.</returns>
    public static OsPath[] ToOsPathArray(this string[] ar)
    {
        var osPathArray = new OsPath[ar.Length];
        for (var i = 0; i < ar.Length; i++) osPathArray[i] = ar[i];
        return osPathArray;
    }

    /// <summary>
    /// Combines all paths inside given <paramref name="ar"/> into a single <see cref="OsPath"/> instance.
    /// </summary>
    /// <param name="ar">The array to be unified.</param>
    /// <returns>A combined <see cref="OsPath"/> instance.</returns>
    public static OsPath Unite(this OsPath[] ar) => 
        Path.Combine(ar.ToStringArray(useNormalizedPath: false));

    /// <summary>
    /// Combines two <see cref="OsPath"/> instances into one.
    /// </summary>
    /// <param name="left">The left-hand side.</param>
    /// <param name="right">The right-hand side.</param>
    /// <returns>A combined <see cref="OsPath"/> instance.</returns>
    public static OsPath Combine(this OsPath left, OsPath right) => 
        Path.Combine(left.BasePath, right.BasePath);
}