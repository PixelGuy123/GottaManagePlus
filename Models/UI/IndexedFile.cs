/*
This file is part of GottaManagePlus (https://github.com/PixelGuy123/GottaManagePlus)

    Copyright (C) 2026 PixelGuy123

    This program is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with this program.  If not, see <https://www.gnu.org/licenses/>.

*/

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
    private static readonly Dictionary<int, IndexedFile> IdIndexFileCache = [];
    
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
            switch (element.ValueKind)
            {
                case JsonValueKind.String:
                    // Leaf node: add the file
                    var fileName = element.GetString();
                    var fullPath = string.IsNullOrEmpty(currentPath) ? fileName : currentPath + CharSeparator + fileName;
    
                    // ONLY collect the path if it contains your special mod folder name
                    if (fullPath?.Contains(Constants.App_SpecialFolderForMods_Name) == true)
                        _collectedPaths.Add((fullPath, fileName)!);
                    
                    break;

                case JsonValueKind.Object:
                    // Recurse into each property
                    foreach (var property in element.EnumerateObject())
                    {
                        var newPath =
                            int.TryParse(property.Name, out _)
                                ? currentPath
                                : string.IsNullOrEmpty(currentPath)
                                    ? property.Name
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
        }
    }
    
    // ---- Public ----
    /// <summary>
    /// Whether the mapping contains the GMP root folder or not.
    /// </summary>
    public bool HasGmpRoot => _collectedPaths.Count != 0;
    
    /// <summary>
    /// Finds a file name by the path given.
    /// </summary>
    /// <param name="paths">The path to be formed.</param>
    /// <returns>A string containing the full file name.</returns>
    public string FindFileByName(params string[] paths)
    {
        var fullPath = string.Join(CharSeparator, paths);
        var fileName = paths[^1];
        return _collectedPaths.Find(p => 
            p.FullPath.Equals(fullPath, StringComparison.OrdinalIgnoreCase) &&
            p.FileName.StartsWith(fileName, StringComparison.OrdinalIgnoreCase)).FileName;
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
        if (IdIndexFileCache.TryGetValue(id.GetInt32(), out var indexedFile)) return indexedFile;

        // Create a new instance from the document.
        indexedFile = new IndexedFile(document);

        // Cache it with the ID.
        IdIndexFileCache[id.GetInt32()] = indexedFile;

        return indexedFile;
    }
}