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
using Avalonia.Platform.Storage;
using Serilog;

namespace GottaManagePlus.Services.ExplorerServices;

public class DirectoryLauncher(ILogger logger)
{
    private readonly ILogger _logger = logger;
    private ILauncher? _launcher;
    public void RegisterLauncher(ILauncher launcher) => _launcher = launcher;
    
    /// <summary>
    /// Opens a directory in the explorer.
    /// </summary>
    /// <param name="directoryInfo">The directory info to be launched.</param>
    /// <returns><see langword="true"/> if the launch was successful; otherwise, <see langword="false"/>.</returns>
    /// <exception cref="InvalidOperationException">If the launcher hasn't been registered yet.</exception>
    public async Task<bool> OpenDirectoryInfo(DirectoryInfo directoryInfo)
    {
        if (_launcher == null)
        {
            _logger.Error("{Name}'s Launcher is null!", GetType().Name);
            throw new InvalidOperationException("Launcher has not been registered yet.");
        }
        
        // Workaround for issue: https://github.com/AvaloniaUI/Avalonia/issues/20230
        if (OperatingSystem.IsLinux())
        {
            // using xdg-open with Linux
            Process.Start(new ProcessStartInfo
            {
                FileName = "xdg-open",
                Arguments = $"\"{directoryInfo.FullName}\"",
                CreateNoWindow = true,
                UseShellExecute = false
            });
            return true;
        }

        return await _launcher.LaunchDirectoryInfoAsync(directoryInfo);
    }
}