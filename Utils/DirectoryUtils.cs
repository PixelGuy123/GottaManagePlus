using System.IO;

namespace GottaManagePlus.Utils;

/// <summary>
/// A static class that serves utilities for manipulating directories.
/// </summary>
public static class DirectoryUtils
{
    /// <summary>
    /// Attempts to get the <see cref="DirectoryInfo"/> or create it if it doesn't exist in storage.
    /// </summary>
    /// <param name="path">The path for the directory's info.</param>
    /// <returns>An instance of a locally existent <see cref="DirectoryInfo"/>.</returns>
    public static DirectoryInfo GetOrCreate(string path)
    {
        var dirInfo =
            new DirectoryInfo(path);
        if (!dirInfo.Exists) dirInfo.Create(); // Try and create directory.
        return dirInfo;
    }
}