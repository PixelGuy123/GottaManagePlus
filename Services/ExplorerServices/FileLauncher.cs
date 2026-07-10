using System.Diagnostics;
using Avalonia.Platform.Storage;
using Serilog;

namespace GottaManagePlus.Services.ExplorerServices;

public class FileLauncher(ILogger logger)
{
    private readonly ILogger _logger = logger;
    private ILauncher? _launcher;
    public void RegisterLauncher(ILauncher launcher) => _launcher = launcher;

    /// <summary>
    /// Attempts to launch a <see cref="FileInfo"/> into the OS.
    /// </summary>
    /// <param name="fileInfo">The <see cref="FileInfo"/> instance to be launched.</param>
    /// <returns><see langword="true"/> if the launch was successful; otherwise, <see langword="false"/>.</returns>
    /// <exception cref="InvalidOperationException">If the launcher hasn't been registered yet.</exception>
    public async Task<bool> TryLaunchFileInfo(FileInfo fileInfo)
    {
        if (_launcher != null) return await _launcher.LaunchFileInfoAsync(fileInfo);
        
        _logger.Error("{Name}'s Launcher is null!", GetType().Name);
        throw new InvalidOperationException("Launcher has not been registered yet.");
    }

    /// <summary>
    /// Attempts to open a <see cref="FileInfo"/> into the explorer.
    /// </summary>
    /// <param name="fileInfo">The <see cref="FileInfo"/> instance to be launched.</param>
    /// <returns><see langword="true"/> if the launch was successful; otherwise, <see langword="false"/>.</returns>
    /// <exception cref="InvalidOperationException">If the launcher hasn't been registered yet.</exception>
    public bool OpenFileInfo(FileInfo fileInfo)
    {
        if (_launcher == null)
        {
            _logger.Error("{Name}'s Launcher is null!", GetType().Name);
            throw new InvalidOperationException("Launcher has not been registered yet.");
        }
        
        if (OperatingSystem.IsWindows())
        {
            // Windows
            Process.Start("explorer.exe", $"/select,\"{fileInfo.FullName}\"");
            return true;
        }
        if (OperatingSystem.IsMacOS())
        {
            // macOS
            Process.Start("open", $"-R \"{fileInfo.FullName}\"");
            return true;
        }
        if (OperatingSystem.IsLinux())
        {
            try
            {
                // Try common file managers with select capability
                string[] fileManagers =
                [
                    "nautilus",
                    "dolphin",
                    "caja"
                ];
            
                // Go through each explorer to see which one works
                foreach (var manager in fileManagers)
                {
                    try
                    {
                        // Console.WriteLine($"Trying {manager} --select \"{fileInfo.FullName}\"");
                        Process.Start(new ProcessStartInfo
                        {
                            FileName = manager,
                            Arguments = $"--select \"{fileInfo.FullName}\"",
                            CreateNoWindow = true,
                            UseShellExecute = false
                        });
                        return true;
                    }
                    catch
                    {
                        // Ignores the exception and tries the next manager
                    }
                }
            
                // Fallback: Just open the directory
                Process.Start(new ProcessStartInfo
                {
                    FileName = "xdg-open",
                    Arguments = $"\"{fileInfo.DirectoryName}\"",
                    CreateNoWindow = true,
                    UseShellExecute = false
                });
                return true;
            }
            catch (Exception ex)
            {
                Log.Logger.Error("Failed to open file explorer: {ExMessage}", ex.Message);
                return false;
            }
        }

        return false;
    }
}