using GottaManagePlus.Utils.JsonConverters;
using System.Text.Json.Serialization;

namespace GottaManagePlus.Models;

#region OsPath Struct
/// <summary>
/// Represents an operating system path with automatic normalization for Windows long paths.
/// </summary>
[JsonConverter(typeof(OsPathJsonConverter))]
public readonly partial struct OsPath : IEquatable<OsPath>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="OsPath"/> struct with the specified path.
    /// </summary>
    /// <param name="path">The raw path string to normalize.</param>
    public OsPath(string path)
    {
        BasePath = path;
        NormalizedPath = Normalize(path);
    }

    /// <summary>
    /// Gets the normalized path.
    /// </summary>
    public string NormalizedPath { get; }
    
    /// <summary>
    /// Gets the base path stored internally in this instance.
    /// </summary>
    public string BasePath { get; }

    /// <summary>
    /// Gets a value indicating whether the path refers to an existing directory.
    /// Falls back to checking if the path ends with a directory separator character
    /// when file attributes cannot be retrieved.
    /// </summary>
    public bool IsDirectory
    {
        get
        {
            if (string.IsNullOrEmpty(NormalizedPath))
                return false;

            try
            {
                var attrs = File.GetAttributes(NormalizedPath);
                return (attrs & FileAttributes.Directory) != 0;
            }
            catch
            {
                return NormalizedPath[^1] == Path.DirectorySeparatorChar;
            }
        }
    }

    /// <summary>
    /// Gets a value indicating whether the path refers to an existing file.
    /// Falls back to checking that the path does not end with a directory separator character
    /// when file attributes cannot be retrieved.
    /// </summary>
    public bool IsFile => !IsDirectory;

    // Implicit Operators
    public static implicit operator OsPath(string path) => new(path);
    public static implicit operator string(OsPath osPath) => osPath.BasePath;
    public static bool operator ==(OsPath a, OsPath b) => a.Equals(b);
    public static bool operator !=(OsPath a, OsPath b) => !(a == b);

    /// <summary>
    /// Determines whether this instance is equal to another <see cref="OsPath"/>.
    /// </summary>
    /// <param name="other">The other <see cref="OsPath"/> to compare.</param>
    /// <returns><c>true</c> if both paths are equal; otherwise, <c>false</c>.</returns>
    public bool Equals(OsPath other) => string.Equals(NormalizedPath, other.NormalizedPath, StringComparison.OrdinalIgnoreCase);

    /// <inheritdoc/>
    public override bool Equals(object? obj) => obj is OsPath other && Equals(other);

    /// <inheritdoc/>
    public override int GetHashCode() => NormalizedPath?.GetHashCode() ?? 0;

    /// <inheritdoc/>
    public override string ToString() => BasePath;

    /// <summary>
    /// Normalizes the given path by applying the Windows long-path prefix and converting
    /// forward slashes to backslashes when running on Windows.
    /// </summary>
    /// <param name="path">The raw path string.</param>
    /// <returns>The normalized path string.</returns>
    private static string Normalize(string path)
    {
        if (string.IsNullOrEmpty(path))
            return path;

        const string longPathPrefix = @"\\?\";
        const char windowsSpecialSeparator = '\\';
        if (!OperatingSystem.IsWindows())
        {
            // Revert the Windows pattern if it happens to be one.
            if (path.StartsWith(longPathPrefix, StringComparison.OrdinalIgnoreCase))
            {
                path = path.Substring(longPathPrefix.Length,
                    path.Length - longPathPrefix.Length - 1)
                    .Replace(windowsSpecialSeparator, Path.DirectorySeparatorChar);
            }
            return path;
        }


        if (!path.StartsWith(longPathPrefix, StringComparison.OrdinalIgnoreCase))
            path = longPathPrefix + path;

        path = path.Replace('/', windowsSpecialSeparator);
        return path;
    }
}
#endregion
