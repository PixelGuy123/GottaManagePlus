using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Avalonia.Platform.Storage;

namespace GottaManagePlus.Services.ExplorerServices;

public class FileLauncher
{
    private ILauncher? _launcher;
    public void RegisterLauncher(ILauncher launcher) => _launcher = launcher;

    public async Task<bool> OpenFileInfo(FileInfo fileInfo)
    {
        if (_launcher == null) throw new InvalidOperationException("Launcher has not been registered yet.");
        return await _launcher.LaunchFileInfoAsync(fileInfo);
        
        //
        // if (OperatingSystem.IsWindows())
        // {
        //     // Windows
        //     Process.Start("explorer.exe", $"/select,\"{fileInfo.FullName}\"");
        //     return true;
        // }
        // if (OperatingSystem.IsMacOS())
        // {
        //     // macOS
        //     Process.Start("open", $"-R \"{fileInfo.FullName}\"");
        //     return true;
        // }
        // if (OperatingSystem.IsLinux())
        // {
        //     try
        //     {
        //         // Try common file managers with select capability
        //         string[] fileManagers =
        //         [
        //             "nautilus",
        //             "dolphin",
        //             "caja"
        //         ];
        //     
        //         // Go through each explorer to see which one works
        //         foreach (var manager in fileManagers)
        //         {
        //             try
        //             {
        //                 // Console.WriteLine($"Trying {manager} --select \"{fileInfo.FullName}\"");
        //                 Process.Start(new ProcessStartInfo
        //                 {
        //                     FileName = manager,
        //                     Arguments = $"--select \"{fileInfo.FullName}\"",
        //                     CreateNoWindow = true,
        //                     UseShellExecute = false
        //                 });
        //                 return true;
        //             }
        //             catch
        //             {
        //                 // Ignores the exception and tries the next manager
        //             }
        //         }
        //     
        //         // Fallback: Just open the directory
        //         Process.Start(new ProcessStartInfo
        //         {
        //             FileName = "xdg-open",
        //             Arguments = $"\"{fileInfo.DirectoryName}\"",
        //             CreateNoWindow = true,
        //             UseShellExecute = false
        //         });
        //         return true;
        //     }
        //     catch (Exception ex)
        //     {
        //         Debug.WriteLine($"Failed to open file explorer: {ex.Message}", Constants.DebugError);
        //         return false;
        //     }
        // }

        // return false;
    }
}