using System.Diagnostics.CodeAnalysis;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Serialization;
using GottaManagePlus.Utils;
using GottaManagePlus.Utils.Collections;
// ReSharper disable NonReadonlyMemberInGetHashCode

namespace GottaManagePlus.Models;

// Representation of a metadata of an outside mod
public class ModManifest
{
    // General Information
    [JsonRequired] public string Guid { get; set; } = "plus.myDeveloperName.myMod";
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

    public override int GetHashCode() => HashCode.Combine(Guid, Name, Author);
    public override string ToString() => $"{Name}_{GetStableHash()}";
    public string GetStableHash() // For getting a stable hash for the directory
    {
        var input = $"{Guid}:{Name}:{Author}";
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(hash).Substring(0, 8); // First 8 chars only
    }
}

public class ModMetadata
{
    // ---- Internal -----
    [JsonIgnore]
    public string? Path = null;
    [JsonIgnore]
    public string? Thumbnail = null;
    public string? InstallationUrl { get; set; } // Supports Gamebanana and GitHub for now
    public List<string> DependenciesUrls { get; set; } = [];
    public AutoSortedList<WrappedGameVersion> SupportedPlusVersions { get; set; } = []; // Automatically sorted by high order
    public DateOnly LastUpdateDate { get; set; }
    
    // ### Important Fields ###
    public bool Activated { get; set; } // Whether the mod is active or not.
    
    // ---- Access Getters for UI ----
    [JsonIgnore] public List<string> StringifiedSupportedPlusVersions =>
    [..SupportedPlusVersions.Select(p => p.ToString())];
}

public struct DestinedAsset : IEquatable<DestinedAsset>
{
    [JsonRequired]
    public required string LocalPath { get; set; }
    public string? Destination { get; set; }
    
    /// <summary>
    /// Combines the asset's final location with the actual file to go there.
    /// If Destination appears to be a directory path (no file extension), returns the directory name.
    /// </summary>
    public string MovedAsset => !string.IsNullOrEmpty(Destination) ? 
            Path.Combine(Destination, Path.GetFileName(LocalPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar))) : 
        throw new NullReferenceException("Invalid destination set.");

    public override string ToString() =>
        !string.IsNullOrEmpty(Destination)
            ? $"LocalPath: '{LocalPath}' – Destination: '{Destination}' – Moved Asset: '{MovedAsset}'"
            : $"LocalPath: '{LocalPath}'";

    public override bool Equals([NotNullWhen(true)] object? obj) =>
        obj is DestinedAsset destinedAsset &&
        destinedAsset.LocalPath.Equals(LocalPath,
            StringComparison.OrdinalIgnoreCase) && // If the destined asset has equal local path
        (string.IsNullOrEmpty(destinedAsset.Destination) ||
         string.IsNullOrEmpty(
             Destination) || // And both destinations are valid and equal (or if one of them is invalid)
         Destination.Equals(destinedAsset.Destination, StringComparison.OrdinalIgnoreCase)); // return true
    public bool Equals(DestinedAsset other) => LocalPath == other.LocalPath && Destination == other.Destination;
    public override int GetHashCode() => HashCode.Combine(LocalPath, Destination);
    public static bool operator ==(DestinedAsset left, DestinedAsset right) => left.Equals(right);
    public static bool operator !=(DestinedAsset left, DestinedAsset right) => !(left == right);
}