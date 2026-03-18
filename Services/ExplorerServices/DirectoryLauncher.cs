using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Avalonia.Platform.Storage;

namespace GottaManagePlus.Services.ExplorerServices;

public class DirectoryLauncher
{
    private ILauncher? _launcher;
    public void RegisterLauncher(ILauncher launcher) => _launcher = launcher;
    
    public async Task<bool> OpenDirectoryInfo(DirectoryInfo directoryInfo)
    {
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
        
        return _launcher != null
            ? await _launcher.LaunchDirectoryInfoAsync(directoryInfo)
            : throw new InvalidOperationException("Launcher has not been registered yet.");
    }
}