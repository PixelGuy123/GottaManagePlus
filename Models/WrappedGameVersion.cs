using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

namespace GottaManagePlus.Models;

public class WrappedGameVersion : IComparable, IComparable<WrappedGameVersion>
{
    public Version WrappedVersion { get; } = new();
    public int? RevisionNumber { get; } 
    
    // Private parameterless constructor used only for deserialization
    [JsonConstructor]
    public WrappedGameVersion() { }

    public WrappedGameVersion(string version)
    {
        if (string.IsNullOrWhiteSpace(version))
            throw new ArgumentException("Version string cannot be null or empty.");

        // Identify the trailing letter
        var lastChar = version[^1];

        if (!char.IsLetter(lastChar))
        {
            WrappedVersion = new Version(version);
            return;
        }
        
        // Remove the letter to get the base numeric part
        var numericPart = version[..^1];
            
        // Calculate revision: 'a' = 1, 'b' = 2...
        RevisionNumber = char.ToLower(lastChar) - 'a' + 1;

        // Combine the numeric part with the new revision number
        WrappedVersion = new Version($"{numericPart}.{RevisionNumber}");
    }

    public override string ToString()
    {
        // If there's no revision, this is a normal version
        if (RevisionNumber is not > 0) return WrappedVersion.ToString();
        
        // Map 1 back to 'a', 2 to 'b'...
        var suffix = (char)(RevisionNumber + 'a' - 1);
            
        // Return formatted version string
        return $"{WrappedVersion.Major}.{WrappedVersion.Minor}{suffix}";
    }

    public override bool Equals(object? obj) =>
        obj switch
        {
            Version ver => ver == WrappedVersion,
            WrappedGameVersion wrapVer => wrapVer.WrappedVersion == WrappedVersion,
            _ => obj == this
        };

    public override int GetHashCode() => HashCode.Combine(WrappedVersion.GetHashCode(), (RevisionNumber ?? 0).GetHashCode());

    public int CompareTo(object? obj) =>
        obj switch
        {
            null => 1,
            Version ver => WrappedVersion.CompareTo(ver),
            WrappedGameVersion wrapVer => WrappedVersion.CompareTo(wrapVer.WrappedVersion),
            _ => throw new ArgumentException($"Cannot compare WrappedGameVersion with {obj.GetType().Name}")
        };

    public int CompareTo(WrappedGameVersion? other) =>
        other switch
        {
            null => 1,
            _ => WrappedVersion.CompareTo(other.WrappedVersion)
        };
}