using System.Text.Json.Serialization;

namespace GottaManagePlus.Models;

public class ModMetadata
{
    [JsonRequired]
    public required string Name { get; set; }
    [JsonRequired]
    public required string Author { get; set; }
    [JsonRequired]
    public required string GamebananaUrl { get; set; }
    
}