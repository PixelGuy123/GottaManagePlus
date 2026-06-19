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

    /// <summary>
    /// Moves all the content inside <paramref name="rootDirPath"/> safely. If a directory or file exists, it is replaced accordingly.
    /// </summary>
    /// <param name="rootDirPath">The directory path to be moved.</param>
    /// <param name="destDirName">The destination path.</param>
    public static void AtomicallyMoveTo(string rootDirPath, string destDirName) =>
        new DirectoryInfo(rootDirPath).AtomicallyMoveTo(destDirName);
    
    /// <summary>
    /// Moves all the content from given <see cref="DirectoryInfo"/> safely. If a directory or file exists, it is replaced accordingly.
    /// </summary>
    /// <param name="directoryInfo">The directory to be moved.</param>
    /// <param name="destDirName">The destination path.</param>
    public static void AtomicallyMoveTo(this DirectoryInfo directoryInfo, string destDirName)
    {
        // Guard against moving a directory onto itself
        if (string.Equals(directoryInfo.FullName, Path.GetFullPath(destDirName), StringComparison.OrdinalIgnoreCase))
            throw new IOException("Cannot move directory to itself.");

        // If destination exists as a file, delete it (replace with directory)
        if (File.Exists(destDirName))
            File.Delete(destDirName);
        
        // If destination directory does not exist, simple move works
        if (!Directory.Exists(destDirName))
        {
            directoryInfo.MoveTo(destDirName);
            return;
        }

        // Destination is an existing directory – merge contents

        // 1. Move all files from source root, replacing if needed
        foreach (var file in directoryInfo.EnumerateFiles())
        {
            var destFilePath = Path.Combine(destDirName, file.Name);

            // If a directory blocks the file path, delete it
            if (Directory.Exists(destFilePath))
                Directory.Delete(destFilePath, recursive: true);

            // Move file (overwrites existing file)
            File.Move(file.FullName, destFilePath, overwrite: true);
        }

        // 2. Recursively move all subdirectories
        foreach (var subDir in directoryInfo.GetDirectories())
        {
            var destSubDirPath = Path.Combine(destDirName, subDir.Name);

            // If a file blocks the subdirectory path, delete it
            if (File.Exists(destSubDirPath))
                File.Delete(destSubDirPath);

            // Recursively merge the subdirectory
            subDir.AtomicallyMoveTo(destSubDirPath);
        }

        // 3. Delete the now-empty source directory
        directoryInfo.Delete();
    }
}