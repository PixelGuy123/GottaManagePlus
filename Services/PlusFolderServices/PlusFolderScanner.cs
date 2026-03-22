using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using GottaManagePlus.Models;
using GottaManagePlus.Utils;
using Serilog;

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
            Log.Logger.Warning("executablePath is empty.");
            return false;
        }

        // Make it a long path and full if possible
        executablePath = FileUtils.GetLongPath(Path.GetFullPath(executablePath));
        
        if (!File.Exists(executablePath))
        {
            Log.Logger.Warning("Failed to find the executable file ({ExecutablePath}).", executablePath);
            return false;
        }

        var rootPath = Path.GetDirectoryName(executablePath);
        if (string.IsNullOrEmpty(rootPath))
        {
            Log.Logger.Warning("Failed to get the directory name from path ({ExecutablePath}).", executablePath);
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
                Log.Logger.Warning("Executable path is not {ExpectedExecName} or not an executable ({ExecutablePath}).", expectedExecName, executablePath);
                return false;
            }

            // If BALDI Data folder exists
            var baldiDataFolder = Path.Combine([rootPath, .. baldiDataFolderPath]);
            if (!Directory.Exists(baldiDataFolder))
            {
                Log.Logger.Warning("Executable path does not contain {Combine} folder ({GetFullPath}).", Path.Combine(baldiDataFolderPath), Path.GetFullPath(baldiDataFolder));
                return false;
            }
            
            // Check if the game version is valid
            if (!TryScanGlobalGameManagerFileAndGetGameVersion(baldiDataFolder, out var gameVersion))
            {
                Log.Logger.Warning("Executable path does not contain globalgamemanagers binary file.");
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
            Log.Logger.Information("--- Offset: {Offset} | Read: {BytesRead} bytes ---", offset, bytesRead);
            Log.Logger.Information("{content}", content);
            
            // Validate game through string
            if (!content.Contains(gameStringToValidate))
                return false;

            // Get indices
            var startVersionStrIndex = content.IndexOf(startVersionString, StringComparison.Ordinal) + startVersionString.Length;
            var endVersionStrIndex = content.IndexOf(endVersionString, startVersionStrIndex, StringComparison.Ordinal);
            // Calculate substring
            var versionSubStr = content.Substring(startVersionStrIndex, endVersionStrIndex - startVersionStrIndex);
            Log.Logger.Error("Retrieved version substring ({VersionSubStr}).", versionSubStr);

            // Try to parse version
            gameVersion = new WrappedGameVersion(versionSubStr);
            Log.Logger.Error("Managed to parse into valid version? ({WrappedGameVersion}).", gameVersion);
            
            return true;
        }
        catch (Exception ex)
        {
            Log.Logger.Error("Failed to scan the globalgamemanagers binary file.\n{exception}", ex);
            return false;
        }
    }

    [GeneratedRegex(@"[^\x20-\x7E\r\n]")]
    private static partial Regex NullCharacterRegex();
}