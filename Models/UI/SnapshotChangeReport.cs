using GottaManagePlus.Models;
using GottaManagePlus.Models.GameEnvironments;

namespace GottaManagePlus.Models.UI;

/// <summary>
/// Represents a detailed report of snapshot changes between two environment snapshots.
/// </summary>
public class SnapshotChangeReport
{
    /// <summary>
    /// Whether there are any changes between the snapshots.
    /// </summary>
    public bool HasChanges { get; }

    /// <summary>
    /// The current snapshot (after changes).
    /// </summary>
    public EnvironmentSnapshot? CurrentSnapshot { get; }

    /// <summary>
    /// The previous snapshot (before changes).
    /// </summary>
    public EnvironmentSnapshot? PreviousSnapshot { get; }

    /// <summary>
    /// List of directories that were added.
    /// </summary>
    public List<string> AddedDirectories { get; } = [];

    /// <summary>
    /// List of directories that were removed.
    /// </summary>
    public List<string> RemovedDirectories { get; } = [];

    /// <summary>
    /// List of files that were added.
    /// </summary>
    public List<EnvironmentSnapshot.SnapshotFileEntry> AddedFiles { get; } = [];

    /// <summary>
    /// List of files that were removed.
    /// </summary>
    public List<EnvironmentSnapshot.SnapshotFileEntry> RemovedFiles { get; } = [];

    /// <summary>
    /// List of files that were modified.
    /// </summary>
    public List<EnvironmentSnapshot.SnapshotFileEntry> ModifiedFiles { get; } = [];

    /// <summary>
    /// Creates a new snapshot change report.
    /// </summary>
    /// <param name="hasChanges">Whether there are any changes.</param>
    public SnapshotChangeReport(bool hasChanges)
    {
        HasChanges = hasChanges;
    }

    /// <summary>
    /// Creates a new snapshot change report with detailed information.
    /// </summary>
    /// <param name="hasChanges">Whether there are any changes.</param>
    /// <param name="currentSnapshot">The current snapshot.</param>
    /// <param name="previousSnapshot">The previous snapshot.</param>
    public SnapshotChangeReport(bool hasChanges, EnvironmentSnapshot currentSnapshot, EnvironmentSnapshot previousSnapshot)
        : this(hasChanges)
    {
        CurrentSnapshot = currentSnapshot;
        PreviousSnapshot = previousSnapshot;

        if (!hasChanges) return;

        // Compare directories
        var currentDirs = new HashSet<string>(currentSnapshot.Directories);
        var previousDirs = new HashSet<string>(previousSnapshot.Directories);

        AddedDirectories = currentDirs.Except(previousDirs).ToList();
        RemovedDirectories = previousDirs.Except(currentDirs).ToList();

        // Compare files
        var currentFiles = currentSnapshot.Files.ToDictionary(f => f.RelativePath!);
        var previousFiles = previousSnapshot.Files.ToDictionary(f => f.RelativePath!);

        var allPaths = new HashSet<string>(currentFiles.Keys);
        allPaths.UnionWith(previousFiles.Keys);

        foreach (var path in allPaths)
        {
            var hasCurrent = currentFiles.TryGetValue(path, out var currentFile);
            var hasPrevious = previousFiles.TryGetValue(path, out var previousFile);

            switch (hasCurrent)
            {
                case true when !hasPrevious:
                    AddedFiles.Add(currentFile!);
                    break;
                case false when hasPrevious:
                    RemovedFiles.Add(previousFile!);
                    break;
                case true when hasPrevious:
                {
                    // Check if file was modified
                    if (currentFile!.LastWriteTimeUtc != previousFile!.LastWriteTimeUtc ||
                        currentFile.SizeBytes != previousFile.SizeBytes)
                    {
                        ModifiedFiles.Add(currentFile);
                    }

                    break;
                }
            }
        }
    }

    /// <summary>
    /// Converts this report to a LogContainer for display in the TreeDataGrid.
    /// </summary>
    public LogContainer ToLogContainer()
    {
        var logContainer = new LogContainer();

        if (!HasChanges)
        {
            logContainer.AddInformation("No Changes", "The environment snapshot has not changed.");
            return logContainer;
        }

        // Add directory changes
        foreach (var dir in AddedDirectories)
            logContainer.AddInformation("Directory Added", dir);

        foreach (var dir in RemovedDirectories)
            logContainer.AddWarning("Directory Removed", dir);
        

        // Add file changes
        foreach (var file in AddedFiles)
            logContainer.AddInformation("File Added", $"{file.RelativePath} ({file.SizeBytes.ToString()})");

        foreach (var file in RemovedFiles)
            logContainer.AddWarning("File Removed", $"{file.RelativePath}");
        
        foreach (var file in ModifiedFiles)
            logContainer.AddInformation("File Modified", $"{file.RelativePath} (Size: {file.SizeBytes.ToString()})");

        return logContainer;
    }
}
