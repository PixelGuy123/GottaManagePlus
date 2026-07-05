using Avalonia;
using Avalonia.Platform.Storage;

namespace GottaManagePlus;

public enum PageNames
{
    Home,
    Settings,
    LogViewer
}

public static class AppInfo
{
    // Application Version Information
    public static readonly string AppVersion = typeof(AppInfo).Assembly.GetName().Version!.ToString();
    public static readonly string AvaloniaVersion = typeof(AvaloniaObject).Assembly.GetName().Version!.ToString();
    public const string MutexName = "pixelguy.gottamanageplus.mutex";
}

public static class HyperLinks
{
    // App's Advertisement & Community Links
    public static readonly Uri DiscordLink = new("https://discord.gg/p2mpGsKAfG");
}

public static class Constants
{
    // ==================== APPLICATION PATHS ====================
    
    // The path to this GMP instance
    public static readonly string ApplicationLocation = AppContext.BaseDirectory;
    
    // Baldi's Basics Plus Steam installation path (platform-specific)
    public static readonly string BaldiPlusFolderSteamPath = 
        OperatingSystem.IsWindows() ? Path.Combine(Path.GetPathRoot(Environment.SystemDirectory) ?? "C:",
            "Program Files (86x)", "Steam", "steamapps",
            "common", "Baldi's Basics Plus") :
        OperatingSystem.IsMacOS() ? Path.Combine(Path.GetPathRoot(Environment.SystemDirectory) ?? "~", "Library",
            "Application Support", "Steam",
            "steamapps", "common", "Baldi's Basics Plus") :
        OperatingSystem.IsLinux() ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), 
            ".steam", "steam", "steamapps", "common", "Baldi's Basics Plus") :
        string.Empty;

    // ==================== FOLDER & FILE NAMES ====================
    
    // Root and special folders
    public const string App_RootFolder = ".gmp";
    public const string App_SpecialFolderForMods_Name = ".gmp";
    public const string App_ProfileExportFolder = "exports";
    public const string App_ProfilesFolder = "profiles";
    public const string App_TemporaryFolder = "temp";
    public const string App_IndexFile = "index";
    
    // File names and extensions
    public const string ProfileMetadataFileName = ".metadata";
    public const string ExportedProfileExtension = ".gmpProfile";
    public const string ProfileDefaultExtension = ".zip";
    public const string ModMetadataDefaultFileName = ".metadata";
    public const string ModManifestDefaultFileName = "manifest.json";
    public const string ModSupportForGameVersionPreviewFilePrefixName = "supVer_";
    public const string BepInExFolderName = "BepInEx";
    public const string PluginDisabledExtension = "disabled";
    
    // BepInEx subdirectories
    public const string ConfigFolder = "config";
    public const string PatchersFolder = "patchers";
    public const string PluginsFolder = "plugins";
    public const string PatchersIndexFolder = ".index";

    // ==================== UI DIALOGS ====================
    
    // Dialog titles
    public const string FailDialog = "Something went wrong...";
    public const string SuccessDialog = "Success!";
    public const string WarningDialog = "Just so you know...";
    
    // File picker filters
    public static readonly FilePickerFileType ExportedProfileFilter = new($"Exported Profile (*{ExportedProfileExtension})")
    {
        Patterns = [$"*{ExportedProfileExtension}"]
    };

    // ==================== TROUBLESHOOTING ====================
    
    // Common issues solutions for each platform
    public static readonly string? CommonIssuesSolution = 
        OperatingSystem.IsWindows() ? SolutionCommonIssuesWindows :
        OperatingSystem.IsMacOS() ? SolutionCommonIssuesMacOS :
        OperatingSystem.IsLinux() ? SolutionCommonIssuesLinux :
        SolutionCommonIssuesWindows; // Default to Windows

    private const string SolutionCommonIssuesWindows = """
                                                       1. Run the application as administrator.
                                                       2. Temporarily disable antivirus software.
                                                       3. Ensure the game directory has proper read/write permissions.
                                                       """;

    private const string SolutionCommonIssuesMacOS = """
                                                     1. Allow the application in System Preferences > Security & Privacy > General.
                                                     2. Grant Full Disk Access to the application if required.
                                                     3. Check and adjust file permissions using Terminal commands like chmod.
                                                     """;

    private const string SolutionCommonIssuesLinux = """
                                                     1. Run the application with elevated privileges using sudo if necessary.
                                                     2. Adjust file permissions with chmod or chown commands.
                                                     3. Check if SELinux or AppArmor is blocking access and configure accordingly.
                                                     """;

    // ==================== UNITY LOG PATHS ====================
    
    /// <summary>
    /// Gets the default Unity log folder path for Baldi's Basics Plus based on the operating system.
    /// </summary>
    /// <returns>The path to the Unity log folder if found; otherwise, <see langword="null"/>.</returns>
    public static string? GetUnityLogFolderPath()
    {
        try
        {
            if (OperatingSystem.IsWindows())
            {
                var appDataLocal = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                if (string.IsNullOrEmpty(appDataLocal)) return null;
                return Path.Combine(appDataLocal, "Low", "Basically Games", "Baldi's Basics Plus");
            }
            
            if (OperatingSystem.IsMacOS())
            {
                var homeDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                if (string.IsNullOrEmpty(homeDir)) return null;
                return Path.Combine(homeDir, "Library", "Logs", "Basically Games", "Baldi's Basics Plus");
            }
            
            if (OperatingSystem.IsLinux())
            {
                var homeDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                if (string.IsNullOrEmpty(homeDir)) return null;
                var linuxPath = Path.Combine(homeDir, ".config", "unity3d", "Basically Games", "Baldi's Basics Plus");
                // Verify the path exists on Linux as per requirements
                return Directory.Exists(linuxPath) ? linuxPath : null;
            }
            
            return null;
        }
        catch
        {
            return null;
        }
    }
}