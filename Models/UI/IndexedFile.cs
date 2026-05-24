using System.Text.Json;

namespace GottaManagePlus.Models.UI;

/// <summary>
/// An abstract representation of the file tree.
/// </summary>
public class IndexedFile
{
    // The full collection of paths
    private readonly List<(string FullPath, string FileName)> _collectedPaths = [];
    public const char CharSeparator = '/';
    
    // Cache for IndexedFiles
    private static readonly Dictionary<int, IndexedFile> idIndexFileCache = [];
    
    private IndexedFile(JsonDocument document)
    {
        const string fileTreeArchive = "_aArchiveFileTree";
        var root = document.RootElement;
        
        // Try to check if the document has the expected property.
        if (!root.TryGetProperty(fileTreeArchive, out var archive))
            throw new ArgumentException($"JSON Document does not contain {fileTreeArchive} property.", nameof(document));
        
        // Current path recorded.
        SearchFurther(archive, string.Empty);
        
        return;
        
        void SearchFurther(JsonElement element, string currentPath)
        {
            // Limit the search to .gmp/ root only
            if (!IsCurrentPathRaw(currentPath) && !currentPath.StartsWith(Constants.App_SpecialFolderForMods_Name))
                return;
            
            switch (element.ValueKind)
            {
                case JsonValueKind.String:
                    // Leaf node: add the file
                    var fileName = element.GetString();
                    var fullPath = IsCurrentPathRaw(currentPath) ? fileName : currentPath + CharSeparator + fileName;
                    _collectedPaths.Add((fullPath, fileName)!);
                    break;

                case JsonValueKind.Object:
                    // Recurse into each property
                    foreach (var property in element.EnumerateObject())
                    {
                        var newPath = 
                            IsCurrentPathRaw(currentPath) ? property.Name 
                            : currentPath + CharSeparator + property.Name;
                        SearchFurther(property.Value, newPath);
                    }
                    break;

                case JsonValueKind.Array:
                    // Recurse into each array element (no path change)
                    foreach (var item in element.EnumerateArray())
                        SearchFurther(item, currentPath);
                    break;

                case JsonValueKind.Undefined:
                case JsonValueKind.Number:
                case JsonValueKind.True:
                case JsonValueKind.False:
                case JsonValueKind.Null:
                default:
                    // Ignore other types (numbers, booleans, null, etc.)
                    break;
            }

            return;

            static bool IsCurrentPathRaw(string currentPath) => string.IsNullOrEmpty(currentPath) || int.TryParse(currentPath, out _);
        }
    }
    
    // ---- Public ----
    /// <summary>
    /// Whether the mapping contains the GMP root folder or not.
    /// </summary>
    public bool HasGMPRoot => _collectedPaths.Exists(p =>
        p.FullPath.StartsWith(Constants.App_SpecialFolderForMods_Name, StringComparison.OrdinalIgnoreCase));
    
    /// <summary>
    /// Finds a file name by the path given.
    /// </summary>
    /// <param name="paths">The path to be formed.</param>
    /// <returns>A string containing the full file name.</returns>
    public string? FindFileByName(params string[] paths)
    {
        var fullPath = string.Join(CharSeparator, paths);
        var fileName = paths[^1];
        return _collectedPaths.Find(p => 
            p.FullPath.Equals(fullPath, StringComparison.OrdinalIgnoreCase) &&
            p.FileName.Equals(fileName, StringComparison.OrdinalIgnoreCase)).FileName;
    }

    // ---- Public Constructor ----
    /// <summary>
    /// A static constructor to get a new instance of <see cref="IndexedFile"/> or a cached one.
    /// </summary>
    /// <param name="document">The document root to look for.</param>
    /// <returns>An instance of <see cref="IndexedFile"/>.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if the document lacks an identifiable property.</exception>
    public static IndexedFile CreateOrGetIndexedFile(JsonDocument document)
    {
        const string fileId = "_idRow";
        var root = document.RootElement;
        if (!root.TryGetProperty(fileId, out var id))
            throw new ArgumentOutOfRangeException(nameof(document),
                "Document does not contain an identifiable number.");

        // Try to get from cache the indexed file.
        if (idIndexFileCache.TryGetValue(id.GetInt32(), out var indexedFile)) return indexedFile;

        // Create a new instance from the document.
        indexedFile = new IndexedFile(document);

        // Cache it with the ID.
        idIndexFileCache[id.GetInt32()] = indexedFile;

        return indexedFile;
    }
}