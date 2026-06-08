using Avalonia;
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
