using System;
using System.IO;
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
}

public static class HyperLinks
{
    // App's Advertisement & Community Links
    public static readonly Uri KofiLink = new("https://ko-fi.com/pixelguy");
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
    public static readonly string App_RootFolder = ".gmp";
    public static readonly string App_SpecialFolderForMods_Name = "_gmp";
    public static readonly string App_ProfileExportFolder = "exports";
    public static readonly string App_ProfilesFolder = "profiles";
    public static readonly string App_TemporaryFolder = "temp";
    public static readonly string App_IndexFile = "index";
    
    // File names and extensions
    public static readonly string ProfileMetadataFileName = ".metadata";
    public static readonly string ExportedProfileExtension = ".gmpProfile";
    public static readonly string ProfileDefaultExtension = ".zip";
    public static readonly string ModSupportForGameVersionPreviewFilePrefixName = "supVer_";
    public static readonly string BepInExFolderName = "BepInEx";
    
    // BepInEx subdirectories
    public static readonly string ConfigFolder = "config";
    public static readonly string PatchersFolder = "patchers";
    public static readonly string PluginsFolder = "plugins";

    // ==================== UI DIALOGS ====================
    
    // Dialog titles
    public static readonly string FailDialog = "Something went wrong...";
    public static readonly string SuccessDialog = "Success!";
    public static readonly string WarningDialog = "Just so you know...";
    
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
}