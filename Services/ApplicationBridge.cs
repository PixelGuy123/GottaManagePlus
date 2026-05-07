using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;

namespace GottaManagePlus.Services;

public class ApplicationBridge
{
    // ---- Private API ----
    internal void SetDesktopEnvironment(IClassicDesktopStyleApplicationLifetime desktop) => _desktop = desktop;
    private IClassicDesktopStyleApplicationLifetime? _desktop;
    
    // ---- Public API ----
    /// <summary>
    /// Exits the application forcefully.
    /// </summary>
    public void Exit() => _desktop?.Shutdown();

    /// <summary>
    /// Freezes the window and does an action while frozen asynchronously.
    /// </summary>
    /// <param name="whileFrozen">The callback invoked while the window is frozen.</param>
    /// <exception cref="NullReferenceException">If <see cref="_desktop"/> is null, this exception is raised.</exception>
    public async Task FreezeWindowAsync(Func<Task> whileFrozen)
    {
        if (_desktop == null)
            throw new NullReferenceException("Desktop is null.");
        
        // Get the top level safely.
        var topLevel = TopLevel.GetTopLevel(_desktop.MainWindow)!;
        topLevel.IsEnabled = false; // freezes toplevel

        // Does job while frozen.
        await whileFrozen();
        
        // Finishes job by enabling window again.
        topLevel.IsEnabled = true;
    }
}

