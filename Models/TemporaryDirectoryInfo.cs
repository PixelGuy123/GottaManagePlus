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

namespace GottaManagePlus.Models;

/// <summary>
/// A representation of a temporary <see cref="DirectoryInfo"/> with a disposable pattern.
/// </summary>
public class TemporaryDirectoryInfo(DirectoryInfo directoryInfo, ILogger? logger) : IDisposable
{
    // ---- Private ----
    private readonly ILogger? _logger = logger;
    
    // ---- Public ----
    /// <summary>
    /// The active directory info.
    /// </summary>
    public DirectoryInfo DirectoryInfo { get; } = directoryInfo;
    
    public void Dispose()
    {
        try
        {
            _logger?.Information("Cleaning up temporary directory '{dir}'.", DirectoryInfo.FullName);
            if (DirectoryInfo.Exists) DirectoryInfo.Delete();
        }
        catch { /* Suppress */ }
    }
}