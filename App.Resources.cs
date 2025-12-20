using System;
using System.IO;

namespace GottaManagePlus;

public enum PageNames
{
    Home,
    Settings,
    Profiles
}

public static class Constants
{
    // Common paths for Steam to store its games
    public static readonly string BaldiPlusFolderSteamPath = 
        // IF Windows, use the following path
        OperatingSystem.IsWindows() ? Path.Combine(Path.GetPathRoot(Environment.SystemDirectory) ?? "C:", "Program Files (86x)", "Steam", "steamapps",
            "common", "Baldi's Basics Plus") : 
        // IF macOS, use following path
            OperatingSystem.IsMacOS() ? Path.Combine(Path.GetPathRoot(Environment.SystemDirectory) ?? "~", "Library", "Application Support", "Steam",
                "steamapps", "common", "Baldi's Basics Plus") : 
            // IF Linux, well, leave the user to show where
            string.Empty;
    
    // Categories for debugging
    public const string DebugWarning = "Warning";
}