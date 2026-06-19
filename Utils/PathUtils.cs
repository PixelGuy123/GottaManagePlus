namespace GottaManagePlus.Utils;

public static class PathUtils
{
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
    /// Turn the whole file name given into a name the OS accepts (based on <see cref="Path.GetInvalidPathChars()"/>).
    /// </summary>
    /// <param name="fileNameForCleanUp">The name to be changed.</param>
    /// <returns>A new filtered name.</returns>
    public static string TurnFileNameLegal(string fileNameForCleanUp)
    {
        // New string.
        var newStr = fileNameForCleanUp.ToCharArray();
        
        // Get invalid characters.
        var invalidChars = Path.GetInvalidPathChars();
        
        // Ensure the name of the file is legal.
        for (var i = 0; i < newStr.Length; i++)
            if (invalidChars.Contains(newStr[i]))
                newStr[i] = '_';
        
        return new string(newStr);
    }
}