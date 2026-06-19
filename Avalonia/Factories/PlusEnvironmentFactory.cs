using System.Collections.Concurrent;
using GottaManagePlus.Interfaces.GameEnvironment;
using GottaManagePlus.Models.GameEnvironments;
using GottaManagePlus.Utils;
using Serilog;

namespace GottaManagePlus.Factories;

/// <summary>
/// A factory responsible for generating the <see cref="PlusEnvironment"/> instance.
/// </summary>
public sealed class PlusEnvironmentFactory(ILogger logger) : IGameEnvironmentFactory
{
    // ---- Private ----
    private readonly ILogger _logger = logger;
    private readonly ConcurrentDictionary<string, PlusEnvironment> _uniquePlusEnvironments = [];
    
    // * For GlobalGameManagers lookup
    private const long GlobalGameManagersByteOffset = 4500; // Bytes to skip
    private const int GlobalGameManagersBytesToRead = 500; // Bytes to read AFTER the skip
    private const string GlobalGameManagersGameStringToValidate = "Baldi's Basics in Education and Learningbasicallygames";
    private const string GlobalGameManagersStartVersionString = "category.games@", GlobalGameManagersEndVersionString = "ff@$";
    
    // ----- Implementation -----
    /// <summary>
    /// Generates a <see cref="PlusEnvironment"/> object from given executable's folder.
    /// </summary>
    /// <param name="executablePath">The path where the game's executable is located.</param>
    /// <returns>An instance of <see cref="PlusEnvironment"/> or <see langword="null"/> if anything fails in the process.</returns>
    public IGameEnvironment? CreateEnvironment(string executablePath)
    {
        if (string.IsNullOrWhiteSpace(executablePath))
        {
            _logger.Warning("executablePath is empty.");
            return null;
        }

        // Make it a long path and full if possible
        executablePath = PathUtils.GetLongPath(Path.GetFullPath(executablePath));
        
        // If the executable path is false, attempt to remove it from the register
        if (!File.Exists(executablePath))
        {
            _logger.Warning("Failed to find the executable file ({ExecutablePath}).", executablePath);
            _uniquePlusEnvironments.TryRemove(executablePath, out _);
            return null;
        }
        
        // If the executable path exists, retrieve it
        if (_uniquePlusEnvironments.TryGetValue(executablePath, out var plusEnvironment))
        {
            _logger.Information("Retrieved existent PlusEnvironment from path: '{envPath}'", executablePath);
            return plusEnvironment;
        }

        var rootPath = Path.GetDirectoryName(executablePath);
        if (string.IsNullOrEmpty(rootPath))
        {
            _logger.Warning("Failed to get the directory name from path ({ExecutablePath}).", executablePath);
            return null;
        }
        
        // Convert to long path if possible
        rootPath = PathUtils.GetLongPath(rootPath);
     
        // OS Checks
        if (OperatingSystem.IsWindows())
            return ValidateIndividually(("BALDI.exe", false), "BALDI_Data");
        
        if (OperatingSystem.IsMacOS())
            return ValidateIndividually(("BALDI.app", true), "Contents", "Resources", "Data");
        
        if (OperatingSystem.IsLinux())
            return ValidateIndividually(("BALDI.x86_64", true), "BALDI_Data");

        return null;
        
        // Helper to validate based on the same pattern that is followed
        PlusEnvironment? ValidateIndividually((string, bool) executableNameAndUnixFlag, params string[] baldiDataFolderPath)
        {
            // if executable is BALDI.app/BALDI.exe
            var (expectedExecName, useUnixCheck) = executableNameAndUnixFlag;
            if (Path.GetFileName(executablePath) != expectedExecName || (useUnixCheck && !FileUtils.CheckIfUnixFileIsExecutable(executablePath)))
            {
                _logger.Warning("Executable path is not {ExpectedExecName} or not an executable ({ExecutablePath}).", expectedExecName, executablePath);
                _uniquePlusEnvironments.TryRemove(executablePath, out _);
                return null;
            }

            // If BALDI Data folder exists
            var baldiDataFolder = Path.Combine([rootPath, .. baldiDataFolderPath]);
            if (!Directory.Exists(baldiDataFolder))
            {
                _logger.Warning("Executable path does not contain {Combine} folder ({GetFullPath}).", Path.Combine(baldiDataFolderPath), Path.GetFullPath(baldiDataFolder));
                _uniquePlusEnvironments.TryRemove(executablePath, out _);
                return null;
            }
            
            // Check if the game version is valid
            var globalGameManagersPath = Path.Combine(baldiDataFolder, "globalgamemanagers");
            if (!GameEnvironmentUtils.TryScanGlobalGameManagerFileAndGetGameVersion(globalGameManagersPath,
                    GlobalGameManagersByteOffset, GlobalGameManagersBytesToRead,
                    GlobalGameManagersGameStringToValidate, GlobalGameManagersStartVersionString,
                    GlobalGameManagersEndVersionString, out var gameVersion, _logger))
            {
                _logger.Warning("Executable path does not contain globalgamemanagers binary file.");
                _uniquePlusEnvironments.TryRemove(executablePath, out _);
                return null;
            }
            
            // Create environment instance.
            var environment = new PlusEnvironment(rootPath, baldiDataFolder, executablePath, gameVersion);
            _uniquePlusEnvironments.TryAdd(executablePath, environment); // Adds to the database
            return environment;
        }
    }
}