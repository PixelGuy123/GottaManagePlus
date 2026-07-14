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

using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;

namespace GottaManagePlus.Services;

public class ApplicationManager
{
    // ---- Private ----
    internal void SetDesktopEnvironment(IClassicDesktopStyleApplicationLifetime desktop) => _desktop = desktop;
    private IClassicDesktopStyleApplicationLifetime? _desktop;
    
    // ---- Public ----
    /// <summary>
    /// Exits the application forcefully.
    /// </summary>
    public void Exit() => _desktop?.Shutdown();

    /// <summary>
    /// Freezes the window and does an action while frozen asynchronously.
    /// </summary>
    /// <param name="whileFrozen">The callback invoked while the window is frozen.</param>
    /// <exception cref="NullReferenceException">If <see cref="_desktop"/> is null, this exception is raised.</exception>
    public async Task FreezeWindowAsync(Func<Task> whileFrozen)
    {
        if (_desktop == null)
            throw new NullReferenceException("Desktop is null.");
        
        // Get the top level safely.
        var topLevel = TopLevel.GetTopLevel(_desktop.MainWindow)!;
        topLevel.IsEnabled = false; // freezes toplevel

        // Does job while frozen.
        await whileFrozen();
        
        // Finishes job by enabling window again.
        topLevel.IsEnabled = true;
    }
}

