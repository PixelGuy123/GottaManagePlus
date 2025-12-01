using System;
using System.Linq;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Xaml;
using Avalonia.Styling;
using GottaManagePlus.Factories;
using GottaManagePlus.ViewModels;
using GottaManagePlus.Views;
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
        collection.AddSingleton<MainWindowViewModel>(); // Singlet
        collection.AddTransient<MyModsViewModel>(); // Transient means the instance only exists when requested and destroys itself when not used
        collection.AddTransient<SettingsViewModel>();

        collection.AddSingleton<Func<PageNames, PageViewModel>>(
            serviceProvider =>
            name =>
            name switch
            {
                PageNames.Home => serviceProvider.GetRequiredService<MyModsViewModel>(),
                PageNames.Settings => serviceProvider.GetRequiredService<SettingsViewModel>(),
                _ => throw new NotImplementedException("PageNames value is not supported!")
            });

        collection.AddSingleton<PageFactory>();

        var services = collection.BuildServiceProvider();

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // Avoid duplicate validations from both Avalonia and the CommunityToolkit. 
            // More info: https://docs.avaloniaui.net/docs/guides/development-guides/data-validation#manage-validationplugins
            DisableAvaloniaDataAnnotationValidation();
            desktop.MainWindow = new MainWindow
            {
                DataContext = services.GetRequiredService<MainWindowViewModel>(),
            };
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