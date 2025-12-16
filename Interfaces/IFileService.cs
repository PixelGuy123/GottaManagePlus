using System.Threading.Tasks;
using Avalonia.Platform.Storage;

namespace GottaManagePlus.Interfaces;

public interface IFilesService
{
    public Task<IStorageFile?> OpenFileAsync(string? title = null);
    public Task<IStorageFile?> SaveFileAsync(string? title = null);
    public Task<IStorageFolder?> OpenFolderAsync(string? title = null);
}