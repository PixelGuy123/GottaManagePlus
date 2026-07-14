/*
This file is part of GottaManagePlus (https://github.com/PixelGuy123/GottaManagePlus)

    Copyright (C) 2026 PixelGuy123

    This program is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with this program.  If not, see <https://www.gnu.org/licenses/>.

*/

using Avalonia;
using Avalonia.Platform.Storage;
using GottaManagePlus.Models;

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
    public static readonly Uri DiscordLink = new("https://discord.gg/p2mpGsKAfG"),
        GithubLink = new("https://github.com/PixelGuy123/GottaManagePlus");
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
    
    // LICENSE Path
    public static readonly string LicensePath = Path.Combine(ApplicationLocation, "LICENSE");

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
    
    // License Description
    public const string LicenseDialogDescription = """
                                             Pixel Guy

                                             Copyright (C) 2026 Pixel Guy

                                             This program is free software: you can redistribute it and/or modify
                                             it under the terms of the GNU General Public License as published by
                                             the Free Software Foundation, either version 3 of the License, or
                                             (at your option) any later version.

                                             This program is distributed in the hope that it will be useful,
                                             but WITHOUT ANY WARRANTY; without even the implied warranty of
                                             MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
                                             GNU General Public License for more details.

                                             You should have received a copy of the GNU General Public License
                                             along with this program. If not, see <https://www.gnu.org/licenses/>.

                                             --------------------------------------------------

                                             For questions, permissions, or source code requests, please contact:
                                             <stickmoderator123@gmail.com>

                                             This program comes with ABSOLUTELY NO WARRANTY.
                                             This is free software, and you are welcome to redistribute it
                                             under certain conditions. See the GNU GPL v3 for details.

                                             Full license text is available in the LICENSE file included
                                             with this program.
                                             
                                             You can open it through this dialog as a shortcut.
                                             """;

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