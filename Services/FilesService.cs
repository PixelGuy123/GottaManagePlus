using System;
using System.Threading.Tasks;
using Avalonia.Platform.Storage;
using GottaManagePlus.Interfaces;

namespace GottaManagePlus.Services;

public class FilesService : IFilesService
{
    private IStorageProvider? _storageProvider;

    public void RegisterProvider(IStorageProvider provider) => _storageProvider = provider;
    
    public async Task<IStorageFile?> OpenFileAsync(string? title = null)
    {
        if (_storageProvider == null) throw new InvalidOperationException("StorageProvider has not been registered yet.");
        
        var files = await _storageProvider.OpenFilePickerAsync(new FilePickerOpenOptions()
        {
            Title = title,
            AllowMultiple = false,
        });

        return files.Count >= 1 ? files[0] : null;
    }

    public async Task<IStorageFile?> SaveFileAsync(string? title = null)
    {
        if (_storageProvider == null) throw new InvalidOperationException("StorageProvider has not been registered yet.");
        
        return await _storageProvider.SaveFilePickerAsync(new FilePickerSaveOptions()
        {
            Title = title
        });
    }

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
}