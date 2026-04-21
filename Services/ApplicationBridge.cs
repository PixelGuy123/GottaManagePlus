using Avalonia.Controls.ApplicationLifetimes;

namespace GottaManagePlus.Services;

public class ApplicationBridge
{
    // ---- Private API ----
    internal void SetDesktopEnvironment(IClassicDesktopStyleApplicationLifetime desktop) => _desktop = desktop;
    private  IClassicDesktopStyleApplicationLifetime? _desktop;
    
    // ---- Public API ----
    public void Exit() => _desktop?.Shutdown();
}