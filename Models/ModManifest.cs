using System.Collections.Generic;
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
    [JsonRequired] public List<DestinedAsset> Assets { get; set; } = [];
    [JsonRequired] public List<string> Plugins { get; set; } = []; // string here means the LocalPath
    
    // ---- Internal -----
    [JsonIgnore] public string? MetadataPath = null;
    [JsonIgnore] public string? Thumbnail { get; set; }
    [JsonIgnore] public string? InstallationUrl { get; set; } // Supports Gamebanana and Github for now
    [JsonIgnore] public List<string>? DependenciesUrls { get; set; }
    [JsonIgnore] public List<string>? SupportedPlusVersions { get; set; }
    
    // ### Important Fields ###
    [JsonIgnore] public bool InstalledInGame { get; set; } // Tells whether the mod is in the right plugins folder or not.
    [JsonIgnore] public bool Activated { get; set; } // Whether the mod is active or not.
}

public class DestinedAsset
{
    [JsonRequired]
    public required string LocalPath { get; set; }
    
    [JsonRequired]
    public required string Destination { get; set; }
}