using System.Diagnostics;
using Avalonia.Platform.Storage;
using Serilog;

namespace GottaManagePlus.Services.ExplorerServices;

public class DirectoryLauncher(ILogger logger)
{
    private readonly ILogger _logger = logger;
    private ILauncher? _launcher;
    public void RegisterLauncher(ILauncher launcher) => _launcher = launcher;
    
    /// <summary>
    /// Opens a directory in the explorer.
    /// </summary>
    /// <param name="directoryInfo">The directory info to be launched.</param>
    /// <returns><see langword="true"/> if the launch was successful; otherwise, <see langword="false"/>.</returns>
    /// <exception cref="InvalidOperationException">If the launcher hasn't been registered yet.</exception>
    public async Task<bool> OpenDirectoryInfo(DirectoryInfo directoryInfo)
    {
        if (_launcher == null)
        {
            _logger.Error("{Name}'s Launcher is null!", GetType().Name);
            throw new InvalidOperationException("Launcher has not been registered yet.");
        }
        
        // Workaround for issue: https://github.com/AvaloniaUI/Avalonia/issues/20230
        if (OperatingSystem.IsLinux())
        {
            // using xdg-open with Linux
            Process.Start(new ProcessStartInfo
            {
                FileName = "xdg-open",
                Arguments = $"\"{directoryInfo.FullName}\"",
                CreateNoWindow = true,
                UseShellExecute = false
            });
            return true;
        }

        return await _launcher.LaunchDirectoryInfoAsync(directoryInfo);
    }
}