using System;
using System.Threading.Tasks;
using Avalonia.Platform.Storage;

namespace GottaManagePlus.Services.ExplorerServices;

/// <summary>
/// A class responsible for handling any type of folder picking request.
/// </summary>
public class DirectoryPicker
{
    private IStorageProvider? _storageProvider;
    public void RegisterProvider(IStorageProvider provider) => _storageProvider = provider;
    
    /// <summary>
    /// Opens a Folder picker through Avalonia's API to select a directory.
    /// </summary>
    /// <param name="title">The folder picker's title.</param>
    /// <returns>An instance of <see cref="IStorageFolder"/> if one happens to be selected; otherwise, <see langword="null"/>.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the <see cref="IStorageProvider"/> from the <see cref="DirectoryPicker"/> is <see langword="null"/>.</exception>
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