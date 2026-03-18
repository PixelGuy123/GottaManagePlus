using System;
using Avalonia.Controls;
using GottaManagePlus.Factories;
using GottaManagePlus.Interfaces;
using GottaManagePlus.Models;
using GottaManagePlus.Services;
using GottaManagePlus.Services.ExplorerServices;
using GottaManagePlus.Services.ModServices;
using GottaManagePlus.Services.PlusFolderServices;
using GottaManagePlus.Services.ProfileServices;
using GottaManagePlus.ViewModels;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace GottaManagePlus;

public partial class App
{
    private void SetupConfiguration(ServiceCollection collection)
    {
        // Configuration Setup
        var config = new ConfigurationBuilder()
            .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
            .AddJsonFile("AppSettings.json", optional: false, reloadOnChange: true)
            .Build();
        collection.AddSingleton<IConfiguration>(config);
        collection.Configure<AppSettings>(config.GetSection(nameof(AppSettings)));
    }

    private static void SetupViewModels(ServiceCollection collection)
    {
        // View Models
        collection.AddSingleton<MainWindowViewModel>(); // Singleton
        collection.AddTransient<MyModsViewModel>(); // Transient means the instance only exists when requested and destroys itself when not used
        collection.AddTransient<SettingsViewModel>();
        collection.AddTransient<ProfilesViewModel>();
    }

    private static void SetupSingletonServices(ServiceCollection collection)
    {
        // Services
        collection.AddSingleton<PageFactory>();
        collection.AddSingleton<DialogService>();
        collection.AddSingleton<SettingsService>();
        collection.AddSingleton<PlusFolderDb>();
        collection.AddSingleton<ProfileProvider>();
        collection.AddSingleton<ProfileStorage>();
        
        // Factory Function
        collection.AddSingleton<Func<Type, PageViewModel>>(
            serviceProvider => type =>
                !type.IsAssignableTo(typeof(PageViewModel)) ? 
                    throw new NotImplementedException("Non PageViewModel supported.") :
                    (PageViewModel)serviceProvider.GetRequiredService(type));
    }

    private static void SetupTransientServices(ServiceCollection collection)
    {
        // Transient Services
        collection.AddTransient<FilePicker>();
        collection.AddTransient<DirectoryPicker>();
        collection.AddTransient<FileLauncher>();
        collection.AddTransient<DirectoryLauncher>();
        collection.AddTransient<ProfileManager>();
        collection.AddTransient<PlusFolderBrowser>();
    }

    private static void SetupServicesForWindowAttributes(ServiceProvider services, Window window)
    {
        // Assign storage providers
        var filesService = services.GetRequiredService<FilePicker>();
        filesService.RegisterProvider(window.StorageProvider);
        var directoryService = services.GetRequiredService<DirectoryPicker>();
        directoryService.RegisterProvider(window.StorageProvider);
            
        // Assign launchers
        var fileLauncher = services.GetRequiredService<FileLauncher>();
        fileLauncher.RegisterLauncher(window.Launcher);
        var directoryLauncher = services.GetRequiredService<DirectoryLauncher>();
        directoryLauncher.RegisterLauncher(window.Launcher);
    }
}