using System;
using System.Diagnostics;
using System.IO;
using GottaManagePlus.Interfaces;
using GottaManagePlus.Utils;

namespace GottaManagePlus.Services;

public class PlusFolderViewer : IGameFolderViewer
{
    public bool ValidateFolder(string executablePath, bool setPathIfTrue = true)
    {
        if (string.IsNullOrWhiteSpace(executablePath))
        {
            Debug.WriteLine("executablePath is empty.", Constants.DebugWarning);
            return false;
        }
        
        if (!File.Exists(executablePath))
        {
            Debug.WriteLine($"Failed to find the executable file ({Path.GetFullPath(executablePath)}).", Constants.DebugWarning);
            return false;
        }

        var rootPath = Path.GetDirectoryName(Path.GetFullPath(executablePath));
        if (string.IsNullOrEmpty(rootPath))
        {
            Debug.WriteLine($"Failed to get the directory name from path ({Path.GetFullPath(executablePath)}).", Constants.DebugWarning);
            return false;
        }
        
        if (OperatingSystem.IsWindows())
        {
            // If executable is BALDI.exe
            if (Path.GetFileName(executablePath) != "BALDI.exe")
            {
                Debug.WriteLine($"Executable path is not BALDI.exe ({Path.GetFullPath(executablePath)}).", Constants.DebugWarning);
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
            if (Path.GetFileName(executablePath) != "BALDI.app" || !UnixUtils.CheckIfUnixFileIsExecutable(executablePath))
            {
                Debug.WriteLine($"Executable path is not BALDI.app or not an executable ({Path.GetFullPath(executablePath)}).", Constants.DebugWarning);
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
            if (Path.GetFileName(executablePath) != "BALDI.x86_64" || !UnixUtils.CheckIfUnixFileIsExecutable(executablePath))
            {
                Debug.WriteLine($"Executable path is not BALDI.x86_64 or not an executable ({Path.GetFullPath(executablePath)}).", Constants.DebugWarning);
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

    public Version GetGameVersion()
    {
        throw new NotImplementedException();
    }

    public string? GetBaldiDataPath() => _baldiDataFolder;
    
    
    public string? GetBepInExPath()
    {
        var bepinexPath = Path.Combine(RootPath, "BepInEx");
        return Directory.Exists(bepinexPath) ? bepinexPath : null;
    }

    // Public getters
    public string RootPath
    {
        get => string.IsNullOrEmpty(_rootPath) ? throw new NullReferenceException("RootPath is undefined.") : _rootPath;
        private set => _rootPath = value;
    }

    // Private members
    private string _baldiDataFolder = string.Empty;
    private string _rootPath = string.Empty;
}