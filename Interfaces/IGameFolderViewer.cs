using System;
using System.Threading.Tasks;
using Avalonia.Platform.Storage;
using GottaManagePlus.Models;

namespace GottaManagePlus.Interfaces;

/// <summary>
/// Defines methods for resolving and validating game-specific directory structures.
/// </summary>
public interface IGameFolderViewer
{
    /// <summary>
    /// Specifies well-known directory types within the game structure.
    /// </summary>
    public enum CommonDirectory
    {
        /// <summary>The BepInEx framework folder.</summary>
        BepInEx,
        /// <summary>The main game data folder.</summary>
        BaldiData,
        /// <summary>The root folder for the manager application.</summary>
        ManagerRoot
    }

    /// <summary>
    /// The implementation should return the absolute path for a specific <see cref="CommonDirectory"/>.
    /// </summary>
    /// <param name="directoryType">The type of directory to resolve.</param>
    /// <returns>The absolute path as a string.</returns>
    string GetPathFrom(CommonDirectory directoryType);

    /// <summary>
    /// The implementation should return the root directory of the game installation.
    /// </summary>
    /// <returns>The game root path.</returns>
    string GetGameRootPath();
    
    /// <summary>
    /// The implementation should return the current version scanned of the game.
    /// </summary>
    /// <returns>The game's current version.</returns>
    WrappedGameVersion GetGameVersion();
    
    /// <summary>
    /// The implementation should verify if the provided path points to a valid game executable.
    /// </summary>
    /// <param name="executablePath">The path to the executable file.</param>
    /// <param name="setPathIfTrue">If <see langword="true"/>, the implementation should store this path as the active game path upon success.</param>
    /// <returns><see langword="true"/> if the folder structure is valid; otherwise, <see langword="false"/>.</returns>
    bool ValidateFolder(string executablePath, bool setPathIfTrue = true);

    /// <summary>
    /// The implementation should combine the provided path segments and ensure the result is within the game root.
    /// </summary>
    /// <param name="paths">The path segments to combine.</param>
    /// <returns>The resulting absolute path.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if no paths are provided.</exception>
    /// <exception cref="InvalidOperationException">Thrown if the resulting path exits the root directory.</exception>
    string SearchPath(params string[] paths);
}