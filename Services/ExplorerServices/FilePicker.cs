using System;
using System.Threading.Tasks;
using Avalonia.Platform.Storage;

namespace GottaManagePlus.Services.ExplorerServices;

/// <summary>
/// A class responsible for handling any type of file picking request.
/// </summary>
// TODO: Add Logger implementation for the exceptions inside these IO services.
public class FilePicker
{
    private IStorageProvider? _storageProvider;
    public void RegisterProvider(IStorageProvider provider) => _storageProvider = provider;
    
    /// <summary>
    /// Opens a file picker using Avalonia's API for selecting a file.
    /// </summary>
    /// <param name="title">The title of the file picker's window.</param>
    /// <param name="suggestedFileName">The file name suggested by default for the picker.</param>
    /// <param name="preselectedPath">The path that will be opened by default by the picker.</param>
    /// <param name="filterChoices">The filter to limit what files shall be opened.</param>
    /// <returns>A <see cref="IStorageFile"/> instance if one happens to be selected; otherwise, <see langword="null"/>.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the <see cref="IStorageProvider"/> from the <see cref="FilePicker"/> is <see langword="null"/>.</exception>
    public async Task<IStorageFile?> OpenSingleFileAsync(string? title = null, string? suggestedFileName = null, string? preselectedPath = null, params FilePickerFileType[] filterChoices)
    {
        if (_storageProvider == null) throw new InvalidOperationException("StorageProvider has not been registered yet.");

        IStorageFolder? folder = null;
        if (!string.IsNullOrEmpty(preselectedPath))
            folder = await _storageProvider.TryGetFolderFromPathAsync(preselectedPath);
        
        var files = await _storageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = title,
            AllowMultiple = false,
            SuggestedStartLocation = folder,
            FileTypeFilter = filterChoices,
            SuggestedFileName = suggestedFileName
        });

        return files.Count >= 1 ? files[0] : null;
    }

    /// <summary>
    /// Opens a file picker using Avalonia's API for saving a file.
    /// </summary>
    /// <param name="title">The title of the file picker's window.</param>
    /// <param name="suggestedFileName">The file name suggested by default for the picker.</param>
    /// <param name="preselectedPath">The path that will be opened by default by the picker.</param>
    /// <param name="filterChoices">The filter to limit what files shall be opened.</param>
    /// <returns>A <see cref="IStorageFile"/> instance if the file happens to be saved by the user; otherwise, <see langword="null"/>.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the <see cref="IStorageProvider"/> from the <see cref="FilePicker"/> is <see langword="null"/>.</exception>
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
}