using System;
using System.IO;
using System.Reflection;
using Avalonia.Platform.Storage;

namespace GottaManagePlus;

public enum PageNames
{
    Home,
    Settings,
    Game
}

public static class Constants
{
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
    // The backup directory based on call
    public static string BackupDir => "_gmp_backup_" + DateTime.Now.Ticks;
    
    // Extensions and names that the app uses
    public const string 
        AppRootFolder = ".gmp", App_SpecialFolderForMods_Name = "_gmp",App_ProfileExportFolder = "exports", AppProfilesFolder = "profiles",
        ProfileMetadataFileName = ".metadata", ExportedProfileExtension = ".gmpProfile", 
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
    
    // Platform-specific recommendations
    public static readonly string SolutionFilePermissions =
            // Linux Suggestion
            OperatingSystem.IsLinux() ? 
            """
            TODO: Actually write a solution for this.
            """ :
            // Default is Windows suggestion
            "1. Executing this tool with administrator permissions.";
}