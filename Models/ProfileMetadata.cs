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

using Tomlyn.Serialization;

namespace GottaManagePlus.Models;

/// <summary>
/// An object that represents the basic data inside a compressed profile.
/// </summary>
public class ProfileMetadata()
{
    [TomlIgnore] public const string DefaultName = "Default";
    
    // Default instance
    /// <summary>
    /// Returns a default instance of <see cref="ProfileMetadata"/> with filled data.
    /// </summary>
    [TomlIgnore]
    public static ProfileMetadata Default => new();

    /// <summary>
    /// A constructor to deep-copy the <see cref="ProfileMetadata"/>.
    /// </summary>
    /// <param name="toCopy">The metadata to be copied as a new instance.</param>
    /// <param name="excludeProfileContent">
    /// <see langword="true"/> means the configs, patchers and mods will be empty
    /// regardless of current value.
    /// </param>
    public ProfileMetadata(ProfileMetadata toCopy, bool excludeProfileContent) : this()
    {
        Name = toCopy.Name;

        // If true, exclude the lists
        if (excludeProfileContent) return;

        ConfigurationFiles = new List<string>(toCopy.ConfigurationFiles);
        PatcherFiles = new List<string>(toCopy.PatcherFiles);
        ModDataFiles = new List<ModManifest>(toCopy.ModDataFiles);
    }
    
    // [Basic Info]
    [TomlRequired] public string Name { get; set; } = DefaultName;
    
    // [Profile Content]
    /*
     * What should the profile expect from this data?
     * CONFIGS & PATCHERS: the profile should expect the direct path to them (the destination path).
     * In this case, the profile itself should always update and scan for new configuration files and patcher files (handled by ProfileStorage).
     * PLUGINS & ASSETS: These are special.
     * The profile should expect the direct path to the DLL files (plugins).
     * For assets, they should expect the LocalPath and Destination to remain the same (immutable data).
     * However, the destination must always be used to localize the folders.
     */
    public List<string> ConfigurationFiles { get; set; } = [];
    public List<string> PatcherFiles { get; set; } = [];
    public List<ModManifest> ModDataFiles { get; set; } = [];
    
    // Overriden Methods
    public override string ToString() => Name;
}