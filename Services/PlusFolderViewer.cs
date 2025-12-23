using System;
using System.Diagnostics;
using System.IO;
using GottaManagePlus.Interfaces;
using GottaManagePlus.Utils;

namespace GottaManagePlus.Services;

/// <summary>
/// A cross-platform implementation of <see cref="IGameFolderViewer"/> tailored for specific OS requirements.
/// </summary>
public class PlusFolderViewer : IGameFolderViewer
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

            // If this is only a validation check, then stop here
            if (!setPathIfTrue) return true;
            
            _baldiDataFolder = baldiDataFolder;
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

            // If this is only a validation check, then stop here
            if (!setPathIfTrue) return true;
            
            _baldiDataFolder = baldiDataFolder;
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

            // If this is only a validation check, then stop here
            if (!setPathIfTrue) return true;
            
            _baldiDataFolder = baldiDataFolder;
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
        if (paths.Length < 1)
            throw new ArgumentOutOfRangeException(nameof(paths));
        
        var formedPath = paths.Length != 1 ? Path.Combine(paths) : paths[0];
        var absolutePath = Path.Combine(RootPath, formedPath);

        return !absolutePath.StartsWith(RootPath) ? 
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
    public string GetPathFrom(IGameFolderViewer.CommonDirectory directoryType, bool relativeToRootPath)
    {
        var path = directoryType switch
        {
            IGameFolderViewer.CommonDirectory.BaldiData => _baldiDataFolder,
            IGameFolderViewer.CommonDirectory.BepInEx => SearchPath("BepInEx"),
            IGameFolderViewer.CommonDirectory.ManagerRoot => SearchPath(Constants.AppRootFolder),
            _ => throw new ArgumentException(directoryType.ToString())
        };

        return relativeToRootPath ? Path.GetRelativePath(GetGameRootPath(), path) : path;
    }
    /// <inheritdoc/>
    public string GetGameRootPath() => RootPath;
    

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
    private string _rootPath = string.Empty;
}