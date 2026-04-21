using System;
using System.IO;
using System.Linq;
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
        SetupTransientServices(collection);
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

            SetupServicesForWindowAttributes(services, desktop,TopLevel.GetTopLevel(desktop.MainWindow)!);
        }

        base.OnFrameworkInitializationCompleted();
    }
}