using GottaManagePlus.Interfaces.GameEnvironment;
using GottaManagePlus.Models;
using GottaManagePlus.Models.SourceGenerators;
using Serilog;
using Tomlyn;

namespace GottaManagePlus.Services.GameEnvironmentServices;

public sealed class GameEnvironmentSnapshotReader(ILogger logger) : IGameEnvironmentSnapshotReader
{
    // ----- Private API -----
    private readonly ILogger _logger = logger;

    // ----- Public API -----
    /// <summary>
    /// Reads the snapshot as a <see cref="EnvironmentSnapshot"/> abstraction.
    /// </summary>
    /// <param name="indexFilePath">The index file to be scanned.</param>
    /// <returns>An instance of <see cref="EnvironmentSnapshot"/> if the deserialization is a success; otherwise, <see langword="null"/>.</returns>
    public EnvironmentSnapshot? ReadSnapshot(string indexFilePath)
    {
        if (!File.Exists(indexFilePath))
        {
            _logger.Warning("Snapshot file not found: {Path}", indexFilePath);
            return null;
        }

        try
        {
            var toml = File.ReadAllText(indexFilePath);
            var snapshot = TomlSerializer.Deserialize<EnvironmentSnapshot>(toml, EnvironmentSnapshotContext.Default);
            _logger.Information("Snapshot loaded from {Path} (taken at {Time})", indexFilePath, snapshot?.CreationTimeUtc);
            return snapshot;
        }
        catch (System.Exception ex)
        {
            _logger.Error(ex, "Failed to read snapshot from {Path}", indexFilePath);
            return null;
        }
    }
}