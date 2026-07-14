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

using ByteSizeLib;
using GottaManagePlus.Interfaces.GameEnvironment;
using GottaManagePlus.Models;
using Serilog;
using Tomlyn;
using EnvironmentSnapshotContext = GottaManagePlus.Utils.SourceGenerators.EnvironmentSnapshotContext;

namespace GottaManagePlus.Services.GameEnvironmentServices;

public sealed class GameEnvironmentSnapshotWriter(ILogger logger) : IGameEnvironmentSnapshotWriter
{
    // ----- Private -----
    private readonly ILogger _logger = logger;

    // ----- Public -----
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
                
                // If the path is the root folder of the application, ignore it.
                if (Path.GetFileName(currentDir)
                        .Equals(Constants.App_RootFolder)) continue;
                
                var relativeDir = Path.GetRelativePath(rootPath, currentDir);
                if (!string.IsNullOrEmpty(relativeDir) && relativeDir != ".")
                    snapshot.Directories.Add(relativeDir);

                foreach (var subDir in Directory.EnumerateDirectories(currentDir))
                    directoryQueue.Enqueue(subDir);

                foreach (var filePath in Directory.EnumerateFiles(currentDir))
                {
                    // Ignore own write path
                    if (filePath.Equals(writeToPath, StringComparison.OrdinalIgnoreCase)) continue;
                    
                    var fileInfo = new FileInfo(filePath);
                    snapshot.Files.Add(new EnvironmentSnapshot.SnapshotFileEntry
                    {
                        RelativePath = Path.GetRelativePath(rootPath, filePath),
                        LastWriteTimeUtc = fileInfo.LastWriteTimeUtc,
                        SizeBytes = ByteSize.FromBytes(fileInfo.Length)
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