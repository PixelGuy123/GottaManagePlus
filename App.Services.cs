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
using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace GottaManagePlus;

public partial class App
{
    private static void SetupViewModels(ServiceCollection collection)
    {
        // View Models
        collection.AddSingleton<MainWindowViewModel>(); // Singleton
        collection.AddTransient<MyModsViewModel>(); // Transient means the instance only exists when requested and destroys itself when not used
        collection.AddTransient<SettingsViewModel>();
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
        
        // Game Environment Setup
        collection.AddSingleton<IGameEnvironmentFactory, PlusEnvironmentFactory>();
        collection.AddSingleton<GameEnvironmentController>();
        
        // IO Services that need TopLevel Access
        collection.AddSingleton<FilePicker>();
        collection.AddSingleton<DirectoryPicker>();
        collection.AddSingleton<FileLauncher>();
        collection.AddSingleton<DirectoryLauncher>();
        
        // Application Service
        collection.AddSingleton<ApplicationManager>();
        
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

    private static void SetupScopedServices(ServiceCollection collection)
    {
        // Setup Gamebanana Client
        collection.AddHttpClient("GameBanana", client =>
        {
            client.BaseAddress = new Uri("https://gamebanana.com/");
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(
                new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json", 1.0d));
            client.DefaultRequestHeaders.Accept.Add(
                new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("images/*", 0.9d));
        });
        
        collection.AddScoped<GamebananaApiService>();
        
        // Game Environment Management
        collection.AddScoped<IGameEnvironmentSnapshotWriter, GameEnvironmentSnapshotWriter>();
        collection.AddScoped<IGameEnvironmentSnapshotReader, GameEnvironmentSnapshotReader>();
        collection.AddScoped<IGameEnvironmentSnapshotComparer, GameEnvironmentSnapshotComparer>();
        
        // Profile Management
        // * Essential Manager Services
        collection.AddScoped<IEnvironmentToLocalParser, EnvironmentToProfileSaver>();
        collection.AddScoped<ILocalToEnvironmentParser, ProfileToEnvironmentExtractor>();
        collection.AddScoped<IProfileStorageScanner, LocalProfileStorageScanner>();
        collection.AddScoped<IProfileCreator, LocalProfileCreator>();
        collection.AddScoped<IProfileExportController, ProfileExportController>();
        collection.AddScoped<IProfileDestructor, LocalProfileDestructor>();
        collection.AddScoped<IProfileCloner, LocalProfileCloner>();
        
        // * Sub-Services utilized by the other interfaces
        // ** Default Profile Services
        collection.AddScoped<ProfileZipWriter>();
        collection.AddScoped<ProfileZipReader>();
        collection.AddScoped<ProfileZipExtractor>();
        // ** Export Profile Services
        collection.AddScoped<ProfileExporter>();
        collection.AddScoped<ProfileExportReader>();
        collection.AddScoped<ProfileExportExtractor>();
        
        // Mod Services
        collection.AddScoped<ModInstaller>();
        collection.AddScoped<ModUnInstaller>();
        collection.AddScoped<ModRepositoryScanner>();
        collection.AddScoped<ModArchiveGenerator>();
        collection.AddScoped<ResourceInstaller>();
        collection.AddScoped<SecurityScanner>();
        collection.AddScoped<ModArchiveExtractor>();
        collection.AddScoped<ManifestLoader>();
        collection.AddScoped<ModActivator>();
        
        // Dialogs
        collection.AddScoped<AppInfoDialogViewModel>();
        collection.AddScoped<ConfirmDialogViewModel>();
        collection.AddScoped<CreateProfileDialogViewModel>();
        collection.AddScoped<LicenseDialogViewModel>();
        collection.AddScoped<LoadingDialogViewModel>();
        collection.AddScoped<ModSelectionDialogViewModel>();
        collection.AddScoped<MultiLoadingDialogViewModel>();
    }

    private static void SetupServicesForWindowAttributes(ServiceProvider services, IClassicDesktopStyleApplicationLifetime desktop, TopLevel window)
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
        
        // Assign Application bridge
        var appBridge = services.GetRequiredService<ApplicationManager>();
        appBridge.SetDesktopEnvironment(desktop);
    }
}
