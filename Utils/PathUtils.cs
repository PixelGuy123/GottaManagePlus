namespace GottaManagePlus.Utils;

public static class PathUtils
{
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