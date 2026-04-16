using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json.Serialization;
using GottaManagePlus.Utils.Collections;

namespace GottaManagePlus.Models;

// Representation of a metadata of an outside mod
public class ModManifest
{
    // General Information
    [JsonRequired] public string Name { get; set; } = "Mod";
    [JsonRequired] public string Author { get; set; } = "Developer";
    [JsonRequired] public string Version { get; set; } = "0.0.0"; 
    [JsonRequired] public string? Description { get; set; }
    
    // Assets
    [JsonRequired] public List<DestinedAsset> Assets { get; set; } = []; // Assets must always be a directory, they cannot be a file.
    [JsonRequired] public List<string> Plugins { get; set; } = []; // string here means the LocalPath, they must always be a file and linked to a .dll.
    [JsonRequired] public List<string> Patchers { get; set; } = []; // patcher here means the BepInEx/Patchers.

    [JsonIgnore] public ModMetadata Metadata { get; set; } = new();
    [JsonIgnore] public bool SupportsCurrentVersion { get; set; }
}

public class ModMetadata
{
    // ---- Internal -----
    public string? Path = null;
    public string? Thumbnail = null;
    public string? InstallationUrl { get; set; } // Supports Gamebanana and GitHub for now
    public List<string> DependenciesUrls { get; set; } = [];
    public AutoSortedList<WrappedGameVersion> SupportedPlusVersions { get; set; } = []; // Automatically sorted by high order
    public DateOnly LastUpdateDate { get; set; }
    
    // ### Important Fields ###
    public bool Activated { get; set; } // Whether the mod is active or not.
}

public struct DestinedAsset
{
    [JsonRequired]
    public required string LocalPath { get; set; }
    public string? Destination { get; set; }
    /// <summary>
    /// Combines the asset's final location with the actual file to go there.
    /// </summary>
    public string MovedAsset => !string.IsNullOrEmpty(Destination) ? 
        Path.Combine(Destination, Path.GetFileName(LocalPath)) : throw new NullReferenceException("Invalid destination set.");
}