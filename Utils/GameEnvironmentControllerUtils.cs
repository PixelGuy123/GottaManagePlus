using GottaManagePlus.Models;
using GottaManagePlus.Services.GameEnvironmentServices;
using Serilog;

namespace GottaManagePlus.Utils;

/// <summary>
/// A utilities class for operations that can be done with a <see cref="GameEnvironmentController"/> instance.
/// </summary>
public static class GameEnvironmentControllerUtils
{
    /// <param name="controller">The <see cref="GameEnvironmentController"/> instance defined.</param>
    extension(GameEnvironmentController controller)
    {
        /// <summary>
        /// Search the absolute location inside the game's folder.
        /// </summary>
        /// <param name="paths">The paths to be combined.</param>
        /// <returns>A sanitized absolute path to a directory inside the game's folder.</returns>
        /// <exception cref="ArgumentOutOfRangeException">The paths array is null or empty.</exception>
        /// <exception cref="UnauthorizedAccessException">If the path attempts to exploit "../" to leave the game's root folder.</exception>
        public OsPath SearchAbsolutePath(params string[] paths)
        {
            var currentEnvironment = controller.CurrentEnvironment;
            if (currentEnvironment == null)
                throw new NullReferenceException("Game Environment is not defined");

            if (paths == null || paths.Length < 1)
                throw new ArgumentOutOfRangeException(nameof(paths));

            var formedPath = Path.Combine(paths);
            var absolutePath = Path.GetFullPath(Path.Combine(currentEnvironment.RootPath.BasePath, formedPath));
            var normalizedRoot = Path.GetFullPath(currentEnvironment.RootPath);

            return !absolutePath.StartsWith(normalizedRoot, StringComparison.Ordinal)
                ? throw new UnauthorizedAccessException($"AbsolutePath attempts to leave the RootPath. ({formedPath})")
                : absolutePath;
        }

        /// <summary>
        /// Search the location inside the game's folder and relative to the game's root folder.
        /// </summary>
        /// <param name="paths">The paths to be combined.</param>
        /// <returns>A sanitized <see cref="string"/> relative path to the directory inside the game's folder.</returns>
        /// <exception cref="NullReferenceException">If the game environment is null.</exception>
        public string SearchRelativePath(params string[] paths) =>
            controller.CurrentEnvironment == null
                ? throw new NullReferenceException("Game Environment is not defined")
                : Path.GetRelativePath(controller.CurrentEnvironment.RootPath, controller.SearchAbsolutePath(paths));

        /// <summary>
        /// Checks whether the path given is safe by the <see cref="GameEnvironmentControllerUtils.SearchAbsolutePath"/> standards.
        /// </summary>
        /// <param name="paths">The path collection to be tested against.</param>
        /// <returns><see langword="true"/> if the path is safe; otherwise, <see langword="false"/>.</returns>
        public bool IsPathSafetyValid(params string[] paths)
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
        /// <param name="logger">Logger to report directory manipulation.</param>
        /// <returns>A <see cref="string"/> pointing to the default path of the profiles' folder.</returns>
        public string GetOrCreateProfilesFolderPath(ILogger? logger = null)
        {
            var path = controller.SearchAbsolutePath(Constants.App_RootFolder, Constants.App_ProfilesFolder);
            DirectoryUtils.GetOrCreate(path);
            logger?.Information("Retrieved Profiles directory at '{path}'.", path);
            return path;
        }

        /// <summary>
        /// Return the default path for exported profiles through controller or create such directory if it doesn't exist.
        /// </summary>
        /// <param name="logger">Logger to report directory manipulation.</param>
        /// <returns>A <see cref="string"/> pointing to the default path of the profiles' export folder.</returns>
        public string GetOrCreateProfilesExportFolderPath(ILogger? logger = null)
        {
            var path = controller.SearchAbsolutePath(Constants.App_RootFolder, Constants.App_ProfileExportFolder);
            DirectoryUtils.GetOrCreate(path);
            logger?.Information("Retrieved Profiles Export directory at '{path}'.", path);
            return path;
        }

        /// <summary>
        /// Creates a temporary subdirectory inside the game's .gmp/temp folder with a unique GUID name.
        /// </summary>
        /// <param name="logger">Optional logger to report directory creation.</param>
        /// <returns>A <see cref="TemporaryDirectoryInfo"/> instance pointing to the newly created temporary subdirectory.</returns>
        public TemporaryDirectoryInfo CreateTempSubdirectory(ILogger? logger = null)
        {
            // Get the base temp folder path: .gmp/temp
            var tempBasePath = controller.SearchAbsolutePath(Constants.App_RootFolder, Constants.App_TemporaryFolder);
    
            // Ensure the base temp directory exists
            DirectoryUtils.GetOrCreate(tempBasePath);
            logger?.Information("Using base temp directory at '{TempBasePath}'.", tempBasePath);
    
            // Generate a unique directory name (GUID is cross-platform safe)
            var uniqueDirName = Guid.NewGuid().ToString();
            var tempSubDirPath = (string)Path.Combine(tempBasePath, uniqueDirName);
    
            // Create the temporary subdirectory
            logger?.Information("Creating temporary subdirectory '{TempSubDirPath}'.", tempSubDirPath);
            return new TemporaryDirectoryInfo(Directory.CreateDirectory(tempSubDirPath), logger);
        }
    }
}