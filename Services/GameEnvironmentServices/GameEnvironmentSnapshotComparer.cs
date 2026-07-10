using GottaManagePlus.Interfaces.GameEnvironment;
using GottaManagePlus.Models;
using GottaManagePlus.Models.GameEnvironments;
using Serilog;

namespace GottaManagePlus.Services.GameEnvironmentServices;

public sealed class GameEnvironmentSnapshotComparer(ILogger logger) : IGameEnvironmentSnapshotComparer
{
    // ---- Private ----
    private readonly ILogger _logger = logger;
    
    // ---- Public ----
    /// <summary>
    /// Compares each snapshot and return a value correspondent to the comparison check.
    /// </summary>
    /// <param name="current">The current snapshot.</param>
    /// <param name="previous">The previous snapshot.</param>
    /// <returns><see langword="true"/> if both snapshots contain any differences between each other; otherwise, <see langword="false"/>.</returns>
    public bool Compare(EnvironmentSnapshot current, EnvironmentSnapshot previous)
    {
        // Announce comparison
        _logger.Information("SNAPSHOT COMPARISON — (Previous: '{previous}') — (Current: '{current}')", 
            previous.CreationTimeUtc, current.CreationTimeUtc);
        // Compare directories.
        var currentDirs = new HashSet<string>(current.Directories);
        var previousDirs = new HashSet<string>(previous.Directories);

        // If directories have anything new, return true.
        if (currentDirs.Count != previousDirs.Count)
        {
            _logger.Information(
                "COMPARISON RESULT: Difference found between directory sizes ({previousDirs} ≠ {currentDirs}).",
                currentDirs.Count, previousDirs.Count);

            var count = 0;
            _logger.Information("-------");
            foreach (var divergence in currentDirs.Count > previousDirs.Count
                         ? currentDirs.Except(previousDirs)
                         : previousDirs.Except(currentDirs))
            {
                // Log directory.
                _logger.Information("\t{count}: '{directory}'", count, divergence);
                
                // If count goes above 50, stop here.
                if (count++ <= 50) continue;
                _logger.Information("And more...");
                break;
            }
            _logger.Information("-------");
            
            return true;
        }

        // Compare files (keyed by relative path).
        var currentFiles = current.Files.ToDictionary(f => f.RelativePath!);
        var previousFiles = previous.Files.ToDictionary(f => f.RelativePath!);

        var allPaths = new HashSet<string>(currentFiles.Keys);
        allPaths.UnionWith(previousFiles.Keys);

        // Check all paths from each file.
        foreach (var path in allPaths)
        {
            var hasCurrent = currentFiles.TryGetValue(path, out var currentFile);
            var hasPrevious = previousFiles.TryGetValue(path, out var previousFile);

            switch (hasCurrent)
            {
                case true when !hasPrevious: // If the current has a file that previous does not...
                case false when hasPrevious: // If the previous has a file that current does not...
                    _logger.Information(
                        "COMPARISON RESULT: File ('{path}') difference found in path (Previous: {prev} ≠ Current: {current}).",
                        path, hasPrevious, hasCurrent);
                    return true;
                case true when hasPrevious: // If both have the file, check the file properties.
                {
                    // Check modification by timestamp.
                    if (currentFile!.LastWriteTimeUtc != previousFile!.LastWriteTimeUtc)
                    {
                        _logger.Information(
                            "COMPARISON RESULT: File ('{path}') difference found in write time (Previous: '{prevTime}' ≠ Current: '{curTime}').",
                            path, previousFile.LastWriteTimeUtc, currentFile.LastWriteTimeUtc);
                        return true;
                    }
                    
                    // Check modification by size.
                    if (currentFile.SizeBytes != previousFile.SizeBytes)
                    {
                        _logger.Information(
                            "COMPARISON RESULT: File ('{path}') difference found in size (Previous: '{prevSize}' ≠ Current: '{curSize}').",
                            path, previousFile.SizeBytes, currentFile.SizeBytes);
                        return true;
                    }
                    
                    break;
                }
            }
        }
        _logger.Information("COMPARISON RESULT: Both snapshots are equal!");
        return false;
    }
}