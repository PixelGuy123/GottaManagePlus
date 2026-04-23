using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Avalonia.Platform.Storage;
using Serilog;

namespace GottaManagePlus.Services.ExplorerServices;

/// <summary>
/// A class responsible for handling any type of folder picking request.
/// </summary>
public class DirectoryPicker(ILogger logger)
{
    private readonly ILogger _logger = logger;
    private IStorageProvider? _storageProvider;
    public void RegisterProvider(IStorageProvider provider) => _storageProvider = provider;
    
    /// <summary>
    /// Opens a Folder picker through Avalonia's API to select multiple directories.
    /// </summary>
    /// <param name="title">The folder picker's title.</param>
    /// <param name="startingLocation">The suggested location to start the search.</param>
    /// <returns>An instance of <see cref="IStorageFolder"/> if one happens to be selected; otherwise, <see langword="null"/>.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the <see cref="IStorageProvider"/> from the <see cref="DirectoryPicker"/> is <see langword="null"/>.</exception>
    public async Task<IReadOnlyList<IStorageFolder>> OpenMultipleFoldersAsync(string? title = null, DirectoryInfo? startingLocation = null) => await GeneralOpenFolderAsync(title, true, startingLocation);
    
    /// <summary>
    /// Opens a Folder picker through Avalonia's API to select a directory.
    /// </summary>
    /// <param name="title">The folder picker's title.</param>
    /// <param name="startingLocation">The suggested location to start the search.</param>
    /// <returns>An instance of <see cref="IStorageFolder"/> if one happens to be selected; otherwise, <see langword="null"/>.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the <see cref="IStorageProvider"/> from the <see cref="DirectoryPicker"/> is <see langword="null"/>.</exception>
    public async Task<IStorageFolder?> OpenFolderAsync(string? title = null, DirectoryInfo? startingLocation = null)
    {
        var folders = await GeneralOpenFolderAsync(title, startingLocation: startingLocation);
        return folders.Count >= 1 ? folders[0] : null;
    }
    
    /// <summary>
    /// Opens a directory picker to select directories in general.
    /// </summary>
    private async Task<IReadOnlyList<IStorageFolder>> GeneralOpenFolderAsync(string? title = null, bool allowMultiple = false, DirectoryInfo? startingLocation = null)
    {
        if (_storageProvider != null)
        {
            var suggestedLocation = startingLocation == null ? 
                null : await _storageProvider.TryGetFolderFromPathAsync(startingLocation.FullName);
            
            return await _storageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
            {
                AllowMultiple = allowMultiple,
                Title = title,
                SuggestedStartLocation = suggestedLocation
            });
        }

        _logger.Error("{Name}\'s StorageProvider is null!", GetType().Name);
        throw new InvalidOperationException("StorageProvider has not been registered yet.");
    }
}