using System;
using System.IO;

namespace GottaManagePlus;

public enum PageNames
{
    Home,
    Settings
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
    
    // Root folder of the app
    public const string AppRootFolder = ".gmp";
    
    // Categories for debugging
    public const string DebugWarning = "Warning", DebugInfo = "Info", DebugError = "Error";
}