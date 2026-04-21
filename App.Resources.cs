using System;
using System.IO;
using System.Reflection;
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
    // ---- Public API ----
    public static readonly string AppVersion = 'v' + AssemblyName.GetAssemblyName(Assembly.GetExecutingAssembly().Location).Version!.ToString();
}

public static class Constants
{
    // Current APP Version
    
    // Common paths for Steam to store its games
    public static readonly string BaldiPlusFolderSteamPath =
        // IF Windows, use the following path
        OperatingSystem.IsWindows() ? Path.Combine(Path.GetPathRoot(Environment.SystemDirectory) ?? "C:",
            "Program Files (86x)", "Steam", "steamapps",
            "common", "Baldi's Basics Plus") :
        // IF macOS, use following path
        OperatingSystem.IsMacOS() ? Path.Combine(Path.GetPathRoot(Environment.SystemDirectory) ?? "~", "Library",
            "Application Support", "Steam",
            "steamapps", "common", "Baldi's Basics Plus") :
        // IF Linux, use following path
        OperatingSystem.IsLinux() ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".steam", "steam", "steamapps", "common", "Baldi's Basics Plus") :
            string.Empty;

    // The path to this GMP instance
    public static readonly string ApplicationLocation = AppContext.BaseDirectory;
    
    // Extensions and names that the app uses
    public const string
        App_RootFolder = ".gmp",
        App_SpecialFolderForMods_Name = "_gmp",
        App_ProfileExportFolder = "exports",
        App_ProfilesFolder = "profiles",
        App_TemporaryFolder = "temp";
    public const string
        ProfileMetadataFileName = ".metadata", ExportedProfileExtension = ".gmpProfile", ProfileDefaultExtension = ".zip", 
        ModSupportForGameVersionPreviewFilePrefixName = "supVer_", BepInExFolderName = "BepInEx";
    
    // File Picker Filters
    public static readonly FilePickerFileType ExportedProfileFilter = new($"Exported Profile (*{ExportedProfileExtension})")
    {
        Patterns = [$"*{ExportedProfileExtension}"]
    };
    
    // Dialog titles
    public const string FailDialog = "Something went wrong...", 
        SuccessDialog = "Success!", 
        WarningDialog = "Just so you know...";
    
    // BepInEx Directory Names
    public const string ConfigFolder = "config",
        PatchersFolder = "patchers",
        PluginsFolder = "plugins";
    
   
    // Common issues solutions for each platform
    public static string CommonIssuesSolution =>
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