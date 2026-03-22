using System;
using System.IO;
using GottaManagePlus.Utils;

namespace GottaManagePlus.Services.PlusFolderServices;

/// <summary>
/// A service specialized in browsing inside the game's folder. 
/// </summary>
public sealed class PlusFolderBrowser(PlusFolderDb plusFolderDb)
{
    // ----- Private API -----
    private readonly PlusFolderDb _plusFolderDb = plusFolderDb;
    
    // ----- Public API -----
    /// <summary>
    /// Search the absolute location inside the game's folder.
    /// </summary>
    /// <param name="paths">The paths to be combined.</param>
    /// <returns>A sanitized <see cref="string"/> absolute path to a directory inside the game's folder.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The paths array is null or empty.</exception>
    /// <exception cref="UnauthorizedAccessException">If the path attempts to exploit "../" to leave the game's root folder.</exception>
    public string SearchAbsolutePath(params string[] paths)
    {
        if (paths == null || paths.Length < 1)
            throw new ArgumentOutOfRangeException(nameof(paths));

        var formedPath = Path.Combine(paths);
        var absolutePath = Path.GetFullPath(Path.Combine(_plusFolderDb.RootPath, formedPath));
        var normalizedRoot = Path.GetFullPath(_plusFolderDb.RootPath);

        return !absolutePath.StartsWith(normalizedRoot, StringComparison.Ordinal) ? 
            throw new UnauthorizedAccessException($"AbsolutePath attempts to leave the RootPath. ({formedPath})") : 
            absolutePath;
    }

    /// <summary>
    /// Search the location inside the game's folder and relative to the game's root folder.
    /// </summary>
    /// <param name="paths">The paths to be combined.</param>
    /// <returns>A sanitized <see cref="string"/> relative path to the directory inside the game's folder.</returns>
    public string SearchRelativePath(params string[] paths) =>
        Path.GetRelativePath(_plusFolderDb.RootPath, SearchAbsolutePath(paths));
}