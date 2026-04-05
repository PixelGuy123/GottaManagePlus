using System;
using System.IO;
using GottaManagePlus.Services.GameEnvironmentServices;

namespace GottaManagePlus.Utils;

/// <summary>
/// A utilities class for operations that can be done with a <see cref="GameEnvironmentController"/> instance.
/// </summary>
public static class GameEnvironmentControllerUtils
{
    /// <summary>
    /// Search the absolute location inside the game's folder.
    /// </summary>
    /// <param name="controller">The <see cref="GameEnvironmentController"/> instance defined.</param>
    /// <param name="paths">The paths to be combined.</param>
    /// <returns>A sanitized <see cref="string"/> absolute path to a directory inside the game's folder.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The paths array is null or empty.</exception>
    /// <exception cref="UnauthorizedAccessException">If the path attempts to exploit "../" to leave the game's root folder.</exception>
    public static string SearchAbsolutePath(this GameEnvironmentController controller, params string[] paths)
    {
        var currentEnvironment = controller.CurrentEnvironment;
        if (currentEnvironment == null)
            throw new NullReferenceException("Game Environment is not defined");

        if (paths == null || paths.Length < 1)
            throw new ArgumentOutOfRangeException(nameof(paths));

        var formedPath = Path.Combine(paths);
        var absolutePath = Path.GetFullPath(Path.Combine(currentEnvironment.RootPath, formedPath));
        var normalizedRoot = Path.GetFullPath(currentEnvironment.RootPath);

        return !absolutePath.StartsWith(normalizedRoot, StringComparison.Ordinal)
            ? throw new UnauthorizedAccessException($"AbsolutePath attempts to leave the RootPath. ({formedPath})")
            : absolutePath;
    }

    /// <summary>
    /// Search the location inside the game's folder and relative to the game's root folder.
    /// </summary>
    /// <param name="controller">The <see cref="GameEnvironmentController"/> instance defined.</param>
    /// <param name="paths">The paths to be combined.</param>
    /// <returns>A sanitized <see cref="string"/> relative path to the directory inside the game's folder.</returns>
    /// <exception cref="NullReferenceException">If the game environment is null.</exception>
    public static string SearchRelativePath(this GameEnvironmentController controller, params string[] paths) =>
        controller.CurrentEnvironment == null
            ? throw new NullReferenceException("Game Environment is not defined")
            : Path.GetRelativePath(controller.CurrentEnvironment.RootPath, controller.SearchAbsolutePath(paths));

    /// <summary>
    /// Returns the default path for profiles through the controller or create such directory if it doesn't exist.
    /// </summary>
    /// <param name="controller">The controller to search the path.</param>
    /// <returns>A <see cref="string"/> pointing to the default path of the profiles' folder.</returns>
    public static string GetOrCreateProfilesFolderPath(this GameEnvironmentController controller)
    {
        var path = controller.SearchAbsolutePath(Constants.AppRootFolder, Constants.AppProfilesFolder);
        if (!Directory.Exists(path)) Directory.CreateDirectory(path);
        return path;
    }

    /// <summary>
    /// Return the default path for exported profiles through controller or create such directory if it doesn't exist.
    /// </summary>
    /// <param name="controller">The controller to search the path.</param>
    /// <returns>A <see cref="string"/> pointing to the default path of the profiles' export folder.</returns>
    public static string GetOrCreateProfilesExportFolderPath(this GameEnvironmentController controller)
    {
        var path = controller.SearchAbsolutePath(Constants.AppRootFolder, Constants.App_ProfileExportFolder);
        if (!Directory.Exists(path)) Directory.CreateDirectory(path);
        return path;
    }
}