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

public static class FileUtils
{
    /// <summary>
    /// Determines whether the specified Unix file has execute permissions for the user, group, or others.
    /// </summary>
    /// <param name="fileInfo">A <see cref="FileInfo"/> object representing the file to check.</param>
    /// <returns><see langword="true"/> if any execute flag (User, Group, or Other) is set; otherwise, <see langword="false"/>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="fileInfo"/> is null.</exception>
    /// <remarks>
    /// This method relies on the <see cref="UnixFileMode"/> property, which is primarily supported on Unix-like operating systems.
    /// </remarks>
    public static bool CheckIfUnixFileIsExecutable(FileInfo fileInfo)
    {
        var mode = fileInfo.UnixFileMode;
        
        // Check if it has an executable permission
        return mode.HasFlag(UnixFileMode.UserExecute) || 
               mode.HasFlag(UnixFileMode.GroupExecute) || 
               mode.HasFlag(UnixFileMode.OtherExecute);
    }

    /// <summary>
    /// Determines whether the file at the specified path has Unix execute permissions.
    /// </summary>
    /// <param name="filePath">The path to the file.</param>
    /// <returns><see langword="true"/> if the file has execute permissions; otherwise, <see langword="false"/>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="filePath"/> is null.</exception>
    /// <exception cref="System.Security.SecurityException">The caller does not have the required permission.</exception>
    /// <exception cref="UnauthorizedAccessException">Access to <paramref name="filePath"/> is denied.</exception>
    public static bool CheckIfUnixFileIsExecutable(string filePath) =>
        CheckIfUnixFileIsExecutable(new FileInfo(filePath));
}