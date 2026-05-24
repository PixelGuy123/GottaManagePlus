using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Text.RegularExpressions;
using GottaManagePlus.Models;
using Serilog;

namespace GottaManagePlus.Utils;

/// <summary>
/// A utilities class for managing internal elements of a game environment.
/// </summary>
public static partial class GameEnvironmentUtils
{
    // ----- Public -----
    /// <summary>
    /// Attempts to scan a Unity GlobalGameManagers file to extract and validate a game version string from a specific binary region.
    /// </summary>
    /// <param name="globalGameManagersPath">Full file path to the GlobalGameManagers file.</param>
    /// <param name="bytesToSkip">Number of bytes to skip from the beginning of the file before reading.</param>
    /// <param name="bytesToRead">Number of bytes to read from the skip position.</param>
    /// <param name="gameStringToValidate">Expected identifying string that must be present in the read region to confirm correct context.</param>
    /// <param name="startVersionString">Marker string immediately preceding the version substring.</param>
    /// <param name="endVersionString">Marker string immediately following the version substring.</param>
    /// <param name="gameVersion">Output parsed game version if successful; otherwise null.</param>
    /// <param name="logger">Logger for diagnostic and error output.</param>
    /// <returns>True if the file was read, validation passed, and version successfully parsed; otherwise false.</returns>
    public static bool TryScanGlobalGameManagerFileAndGetGameVersion(string globalGameManagersPath, long bytesToSkip,
        int bytesToRead, string gameStringToValidate, string startVersionString, string endVersionString,
        [NotNullWhen(true)] out WrappedGameVersion? gameVersion, ILogger logger)
    {
        gameVersion = null;
        
        // Check if file exists at specified path
        if (!File.Exists(globalGameManagersPath))
            return false;
        
        try
        {
            // Open file stream for reading.
            using var fs = new FileStream(globalGameManagersPath, FileMode.Open, FileAccess.Read);

            // Seek to the position where version data is expected.
            fs.Seek(bytesToSkip, SeekOrigin.Begin);

            // Read specified number of bytes into buffer.
            var buffer = new byte[bytesToRead];
            var bytesRead = fs.Read(buffer, 0, bytesToRead);

            // Convert raw bytes to ASCII string.
            var content = Encoding.ASCII.GetString(buffer, 0, bytesRead);
            // Strip out any non-printable control characters to clean the string.
            content = NullCharacterRegex().Replace(content, string.Empty);
            
            // Log diagnostic information about read operation.
            logger.Information("--- bytesToSkip: {bytesToSkip} | Read: {BytesRead} bytes ---", bytesToSkip, bytesRead);
            logger.Information("{content}", content);
            
            // Validate that the expected game identifier exists in the content.
            if (!content.Contains(gameStringToValidate))
                return false;

            // Locate the version substring between start and end markers.
            var startVersionStrIndex = content.IndexOf(startVersionString, StringComparison.Ordinal) + startVersionString.Length;
            var endVersionStrIndex = content.IndexOf(endVersionString, startVersionStrIndex, StringComparison.Ordinal);
            // Extract the raw version string from between the markers.
            var versionSubStr = content.Substring(startVersionStrIndex, endVersionStrIndex - startVersionStrIndex);
            logger.Information("Retrieved version substring ({VersionSubStr}).", versionSubStr);

            // Attempt to parse the extracted string into a WrappedGameVersion object.
            gameVersion = new WrappedGameVersion(versionSubStr);
            logger.Information("Managed to parse into valid version? ({WrappedGameVersion}).", gameVersion);
            
            return true;
        }
        catch (Exception ex)
        {
            // Log any exceptions that occur during file operations or parsing
            logger.Error("Failed to scan the globalgamemanagers binary file.\n{exception}", ex);
            return false;
        }
    }

    [GeneratedRegex(@"[^\x20-\x7E\r\n]")]
    private static partial Regex NullCharacterRegex();
}