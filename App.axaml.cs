using System;
using System.Linq;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Xaml;
using GottaManagePlus.Factories;
using GottaManagePlus.Models;
using GottaManagePlus.Services;
using GottaManagePlus.ViewModels;
using GottaManagePlus.Views;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace GottaManagePlus;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        var collection = new ServiceCollection();
        
        // Configuration Setup
        var config = new ConfigurationBuilder()
            .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
            .AddJsonFile("AppSettings.json", optional: false, reloadOnChange: true)
            .Build();
        collection.AddSingleton<IConfiguration>(config);
        collection.Configure<AppSettings>(config.GetSection(nameof(AppSettings)));
        
        // Services
        collection.AddSingleton<PageFactory>();
        collection.AddSingleton<DialogService>();
        collection.AddSingleton<FilesService>();
        collection.AddSingleton<SettingsService>();
        collection.AddSingleton<PlusFolderViewer>();
        collection.AddSingleton<ProfileProvider>();
        
        // View Models
        collection.AddSingleton<MainWindowViewModel>(); // Singleton
        collection.AddTransient<MyModsViewModel>(); // Transient means the instance only exists when requested and destroys itself when not used
        collection.AddTransient<SettingsViewModel>();
        collection.AddTransient<ProfilesViewModel>();
        
        // Factory Function
        collection.AddSingleton<Func<Type, PageViewModel>>(
            serviceProvider => type =>
                !type.IsAssignableTo(typeof(PageViewModel)) ? 
                    throw new NotImplementedException("Non PageViewModel supported.") :
                    (PageViewModel)serviceProvider.GetRequiredService(type));

        var services = collection.BuildServiceProvider();
        
        // Setup Services
        // * Assigns the MainWindowViewModel to the Dialog Service
        services.GetRequiredService<DialogService>()
            .RegisterProvider(services.GetRequiredService<MainWindowViewModel>());

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // Avoid duplicate validations from both Avalonia and the CommunityToolkit. 
            // More info: https://docs.avaloniaui.net/docs/guides/development-guides/data-validation#manage-validationplugins
            DisableAvaloniaDataAnnotationValidation();
            desktop.MainWindow = new MainWindow
            {
                DataContext = services.GetRequiredService<MainWindowViewModel>(),
            };
            
            // Assign services dependent on the MainWindow
            var filesService = services.GetRequiredService<FilesService>();
            filesService.RegisterProvider(desktop.MainWindow.StorageProvider);
            filesService.RegisterLauncher(desktop.MainWindow.Launcher);
        }

        base.OnFrameworkInitializationCompleted();
    }

    private static void DisableAvaloniaDataAnnotationValidation()
    {
        // Get an array of plugins to remove
        var dataValidationPluginsToRemove =
            BindingPlugins.DataValidators.OfType<DataAnnotationsValidationPlugin>().ToArray();

        // remove each entry found
        foreach (var plugin in dataValidationPluginsToRemove)
        {
            BindingPlugins.DataValidators.Remove(plugin);
        }
    }
}