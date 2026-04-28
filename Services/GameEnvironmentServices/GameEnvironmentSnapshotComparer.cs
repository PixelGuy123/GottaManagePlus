using System.Collections.Generic;
using System.Linq;
using GottaManagePlus.Interfaces.GameEnvironment;
using GottaManagePlus.Models;

namespace GottaManagePlus.Services.GameEnvironmentServices;

public sealed class GameEnvironmentSnapshotComparer : IGameEnvironmentSnapshotComparer
{
    /// <summary>
    /// Compares each snapshot.
    /// </summary>
    /// <param name="current">The current snapshot.</param>
    /// <param name="previous">The previous snapshot.</param>
    /// <returns><see langword="true"/> if both snapshots contain any differences between each other; otherwise, <see langword="false"/>.</returns>
    public bool Compare(EnvironmentSnapshot current, EnvironmentSnapshot previous)
    {
        // Compare directories.
        var currentDirs = new HashSet<string>(current.Directories);
        var previousDirs = new HashSet<string>(previous.Directories);

        // If directories have anything new, return true.
        if (currentDirs.Except(previousDirs).Any() || previousDirs.Except(currentDirs).Any())
            return true;

        // Compare files (keyed by relative path).
        var currentFiles = current.Files.ToDictionary(f => f.RelativePath, f => f);
        var previousFiles = previous.Files.ToDictionary(f => f.RelativePath, f => f);

        var allPaths = new HashSet<string>(currentFiles.Keys);
        allPaths.UnionWith(previousFiles.Keys);

        // Check all paths from each file.
        foreach (var path in allPaths)
        {
            var hasCurrent = currentFiles.TryGetValue(path, out var currentFile);
            var hasPrevious = previousFiles.TryGetValue(path, out var previousFile);

            switch (hasCurrent)
            {
                case true when !hasPrevious:
                case false when hasPrevious:
                    return true;
                case true when hasPrevious:
                {
                    // Check modification by timestamp or size.
                    if (currentFile!.LastWriteTimeUtc != previousFile!.LastWriteTimeUtc ||
                        currentFile.SizeBytes != previousFile.SizeBytes)
                        return true;
                    
                    break;
                }
            }
        }

        return false;
    }
}