using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using GottaManagePlus.Interfaces.GameEnvironment;
using GottaManagePlus.Models;
using GottaManagePlus.Models.SourceGenerators;
using Serilog;
using Tomlyn;

namespace GottaManagePlus.Services.GameEnvironmentServices;

public sealed class GameEnvironmentSnapshotWriter(ILogger logger) : IGameEnvironmentSnapshotWriter
{
    // ----- Private API -----
    private readonly ILogger _logger = logger;

    // ----- Public API -----
    /// <summary>
    /// Writes a snapshot to the expected index file.
    /// </summary>
    /// <param name="rootPath">The root path of the environment.</param>
    /// <param name="writeToPath">The path of the file to be serialized.</param>
    public async Task WriteSnapshotAsync(string rootPath, string writeToPath)
    {
        _logger.Information("Creating environment snapshot of {RootPath} ...", rootPath);
        
        var snapshot = new EnvironmentSnapshot
        {
            CreationTimeUtc = DateTime.UtcNow
        };

        try
        {
            // Enumerate directories and files recursively.
            var directoryQueue = new Queue<string>();
            directoryQueue.Enqueue(rootPath);

            while (directoryQueue.Count > 0)
            {
                var currentDir = directoryQueue.Dequeue();
                var relativeDir = Path.GetRelativePath(rootPath, currentDir);
                if (!string.IsNullOrEmpty(relativeDir) && relativeDir != ".")
                    snapshot.Directories.Add(relativeDir);

                foreach (var subDir in Directory.EnumerateDirectories(currentDir))
                    directoryQueue.Enqueue(subDir);

                foreach (var filePath in Directory.EnumerateFiles(currentDir))
                {
                    var fileInfo = new FileInfo(filePath);
                    snapshot.Files.Add(new EnvironmentSnapshot.SnapshotFileEntry
                    {
                        RelativePath = Path.GetRelativePath(rootPath, filePath),
                        LastWriteTimeUtc = fileInfo.LastWriteTimeUtc,
                        SizeBytes = fileInfo.Length
                    });
                }
            }

            // Serialize to TOML and write to file.
            var toml = TomlSerializer.Serialize(snapshot, EnvironmentSnapshotContext.Default);
            await File.WriteAllTextAsync(writeToPath, toml);

            _logger.Information("Snapshot saved to {IndexFile} with {FileCount} files, {DirCount} directories.",
                writeToPath, snapshot.Files.Count, snapshot.Directories.Count);
        }
        catch(Exception e)
        {
            _logger.Error(e, "Failed to take a snapshot of the environment.");
        }
    }
}