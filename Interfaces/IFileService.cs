using System.IO;
using System.Threading.Tasks;
using Avalonia.Platform.Storage;

namespace GottaManagePlus.Interfaces;

/// <summary>
/// Defines asynchronous methods for interacting with the file system via UI pickers and system launchers.
/// </summary>
public interface IFilesService
{
    /// <summary>
    /// The implementation should prompt the user to select a file for opening.
    /// </summary>
    /// <param name="title">The title of the dialog.</param>
    /// <param name="preselectedPath">The initial directory path for the picker.</param>
    /// <returns>A task representing the operation, containing the selected <see cref="IStorageFile"/> or <see langword="null"/>.</returns>
    public Task<IStorageFile?> OpenFileAsync(string? title = null, string? preselectedPath = null);

    /// <summary>
    /// The implementation should prompt the user to select a location and name to save a file.
    /// </summary>
    /// <param name="title">The title of the dialog.</param>
    /// <param name="suggestedFileName">The suggested file name for the save operation.</param>
    /// <param name="preselectedPath">The initial directory path for the picker.</param>
    /// <param name="fileChoices">The file type choices for the save operation.</param>
    /// <returns>A task representing the operation, containing the saved <see cref="IStorageFile"/> or <see langword="null"/>.</returns>
    public Task<IStorageFile?> SaveFileAsync(string? title = null, string? suggestedFileName = null, string? preselectedPath = null, params FilePickerFileType[] fileChoices);

    /// <summary>
    /// The implementation should prompt the user to select a folder.
    /// </summary>
    /// <param name="title">The title of the dialog.</param>
    /// <returns>A task representing the operation, containing the selected <see cref="IStorageFolder"/> or <see langword="null"/>.</returns>
    public Task<IStorageFolder?> OpenFolderAsync(string? title = null);

    /// <summary>
    /// The implementation should open the specified directory in the system's file explorer.
    /// </summary>
    /// <param name="directoryInfo">The directory information to open.</param>
    /// <returns>A task representing the operation, containing <see langword="true"/> if successful.</returns>
    public Task<bool> OpenDirectoryInfo(DirectoryInfo directoryInfo);
}