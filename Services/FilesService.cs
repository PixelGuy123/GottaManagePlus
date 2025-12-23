using System;
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
    public async Task<IStorageFile?> OpenFileAsync(string? title = null, string? preselectedPath = null)
    {
        if (_storageProvider == null) throw new InvalidOperationException("StorageProvider has not been registered yet.");

        IStorageFolder? folder = null;
        if (!string.IsNullOrEmpty(preselectedPath))
            folder = await _storageProvider.TryGetFolderFromPathAsync(preselectedPath);
        
        var files = await _storageProvider.OpenFilePickerAsync(new FilePickerOpenOptions()
        {
            Title = title,
            AllowMultiple = false,
            SuggestedStartLocation = folder
        });

        return files.Count >= 1 ? files[0] : null;
    }

    /// <inheritdoc/>
    /// <exception cref="InvalidOperationException">Thrown if the storage provider is not registered.</exception>
    public async Task<IStorageFile?> SaveFileAsync(string? title = null, string? preselectedPath = null)
    {
        if (_storageProvider == null) throw new InvalidOperationException("StorageProvider has not been registered yet.");
        
        IStorageFolder? folder = null;
        if (!string.IsNullOrEmpty(preselectedPath))
            folder = await _storageProvider.TryGetFolderFromPathAsync(preselectedPath);
        
        return await _storageProvider.SaveFilePickerAsync(new FilePickerSaveOptions()
        {
            Title = title,
            SuggestedStartLocation = folder
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
    public Task<bool> OpenDirectoryInfo(DirectoryInfo directoryInfo) =>
        _launcher != null ? _launcher.LaunchDirectoryInfoAsync(directoryInfo) : 
            throw new InvalidOperationException("Launcher has not been registered yet.");
    
}