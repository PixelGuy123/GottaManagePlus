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

﻿using Avalonia;
using Avalonia.Threading;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;

namespace GottaManagePlus;

sealed class Program
{
    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static void Main(string[] args)
    {
        // Single instance implementation
        using var mutex = new Mutex(true, AppInfo.MutexName, out var isFirstInstance);
        if (!isFirstInstance)
        {
            ShowAlreadyRunningPopup();
            return;
        }
        
        // Create classic app
        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
    }
    
    // Helper for displaying a message box
    private static void ShowAlreadyRunningPopup()
    {
        // TODO: Implement localization here too
        var box = MessageBoxManager.GetMessageBoxStandard("Already Running",
            "An instance is already running.",
            ButtonEnum.Ok,
            Icon.Warning);
        
        // Run a minimal Avalonia application just to show this popup
        AppBuilder.Configure<Application>()
            .UsePlatformDetect()
            .SetupWithoutStarting();
        
        // Shows asynchronously
        Dispatcher.UIThread.Invoke(box.ShowAsync);
    }

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .With(new SkiaOptions
            {
                MaxGpuResourceSizeBytes = 256 * 1024 * 1024, // 256 MB
                UseOpacitySaveLayer = true // Allow Svgs to have transparency
            })
            .WithInterFont()
            .LogToTrace();
}
