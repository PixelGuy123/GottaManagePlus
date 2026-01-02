using System;

namespace GottaManagePlus.Models;

public class WrappedGameVersion
{
    public Version WrappedVersion { get; }

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
        var numericPart = version.Substring(0, version.Length - 1);
            
        // Calculate revision: 'a' = 1, 'b' = 2...
        var revision = char.ToLower(lastChar) - 'a' + 1;

        // Combine the numeric part with the new revision number
        WrappedVersion = new Version($"{numericPart}.{revision}");
    }

    public override string ToString()
    {
        // If there's no revision, this is a normal version
        if (WrappedVersion.Revision <= 0) return WrappedVersion.ToString();
        
        // Map 1 back to 'a', 2 to 'b'...
        var suffix = (char)(WrappedVersion.Revision + 'a' - 1);
            
        // Return formatted version string
        return $"{WrappedVersion.Major}.{WrappedVersion.Minor}.{WrappedVersion.Build}{suffix}";
    }

    public override bool Equals(object? obj) =>
        obj switch
        {
            Version ver => ver == WrappedVersion,
            WrappedGameVersion wrapVer => wrapVer.WrappedVersion == WrappedVersion,
            _ => obj == this
        };

    public override int GetHashCode() => WrappedVersion.GetHashCode();
}