using System;
using System.IO;
using GottaManagePlus.Interfaces;

namespace GottaManagePlus.Utils;

public static class FileUtils
{
    /// <summary>
    /// Determines whether the specified Unix file has execute permissions for the user, group, or others.
    /// </summary>
    /// <param name="fileInfo">A <see cref="FileInfo"/> object representing the file to check.</param>
    /// <returns><see langword="true"/> if any execute flag (User, Group, or Other) is set; otherwise, <see langword="false"/>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="fileInfo"/> is null.</exception>
    /// <remarks>
    /// This method relies on the <see cref="UnixFileMode"/> property, which is primarily supported on Unix-like operating systems.
    /// </remarks>
    public static bool CheckIfUnixFileIsExecutable(FileInfo fileInfo)
    {
        var mode = fileInfo.UnixFileMode;
        
        // Check if it has an executable permission
        return mode.HasFlag(UnixFileMode.UserExecute) || 
               mode.HasFlag(UnixFileMode.GroupExecute) || 
               mode.HasFlag(UnixFileMode.OtherExecute);
    }
    /// <summary>
    /// Determines whether the file at the specified path has Unix execute permissions.
    /// </summary>
    /// <param name="filePath">The path to the file.</param>
    /// <returns><see langword="true"/> if the file has execute permissions; otherwise, <see langword="false"/>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="filePath"/> is null.</exception>
    /// <exception cref="System.Security.SecurityException">The caller does not have the required permission.</exception>
    /// <exception cref="UnauthorizedAccessException">Access to <paramref name="filePath"/> is denied.</exception>
    public static bool CheckIfUnixFileIsExecutable(string filePath) => CheckIfUnixFileIsExecutable(new FileInfo(filePath));

    /// <summary>
    /// Prepends the Windows Long Path prefix (<c>\\?\</c>) to the specified path if the operating system is Windows and the prefix is missing.
    /// </summary>
    /// <param name="fullPath">The full path to transform.</param>
    /// <returns>
    /// The path with the Long Path prefix and backslash separators if on Windows; 
    /// otherwise, the original <paramref name="fullPath"/>.
    /// </returns>
    /// <remarks>
    /// This is used to bypass the MAX_PATH (260 characters) limitation in the Windows API.
    /// </remarks>
    public static string GetLongPath(string fullPath)
    {
        const string prefix = @"\\?\";
        
        // If not Windows or starts with the prefix, it can return the same path again
        if (!OperatingSystem.IsWindows() || fullPath.StartsWith(prefix)) return fullPath;

        return prefix + fullPath.Replace('/', '\\'); // Replace / with \ for the prefix syntax
    }
    
    /// <summary>
    /// Determines if the current directory is located within the manager's root directory, 
    /// stopping the search if the game root folder is reached.
    /// </summary>
    /// <param name="directory">The directory to start the search from.</param>
    /// <param name="viewer">The <see cref="IGameFolderViewer"/> providing context for the game root path.</param>
    /// <returns><see langword="true"/> if <see cref="Constants.AppRootFolder"/> is a parent of the current directory; otherwise, <see langword="false"/>.</returns>
    public static bool IsWithinManagerRootDirectory(this DirectoryInfo directory, IGameFolderViewer viewer) =>
        IsWithinFolderName(directory, 
            Constants.AppRootFolder, // Get manager root folder
            Path.GetFileName(Path.GetDirectoryName(viewer.GetGameRootPath()))); // get game root folder
    
    /// <summary>
    /// Determines if the current directory is located within the manager's root directory, 
    /// stopping the search if the game root folder is reached.
    /// </summary>
    /// <param name="path">The directory to start the search from.</param>
    /// <param name="viewer">The <see cref="IGameFolderViewer"/> providing context for the game root path.</param>
    /// <returns><see langword="true"/> if <see cref="Constants.AppRootFolder"/> is a parent of the current directory; otherwise, <see langword="false"/>.</returns>
    public static bool IsWithinManagerRootDirectory(string path, IGameFolderViewer viewer) => 
        new DirectoryInfo(path).IsWithinManagerRootDirectory(viewer);
    
    /// <summary>
    /// Determines if the current directory is located within the game's root directory.
    /// </summary>
    /// <param name="directory">The directory to start the search from.</param>
    /// <param name="viewer">The <see cref="IGameFolderViewer"/> providing context for the game root path.</param>
    /// <returns><see langword="true"/> if the game folder name is found in the directory's parent hierarchy; otherwise, <see langword="false"/>.</returns>
    public static bool IsWithinGameRootDirectory(this DirectoryInfo directory, IGameFolderViewer viewer) => 
        IsWithinFolderName(directory, 
            Path.GetFileName(Path.GetDirectoryName(viewer.GetGameRootPath())) ?? "Baldi's Basics Plus"
            );
    /// <summary>
    /// Determines if the current directory is located within the game's root directory.
    /// </summary>
    /// <param name="path">The directory to start the search from.</param>
    /// <param name="viewer">The <see cref="IGameFolderViewer"/> providing context for the game root path.</param>
    /// <returns><see langword="true"/> if the game folder name is found in the directory's parent hierarchy; otherwise, <see langword="false"/>.</returns>
    public static bool IsWithinGameRootDirectory(string path, IGameFolderViewer viewer) =>
        new DirectoryInfo(path).IsWithinGameRootDirectory(viewer);
    
    
    // Private methods
    /// <summary>
    /// Iterates up the directory tree to check if the current directory is a child of a specific folder name.
    /// </summary>
    /// <param name="directory">The starting directory.</param>
    /// <param name="expectedParent">The folder name to search for in the hierarchy.</param>
    /// <param name="limitParent">An optional folder name that, if encountered, stops the upward search.</param>
    /// <returns><see langword="true"/> if <paramref name="expectedParent"/> is found before the root or <paramref name="limitParent"/>; otherwise, <see langword="false"/>.</returns>
    private static bool IsWithinFolderName(DirectoryInfo directory, string expectedParent, string? limitParent = null)
    {
        // Check parent folder
        var parent = directory.Parent;
        while (parent != null && (limitParent == null || !parent.Name.Equals(limitParent, StringComparison.Ordinal)))
        {
            // If at least the parent folder is .gmp, everything's fine
            if (parent.Name.Equals(expectedParent, StringComparison.OrdinalIgnoreCase))
                return true;
            
            parent = parent.Parent;
        }
        return false;
    }
}