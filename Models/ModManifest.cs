using System.Collections.Generic;
using System.IO;
using System.Text.Json.Serialization;

namespace GottaManagePlus.Models;

// Representation of a metadata of an outside mod
public class ModManifest
{
    // General Information
    [JsonRequired] public required string Name { get; set; } = "Mod";
    [JsonRequired] public required string Author { get; set; } = "Developer";
    [JsonRequired] public required string Version { get; set; } = "0.0.0"; 
    public required string Description { get; set; }
    
    // Assets
    [JsonRequired] public List<DestinedAsset> Assets { get; set; } = []; // Assets must always be a directory, they cannot be a file.
    [JsonRequired] public List<string> Plugins { get; set; } = []; // string here means the LocalPath

    [JsonIgnore] public ModMetadata Metadata { get; set; } = new();
    [JsonIgnore] public List<string> SupportedVersions { get; set; } = [];
}

public class ModMetadata
{
    // ---- Internal -----
    public string? Path = null;
    public string? Thumbnail { get; set; }
    public string? InstallationUrl { get; set; } // Supports Gamebanana and GitHub for now
    public List<string>? DependenciesUrls { get; set; }
    public List<string>? SupportedPlusVersions { get; set; }
    
    // ### Important Fields ###
    public bool InstalledInGame { get; set; } // Tells whether the mod is in the right plugins folder or not.
    public bool Activated { get; set; } // Whether the mod is active or not.
}

public class DestinedAsset
{
    [JsonRequired]
    public required string LocalPath { get; set; }
    [JsonRequired]
    public required string Destination { get; set; }
    /// <summary>
    /// Combines the asset's final location with the actual file to go there.
    /// </summary>
    public string MovedAsset => Path.Combine(Destination, Path.GetFileName(LocalPath));
}