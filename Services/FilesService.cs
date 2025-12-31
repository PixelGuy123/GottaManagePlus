using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Avalonia.Platform.Storage;
using GottaManagePlus.Interfaces;

namespace GottaManagePlus.Services;

/// <summary>
/// Implements file operations using an injected <see cref="IStorageProvider"/> and <see cref="ILauncher"/>.
/// </summary>
public class FilesService : IFilesService
{
    private IStorageProvider? _storageProvider;
    public void RegisterProvider(IStorageProvider provider) => _storageProvider = provider;
    private ILauncher? _launcher;
    public void RegisterLauncher(ILauncher launcher) => _launcher = launcher;

    /// <inheritdoc/>
    /// <exception cref="InvalidOperationException">Thrown if the storage provider is not registered.</exception>
    public async Task<IStorageFile?> OpenFileAsync(string? title = null, string? suggestedFileName = null, string? preselectedPath = null, params FilePickerFileType[] filterChoices)
    {
        if (_storageProvider == null) throw new InvalidOperationException("StorageProvider has not been registered yet.");

        IStorageFolder? folder = null;
        if (!string.IsNullOrEmpty(preselectedPath))
            folder = await _storageProvider.TryGetFolderFromPathAsync(preselectedPath);
        
        var files = await _storageProvider.OpenFilePickerAsync(new FilePickerOpenOptions()
        {
            Title = title,
            AllowMultiple = false,
            SuggestedStartLocation = folder,
            FileTypeFilter = filterChoices,
            SuggestedFileName = suggestedFileName
        });

        return files.Count >= 1 ? files[0] : null;
    }

    /// <inheritdoc/>
    /// <exception cref="InvalidOperationException">Thrown if the storage provider is not registered.</exception>
    public async Task<IStorageFile?> SaveFileAsync(string? title = null, string? suggestedFileName = null, string? preselectedPath = null, params FilePickerFileType[] filterChoices)
    {
        if (_storageProvider == null) throw new InvalidOperationException("StorageProvider has not been registered yet.");
        
        IStorageFolder? folder = null;
        if (!string.IsNullOrEmpty(preselectedPath))
            folder = await _storageProvider.TryGetFolderFromPathAsync(preselectedPath);
        
        return await _storageProvider.SaveFilePickerAsync(new FilePickerSaveOptions()
        {
            Title = title,
            SuggestedStartLocation = folder,
            FileTypeChoices = filterChoices,
            SuggestedFileName = suggestedFileName
        });
    }
    
    /// <inheritdoc/>
    /// <exception cref="InvalidOperationException">Thrown if the storage provider is not registered.</exception>
    public async Task<IStorageFolder?> OpenFolderAsync(string? title = null)
    {
        if (_storageProvider == null) throw new InvalidOperationException("StorageProvider has not been registered yet.");
        
        var folders = await _storageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions()
        {
            AllowMultiple = false,
            Title = title
        });
        
        return folders.Count >= 1 ? folders[0] : null;
    }

    /// <inheritdoc/>
    /// <exception cref="InvalidOperationException">Thrown if the launcher is not registered.</exception>
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

    public bool OpenFileInfo(FileInfo fileInfo)
    {
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
                Debug.WriteLine($"Failed to open file explorer: {ex.Message}", Constants.DebugError);
                return false;
            }
        }

        return false;
    }
}