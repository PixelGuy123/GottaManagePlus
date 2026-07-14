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

using Serilog;
using GottaManagePlus.Models;

namespace GottaManagePlus.Utils;

/// <summary>
/// A static class that serves utilities for manipulating directories.
/// </summary>
public static class DirectoryUtils
{
    /// <summary>
    /// Attempts to get the <see cref="DirectoryInfo"/> or create it if it doesn't exist in storage.
    /// </summary>
    /// <param name="path">The path for the directory's info.</param>
    /// <returns>An instance of a locally existent <see cref="DirectoryInfo"/>.</returns>
    public static DirectoryInfo GetOrCreate(string path)
    {
        var dirInfo =
            new DirectoryInfo(path);
        if (!dirInfo.Exists) dirInfo.Create(); // Try and create directory.
        return dirInfo;
    }

    /// <summary>
    /// Moves all the content inside <paramref name="rootDirPath"/> safely. If a directory or file exists, it is replaced accordingly.
    /// </summary>
    /// <param name="rootDirPath">The directory path to be moved.</param>
    /// <param name="destDirName">The destination path.</param>
    public static void AtomicallyMoveTo(string rootDirPath, string destDirName) =>
        new DirectoryInfo(rootDirPath).AtomicallyMoveTo(destDirName);

    /// <param name="directoryInfo">The directory to be moved.</param>
    extension(DirectoryInfo directoryInfo)
    {
        /// <summary>
        /// Moves all the content from given <see cref="DirectoryInfo"/> safely. If a directory or file exists, it is replaced accordingly.
        /// </summary>
        /// <param name="destDirName">The destination path.</param>
        public void AtomicallyMoveTo(string destDirName)
        {
            // Guard against moving a directory onto itself
            if (string.Equals(directoryInfo.FullName, (string)Path.GetFullPath(destDirName), StringComparison.OrdinalIgnoreCase))
                throw new IOException("Cannot move directory to itself.");

            // If destination exists as a file, delete it (replace with directory)
            if (File.Exists(destDirName))
                File.Delete(destDirName);
        
            // If destination directory does not exist, simple move works
            if (!Directory.Exists(destDirName))
            {
                directoryInfo.MoveTo(destDirName);
                return;
            }

            // Destination is an existing directory – merge contents

            // 1. Move all files from source root, replacing if needed
            foreach (var file in directoryInfo.EnumerateFiles())
            {
                var destFilePath = (string)Path.Combine(destDirName, file.Name);

                // If a directory blocks the file path, delete it
                if (Directory.Exists(destFilePath))
                    Directory.Delete(destFilePath, recursive: true);

                // Move file (overwrites existing file)
                File.Move(file.FullName, destFilePath, overwrite: true);
            }

            // 2. Recursively move all subdirectories
            foreach (var subDir in directoryInfo.GetDirectories())
            {
                var destSubDirPath = (string)Path.Combine(destDirName, subDir.Name);

                // If a file blocks the subdirectory path, delete it
                if (File.Exists(destSubDirPath))
                    File.Delete(destSubDirPath);

                // Recursively merge the subdirectory
                subDir.AtomicallyMoveTo(destSubDirPath);
            }

            // 3. Delete the now-empty source directory
            directoryInfo.Delete();
        }

        /// <summary>
        /// Moves the content of the <paramref name="directoryInfo"/> to a destined location.
        /// </summary>
        /// <param name="destDirName">The path to the new directory.</param>
        /// <param name="logger">The logger for the content.</param>
        public void MoveInnerContentTo(string destDirName, ILogger? logger = null)
        {
            // Guard against moving a directory onto itself
            if (string.Equals(directoryInfo.FullName, (string)Path.GetFullPath(destDirName), StringComparison.OrdinalIgnoreCase))
                throw new IOException("Cannot move directory to itself.");
        
            // 1. Move all files from source root, replacing if needed
            foreach (var file in directoryInfo.EnumerateFiles())
            {
                var destFilePath = (string)Path.Combine(destDirName, file.Name);

                // If a directory blocks the file path, delete it
                if (Directory.Exists(destFilePath))
                    Directory.Delete(destFilePath, recursive: true);

                // Move file (overwrites existing file)
                File.Move(file.FullName, destFilePath, overwrite: true);
                logger?.Information("Moved file '{FileFullName}' to '{DestFilePath}'", file.FullName, destFilePath);
            }

            // 2. Recursively move all subdirectories
            foreach (var subDir in directoryInfo.GetDirectories())
            {
                var destSubDirPath = (string)Path.Combine(destDirName, subDir.Name);

                // If a file blocks the subdirectory path, delete it
                if (File.Exists(destSubDirPath))
                    File.Delete(destSubDirPath);

                // Recursively merge the subdirectory
                subDir.MoveTo(destSubDirPath);
                logger?.Information("Moved directory '{FileFullName}' to '{DestFilePath}'", subDir.FullName, destSubDirPath);
            }

            // 3. Delete the now-empty source directory
            directoryInfo.Delete();
        }
    }
}