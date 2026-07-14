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

public static class PathUtils
{
    /// <summary>
    /// Turn the whole file name given into a name the OS accepts (based on <see cref="Path.GetInvalidPathChars()"/>).
    /// </summary>
    /// <param name="fileNameForCleanUp">The name to be changed.</param>
    /// <returns>A new filtered name.</returns>
    public static string TurnFileNameLegal(string fileNameForCleanUp)
    {
        // New string.
        var newStr = fileNameForCleanUp.ToCharArray();
        
        // Get invalid characters.
        var invalidChars = Path.GetInvalidPathChars();
        
        // Ensure the name of the file is legal.
        for (var i = 0; i < newStr.Length; i++)
            if (invalidChars.Contains(newStr[i]))
                newStr[i] = '_';
        
        return new string(newStr);
    }
}