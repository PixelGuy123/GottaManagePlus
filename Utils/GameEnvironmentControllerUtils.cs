using System;
using System.IO;
using GottaManagePlus.Services.GameEnvironmentServices;
using Serilog;

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
    /// Checks whether the path given is safe by the <see cref="GameEnvironmentControllerUtils.SearchAbsolutePath"/> standards.
    /// </summary>
    /// <param name="controller">The controller for the environment.</param>
    /// <param name="paths">The path collection to be tested agaisnt.</param>
    /// <returns><see langword="true"/> if the path is safe; otherwise, <see langword="false"/>.</returns>
    public static bool IsPathSafetyValid(this GameEnvironmentController controller, params string[] paths)
    {
        try
        {
            // Does a search with no return.
            controller.SearchAbsolutePath(paths);
            
            // If the search is a success, then return true.
            return true;
        }
        catch (UnauthorizedAccessException)
        {
            return false;
        }
        
        // Any other exception is thrown back.
    }

    /// <summary>
    /// Returns the default path for profiles through the controller or create such directory if it doesn't exist.
    /// </summary>
    /// <param name="controller">The controller to search the path.</param>
    /// <param name="logger">Logger to report directory manipulation.</param>
    /// <returns>A <see cref="string"/> pointing to the default path of the profiles' folder.</returns>
    public static string GetOrCreateProfilesFolderPath(this GameEnvironmentController controller, ILogger? logger = null)
    {
        var path = controller.SearchAbsolutePath(Constants.App_RootFolder, Constants.App_ProfilesFolder);
        if (!Directory.Exists(path))
        {
            logger?.Information("Creating Profiles directory at \'{path}\'.", path);
            Directory.CreateDirectory(path);
        }
        else logger?.Information("Retrieved Profiles directory at \'{path}\'.", path);
        return path;
    }

    /// <summary>
    /// Return the default path for exported profiles through controller or create such directory if it doesn't exist.
    /// </summary>
    /// <param name="controller">The controller to search the path.</param>
    /// <param name="logger">Logger to report directory manipulation.</param>
    /// <returns>A <see cref="string"/> pointing to the default path of the profiles' export folder.</returns>
    public static string GetOrCreateProfilesExportFolderPath(this GameEnvironmentController controller, ILogger? logger = null)
    {
        var path = controller.SearchAbsolutePath(Constants.App_RootFolder, Constants.App_ProfileExportFolder);
        if (!Directory.Exists(path))
        {
            logger?.Information("Creating Profiles Export directory at \'{path}\'.", path);
            Directory.CreateDirectory(path);
        }
        else logger?.Information("Retrieved Profiles Export directory at \'{path}\'.", path);
        return path;
    }
    
    /// <summary>
    /// Creates a temporary subdirectory inside the game's .gmp/temp folder with a unique GUID name.
    /// </summary>
    /// <param name="controller">The controller to resolve the game's root path.</param>
    /// <param name="logger">Optional logger to report directory creation.</param>
    /// <returns>A <see cref="DirectoryInfo"/> instance pointing to the newly created temporary subdirectory.</returns>
    public static DirectoryInfo CreateTempSubdirectory(this GameEnvironmentController controller, ILogger? logger = null)
    {
        // Get the base temp folder path: .gmp/temp
        var tempBasePath = controller.SearchAbsolutePath(Constants.App_RootFolder, Constants.App_TemporaryFolder);
    
        // Ensure the base temp directory exists
        if (!Directory.Exists(tempBasePath))
        {
            logger?.Information("Creating base temp directory at \'{TempBasePath}\'.", tempBasePath);
            Directory.CreateDirectory(tempBasePath);
        }
        else
        {
            logger?.Information("Base temp directory already exists at \'{TempBasePath}\'.", tempBasePath);
        }
    
        // Generate a unique directory name (GUID is cross-platform safe)
        var uniqueDirName = Guid.NewGuid().ToString();
        var tempSubDirPath = Path.Combine(tempBasePath, uniqueDirName);
    
        // Create the temporary subdirectory
        logger?.Information("Creating temporary subdirectory \'{TempSubDirPath}\'.", tempSubDirPath);
        return Directory.CreateDirectory(tempSubDirPath);
    }
}