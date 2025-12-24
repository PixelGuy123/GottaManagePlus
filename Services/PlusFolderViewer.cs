using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using GottaManagePlus.Interfaces;
using GottaManagePlus.Utils;

namespace GottaManagePlus.Services;

/// <summary>
/// A cross-platform implementation of <see cref="IGameFolderViewer"/> tailored for specific OS requirements.
/// </summary>
public partial class PlusFolderViewer : IGameFolderViewer
{
    /// <summary>
    /// Validates the game executable based on the current Operating System (Windows, macOS, or Linux).
    /// </summary>
    /// <inheritdoc/>
    public bool ValidateFolder(string executablePath, bool setPathIfTrue = true)
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
        Version? gameVersion;
        
        if (OperatingSystem.IsWindows())
        {
            // If executable is BALDI.exe
            if (Path.GetFileName(executablePath) != "BALDI.exe")
            {
                Debug.WriteLine($"Executable path is not BALDI.exe ({executablePath}).", Constants.DebugWarning);
                return false;
            }

            // If BALDI Data folder exists
            var baldiDataFolder = Path.Combine(rootPath, "BALDI_Data");
            if (!Directory.Exists(baldiDataFolder))
            {
                Debug.WriteLine($"Executable path does not contain BALDI_Data folder ({Path.GetFullPath(baldiDataFolder)}).", Constants.DebugWarning);
                return false;
            }
            
            // Check if the game version is valid
            if (!TryScanGlobalGameManagerFileAndGetGameVersion(baldiDataFolder, out gameVersion))
            {
                Debug.WriteLine("Executable path does not contain globalgamemanagers binary file.", Constants.DebugWarning);
                return false;
            }
            
            // If this is only a validation check, then stop here
            if (!setPathIfTrue) return true;
            
            _baldiDataFolder = baldiDataFolder;
            if (gameVersion != null)
                _gameVersion = gameVersion;
            RootPath = rootPath;
            
            return true;
        }

        if (OperatingSystem.IsMacOS())
        {
            // if executable is BALDI.app
            if (Path.GetFileName(executablePath) != "BALDI.app" || !FileUtils.CheckIfUnixFileIsExecutable(executablePath))
            {
                Debug.WriteLine($"Executable path is not BALDI.app or not an executable ({executablePath}).", Constants.DebugWarning);
                return false;
            }

            // If BALDI Data folder exists
            var baldiDataFolder = Path.Combine(rootPath, "Contents", "Resources", "Data");
            if (!Directory.Exists(baldiDataFolder))
            {
                Debug.WriteLine($"Executable path does not contain Contents/Resources/Data folder ({Path.GetFullPath(baldiDataFolder)}).", Constants.DebugWarning);
                return false;
            }
            
            // Check if the game version is valid
            if (!TryScanGlobalGameManagerFileAndGetGameVersion(baldiDataFolder, out gameVersion))
            {
                Debug.WriteLine("Executable path does not contain globalgamemanagers binary file.", Constants.DebugWarning);
                return false;
            }
            
            // If this is only a validation check, then stop here
            if (!setPathIfTrue) return true;
            
            _baldiDataFolder = baldiDataFolder;
            if (gameVersion != null)
                _gameVersion = gameVersion;
            RootPath = rootPath;
            return true;
        }
        
        if (OperatingSystem.IsLinux())
        {
            // if executable is BALDI.app
            if (Path.GetFileName(executablePath) != "BALDI.x86_64" || !FileUtils.CheckIfUnixFileIsExecutable(executablePath))
            {
                Debug.WriteLine($"Executable path is not BALDI.x86_64 or not an executable ({executablePath}).", Constants.DebugWarning);
                return false;
            }

            // If BALDI Data folder exists
            var baldiDataFolder = Path.Combine(rootPath, "BALDI_Data");
            if (!Directory.Exists(baldiDataFolder))
            {
                Debug.WriteLine($"Executable path does not contain BALDI_Data folder ({Path.GetFullPath(baldiDataFolder)}).", Constants.DebugWarning);
                return false;
            }

            // Check if the game version is valid
            if (!TryScanGlobalGameManagerFileAndGetGameVersion(baldiDataFolder, out gameVersion))
            {
                Debug.WriteLine("Executable path does not contain globalgamemanagers binary file.", Constants.DebugWarning);
                return false;
            }
            
            // If this is only a validation check, then stop here
            if (!setPathIfTrue) return true;
            
            _baldiDataFolder = baldiDataFolder;
            if (gameVersion != null)
                _gameVersion = gameVersion;
            RootPath = rootPath;
            return true;
        }

        return false;
    }

    /// <summary>
    /// Combines paths and verifies that the resulting path does not escape the <see cref="RootPath"/>.
    /// </summary>
    /// <param name="paths">The path in sequence to be followed.</param>
    /// <inheritdoc/>
    public string SearchPath(params string[] paths)
    {
        if (paths == null || paths.Length < 1)
            throw new ArgumentOutOfRangeException(nameof(paths));

        var formedPath = Path.Combine(paths);
        var absolutePath = Path.GetFullPath(Path.Combine(RootPath, formedPath));
        var normalizedRoot = RootPath.EndsWith(Path.DirectorySeparatorChar) ? 
            RootPath : 
            RootPath + Path.DirectorySeparatorChar;

        return !absolutePath.StartsWith(normalizedRoot, StringComparison.Ordinal) ? 
            throw new InvalidOperationException($"AbsolutePath attempts to leave the RootPath. ({formedPath})") : 
            FileUtils.GetLongPath(absolutePath);
    }
    /// <summary>
    /// Try to combine paths and guarantee the resulting path does not escape the <see cref="RootPath"/>.
    /// </summary>
    /// <param name="absolutePath">The absolute path created by the search</param>
    /// <param name="paths">The path in sequence to be followed.</param>
    /// <returns><see langword="true"/> if the search was done successfully and within the game's root folder; otherwise, <see langword="false"/>.</returns>
    public bool TrySearchPath(out string absolutePath, params string[] paths) 
        // If returns false, whatever tried to search it is definitely trying to leave the RootPath!
    {
        absolutePath = string.Empty;
        try
        {
            absolutePath = SearchPath(paths);
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Resolves common directories using the local configuration.
    /// </summary>
    /// <inheritdoc/>
    public string GetPathFrom(IGameFolderViewer.CommonDirectory directoryType) => directoryType switch
        {
            IGameFolderViewer.CommonDirectory.BaldiData => _baldiDataFolder,
            IGameFolderViewer.CommonDirectory.BepInEx => SearchPath("BepInEx"),
            IGameFolderViewer.CommonDirectory.ManagerRoot => SearchPath(Constants.AppRootFolder),
            _ => throw new ArgumentException(directoryType.ToString())
        };
    /// <inheritdoc/>
    public string GetGameRootPath() => RootPath;
    /// <inheritdoc/>
    public Version GetGameVersion() => _gameVersion;

    // Public getters
    /// <summary>
    /// Gets the current validated root path of the game.
    /// </summary>
    /// <exception cref="NullReferenceException">Thrown if the root path has not been set via validation.</exception>
    public string RootPath
    {
        get => string.IsNullOrEmpty(_rootPath) ? throw new NullReferenceException("RootPath is undefined.") : _rootPath;
        private set => _rootPath = value;
    }

    // Private members
    private string _baldiDataFolder = string.Empty;
    private Version _gameVersion = new("0.0.0.0");
    private string _rootPath = string.Empty;
    
    // Private methods
    private bool TryScanGlobalGameManagerFileAndGetGameVersion(string dataPath, out Version? gameVersion)
    {
        gameVersion = null;
        // Path to binary file
        var globalGameMngrPath = Path.Combine(dataPath, "globalgamemanagers");
        
        if (!File.Exists(globalGameMngrPath))
            return false;
        
        const long offset = 4500; // Bytes to skip
        const int bytesToRead = 500; // Bytes to read AFTER the skip
        const string gameStringToValidate = "Baldi's Basics in Education and Learningbasicallygames";
        const string startVersionString = "category.games@", endVersionString = "ff@$";
        
        try
        {
            using var fs = new FileStream(globalGameMngrPath, FileMode.Open, FileAccess.Read);

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
            var couldParse = Version.TryParse(versionSubStr, out gameVersion);
            Debug.WriteLine($"Managed to parse into valid version? ({couldParse}).");
            
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