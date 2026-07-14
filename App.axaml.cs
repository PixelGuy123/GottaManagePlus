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

using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Xaml;
using GottaManagePlus.Services;
using GottaManagePlus.ViewModels;
using GottaManagePlus.Views;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace GottaManagePlus;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
#if DEBUG
        this.AttachDeveloperTools();
#endif
    }

    public override void OnFrameworkInitializationCompleted()
    {
        // Collection creation
        var collection = new ServiceCollection();
        
        // Services to set up
        SetupSingletonServices(collection);
        SetupScopedServices(collection);
        SetupViewModels(collection);

        // Build service provider
        var services = collection.BuildServiceProvider();
        
        // Setup Services
        // * Assigns the MainWindowViewModel to the Dialog Service
        services.GetRequiredService<DialogService>()
            .RegisterProvider(services.GetRequiredService<MainWindowViewModel>());

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow
            {
                DataContext = services.GetRequiredService<MainWindowViewModel>(),
            };

            desktop.Exit += (_, _) =>
            {
                // Ensure the logger is closed beforehand
                Log.CloseAndFlush();
            };

            // FileService, ApplicationManager, etc.
            SetupServicesForWindowAttributes(services, desktop,TopLevel.GetTopLevel(desktop.MainWindow)!);
            
            // Setup Custom Schema
            if (this.TryGetFeature<IActivatableLifetime>() is { } activatableLifetime)
                SetupHandle(activatableLifetime);
        }

        base.OnFrameworkInitializationCompleted();
    }
}