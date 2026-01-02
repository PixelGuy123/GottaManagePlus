using System.Text.Json.Serialization;

namespace GottaManagePlus.Models;

public class ModMetadata
{
    // Sub Categories
    public class ModResourcesMetaData
    {
        [JsonRequired] public required string ResourcePath { get; set; }
        [JsonRequired] public required string ResourceDestination { get; set; }
    }
    
    // JSON Ignored
    [JsonIgnore] public string? MetadataPath = null;
    [JsonIgnore] public string[]? SupportedVersions = null;
    
    // General Information
    [JsonRequired] public required string Name { get; set; } = "Mod";
    [JsonRequired] public required string Author { get; set; } = "Developer";
    [JsonRequired] public required string Version { get; set; } = "0.0.0";
    [JsonRequired] public required string GamebananaUrl { get; set; }
    public required string? Thumbnail { get; set; } = null;
    
    // Plugin Information
    [JsonRequired] public required ModResourcesMetaData[] Plugins { get; set; }
    
    // Assets Information
    public required ModResourcesMetaData[] Assets { get; set; } = [];

}