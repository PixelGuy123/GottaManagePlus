using System;
using System.IO;
using Avalonia.Controls;
using GottaManagePlus.Factories;
using GottaManagePlus.Interfaces.GameEnvironment;
using GottaManagePlus.Interfaces.ProfileManagement;
using GottaManagePlus.Models;
using GottaManagePlus.Services;
using GottaManagePlus.Services.APIServices;
using GottaManagePlus.Services.ExplorerServices;
using GottaManagePlus.Services.GameEnvironmentServices;
using GottaManagePlus.Services.ModServices;
using GottaManagePlus.Services.ProfileServices;
using GottaManagePlus.Services.ProfileServices.Extractors;
using GottaManagePlus.Services.ProfileServices.Management;
using GottaManagePlus.Services.ProfileServices.Readers;
using GottaManagePlus.Services.ProfileServices.Writers;
using GottaManagePlus.ViewModels;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

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
        
        // Profile Services
        collection.AddSingleton<ProfileManager>();
        collection.AddSingleton<ProfileRepository>();
        
        // Mod Services
        collection.AddSingleton<ResourceInstaller>();
        collection.AddSingleton<SecurityScanner>();
        collection.AddSingleton<ModArchiveExtractor>();
        collection.AddSingleton<ManifestLoader>();
        
        // Game Environment Setup
        collection.AddSingleton<IGameEnvironmentFactory, PlusEnvironmentFactory>();
        collection.AddSingleton<GameEnvironmentController>();
        
        // Logging Setup
        Log.Logger = new LoggerConfiguration()
#if DEBUG
            .WriteTo.Console()
#endif
            .WriteTo.File(Path.Combine(Constants.ApplicationLocation, "Logs", DateTime.Now.ToLongTimeString() + ".log"))
            .CreateLogger();
        collection.AddSingleton(Log.Logger);
        
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
        collection.AddTransient<GamebananaApiService>();
        
        // Profile Management
        // * Essential Manager Services
        collection.AddTransient<IEnvironmentToLocalParser, EnvironmentToProfileSaver>();
        collection.AddTransient<ILocalToEnvironmentParser, ProfileToEnvironmentExtractor>();
        collection.AddTransient<IProfileStorageScanner, LocalProfileStorageScanner>();
        collection.AddTransient<IProfileCreator, LocalProfileCreator>();
        collection.AddTransient<IProfileExportController, ProfileExportController>();
        collection.AddTransient<IProfileDestructor, LocalProfileDestructor>();
        collection.AddTransient<IProfileCloner, LocalProfileCloner>();
        
        // * Sub-Services utilized by the other interfaces
        // ** Default Profile Services
        collection.AddTransient<ProfileZipWriter>();
        collection.AddTransient<ProfileZipReader>();
        collection.AddTransient<ProfileZipExtractor>();
        // ** Export Profile Services
        collection.AddTransient<ProfileExporter>();
        collection.AddTransient<ProfileExportReader>();
        collection.AddTransient<ProfileExportExtractor>();
        
        // Mod Services
        collection.AddTransient<ModInstaller>();
        collection.AddTransient<ModUnInstaller>();
    }

    private static void SetupScopedServices(ServiceCollection collection)
    {
        // Setup Gamebanana Client
        collection.AddHttpClient("GameBanana", client =>
        {
            client.BaseAddress = new Uri("https://gamebanana.com/apiv11");
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(
                new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
        });
    }

    private static void SetupServicesForWindowAttributes(ServiceProvider services, TopLevel window)
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