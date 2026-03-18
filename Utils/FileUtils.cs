using System;
using System.IO;
using System.Linq;
using GottaManagePlus.Interfaces;

namespace GottaManagePlus.Utils;

public static class FileUtils
{
    /// <summary>
    /// Separates a full file path into its file name (without any extensions) and its full extension(s).
    /// </summary>
    /// <param name="fullPath">The complete path to the file.</param>
    /// <returns>A tuple where the first item is the full extension(s) (e.g., ".tar.gz") and the second item is the file name without any extensions.</returns>
    public static (string, string) SeparateFileNameFromExtensions(string fullPath)
    {
        var fileName = Path.GetFileNameWithoutExtension(fullPath);
        var extensions = Path.GetExtension(fullPath);
        while (Path.HasExtension(fileName))
        {
            extensions = Path.GetExtension(fileName) + extensions; // Inversion of this order
            fileName = Path.GetFileNameWithoutExtension(fileName); // Get without extension again
        }

        return (extensions, fileName);
    }
    
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
    /// Checks whether given path is still inside an expected base directory.
    /// </summary>
    /// <param name="fullPath">The path to be checked.</param>
    /// <param name="baseDirectory">The expected directory the path should be inside.</param>
    /// <returns><see langword="true"/> if the path is indeed inside the base directory; otherwise, <see langword="false"/>.</returns>
    public static bool IsPathWithinBase(string fullPath, string baseDirectory)
    {
        if (string.IsNullOrEmpty(fullPath) || string.IsNullOrEmpty(baseDirectory))
            return false;
    
        var normalizedFullPath = Path.GetFullPath(fullPath);
        var normalizedBasePath = Path.GetFullPath(baseDirectory);
    
        // Ensure the path starts with the base directory
        return normalizedFullPath.StartsWith(normalizedBasePath, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Replace all invalid characters in a string with '_'.
    /// </summary>
    /// <param name="path">The path/string to be corrected.</param>
    /// <returns>A filtered out <see cref="string"/> with '_' in the place of where invalid characters were located.</returns>
    public static string FilterOutInvalidChars(string path)
    {
        var newPath = path.ToCharArray();
        for (var i = 0; i < newPath.Length; i++)
        {
            var c = newPath[i];
            if (!Path.GetInvalidPathChars().Contains(c)) continue;

            newPath[i] = '_'; // Update back the array to replace the invalid character
        }

        return new string(newPath);
    }

    /// <summary>
    /// Assures that given path string has the current path separator character in the running OS.
    /// </summary>
    /// <param name="path">The path to be corrected.</param>
    /// <returns>A path that respects the separator character used by the current environment.</returns>
    public static string CorrectForeignPathToEnvironment(string path) =>
        path.Replace('/', Path.DirectorySeparatorChar)
            .Replace('\\', Path.DirectorySeparatorChar);
}