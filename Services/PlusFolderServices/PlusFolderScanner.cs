using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using GottaManagePlus.Models;
using GottaManagePlus.Utils;

namespace GottaManagePlus.Services.PlusFolderServices;

/// <summary>
/// A service responsible for validating the integrity of the game's folder, likewise updating the database.
/// </summary>
public static partial class PlusFolderScanner
{
    /// <summary>
    /// Checks whether the path given to the folder is legitimately a Baldi's Basics Plus folder.
    /// </summary>
    /// <param name="db">The database to check data from the game.</param>
    /// <param name="executablePath">The path to the executable of the game.</param>
    /// <returns><see langword="true"/> if the folder is indeed correct; otherwise, <see langword="false"/>.</returns>
    /// <remarks>
    /// Every time this method is used, the database is updated to use the folder scanned through it.
    /// </remarks>
    public static bool ValidateAndSetAsGameFolder(this PlusFolderDb db, string executablePath)
    {
        if (string.IsNullOrWhiteSpace(executablePath))
        {
            Debug.WriteLine("executablePath is empty.", Constants.DebugWarning);
            return false;
        }

        // Make it a long path and full if possible
        executablePath = FileUtils.GetLongPath(Path.GetFullPath(executablePath));
        
        if (!File.Exists(executablePath))
        {
            Debug.WriteLine($"Failed to find the executable file ({executablePath}).", Constants.DebugWarning);
            return false;
        }

        var rootPath = Path.GetDirectoryName(executablePath);
        if (string.IsNullOrEmpty(rootPath))
        {
            Debug.WriteLine($"Failed to get the directory name from path ({executablePath}).", Constants.DebugWarning);
            return false;
        }
        
        // Convert to long path if possible
        rootPath = FileUtils.GetLongPath(rootPath);
     
        // OS Checks
        if (OperatingSystem.IsWindows())
            return ValidateIndividually(("BALDI.exe", false), "BALDI_Data");
        
        if (OperatingSystem.IsMacOS())
            return ValidateIndividually(("BALDI.app", true), "Contents", "Resources", "Data");
        
        if (OperatingSystem.IsLinux())
            return ValidateIndividually(("BALDI.x86_64", true), "BALDI_Data");

        return false;
        
        // Helper to validate based on the same pattern that is followed
        bool ValidateIndividually((string, bool) executableNameAndUnixFlag, params string[] baldiDataFolderPath)
        {
            // if executable is BALDI.app
            var (expectedExecName, useUnixCheck) = executableNameAndUnixFlag;
            if (Path.GetFileName(executablePath) != expectedExecName || (useUnixCheck && !FileUtils.CheckIfUnixFileIsExecutable(executablePath)))
            {
                Debug.WriteLine($"Executable path is not {expectedExecName} or not an executable ({executablePath}).", Constants.DebugWarning);
                return false;
            }

            // If BALDI Data folder exists
            var baldiDataFolder = Path.Combine([rootPath, .. baldiDataFolderPath]);
            if (!Directory.Exists(baldiDataFolder))
            {
                Debug.WriteLine($"Executable path does not contain {Path.Combine(baldiDataFolderPath)} folder ({Path.GetFullPath(baldiDataFolder)}).", Constants.DebugWarning);
                return false;
            }
            
            // Check if the game version is valid
            if (!TryScanGlobalGameManagerFileAndGetGameVersion(baldiDataFolder, out var gameVersion))
            {
                Debug.WriteLine("Executable path does not contain globalgamemanagers binary file.", Constants.DebugWarning);
                return false;
            }
            
            // Update data
            db.BaldiDataFolder = baldiDataFolder;
            if (gameVersion != null)
                db.GameVersion = gameVersion;
            db.RootPath = rootPath;
            return true;
        }
    }

    // ----- Private API -----
    /// <summary>
    /// Scans the given path (expected globalgamemanagers) and attempt to return back the version stored in this binary file.
    /// </summary>
    /// <param name="dataPath">The path of the binary file.</param>
    /// <param name="gameVersion">The version this method could scan.</param>
    /// <returns><see langword="true"/> if the scan was successful; otherwise, <see langword="false"/>.</returns>
    private static bool TryScanGlobalGameManagerFileAndGetGameVersion(string dataPath, out WrappedGameVersion? gameVersion)
    {
        gameVersion = null;
        // Path to binary file
        var globalGameMgrPath = Path.Combine(dataPath, "globalgamemanagers");
        
        if (!File.Exists(globalGameMgrPath))
            return false;
        
        const long offset = 4500; // Bytes to skip
        const int bytesToRead = 500; // Bytes to read AFTER the skip
        const string gameStringToValidate = "Baldi's Basics in Education and Learningbasicallygames";
        const string startVersionString = "category.games@", endVersionString = "ff@$";
        
        try
        {
            using var fs = new FileStream(globalGameMgrPath, FileMode.Open, FileAccess.Read);

            // Byte Offset
            fs.Seek(offset, SeekOrigin.Begin);

            var buffer = new byte[bytesToRead];
            var bytesRead = fs.Read(buffer, 0, bytesToRead);

            // Convert data to string
            var content = Encoding.ASCII.GetString(buffer, 0, bytesRead);
            // Remove any character that is not a common ASCII character (remove control chars)
            content = NullCharacterRegex().Replace(content, "");
            
            // Debug
            Debug.WriteLine($"--- Offset: {offset} | Read: {bytesRead} bytes ---", Constants.DebugInfo);
            Debug.WriteLine(content, Constants.DebugInfo);
            
            // Validate game through string
            if (!content.Contains(gameStringToValidate))
                return false;

            // Get indices
            var startVersionStrIndex = content.IndexOf(startVersionString, StringComparison.Ordinal) + startVersionString.Length;
            var endVersionStrIndex = content.IndexOf(endVersionString, startVersionStrIndex, StringComparison.Ordinal);
            // Calculate substring
            var versionSubStr = content.Substring(startVersionStrIndex, endVersionStrIndex - startVersionStrIndex);
            Debug.WriteLine($"Retrieved version substring ({versionSubStr}).", Constants.DebugInfo);

            // Try to parse version
            gameVersion = new WrappedGameVersion(versionSubStr);
            Debug.WriteLine($"Managed to parse into valid version? ({gameVersion}).");
            
            return true;
        }
        catch (Exception ex)
        {
            Debug.WriteLine("Failed to scan the globalgamemanagers binary file.", Constants.DebugError);
            Debug.WriteLine(ex.ToString(), Constants.DebugError);
            return false;
        }
    }

    [GeneratedRegex(@"[^\x20-\x7E\r\n]")]
    private static partial Regex NullCharacterRegex();
}