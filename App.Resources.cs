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
    public static readonly string WindowsBaldiPlusFolderSteamPath = OperatingSystem.IsWindows() ?
        Path.Combine(Path.GetPathRoot(Environment.SystemDirectory) ?? "C:", "Program Files (86x)", "Steam", "steamapps",
            "common", "Baldi's Basics Plus") : string.Empty;
    public static readonly string MacOsBaldiPlusFolderSteamPath = OperatingSystem.IsMacOS() ?
        Path.Combine(Path.GetPathRoot(Environment.SystemDirectory) ?? "~", "Library", "Application Support", "Steam",
            "steamapps", "common", "Baldi's Basics Plus") : string.Empty;
}