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

using GottaManagePlus.Interfaces.GameEnvironment;
using GottaManagePlus.Models;
using Serilog;
using Tomlyn;
using EnvironmentSnapshotContext = GottaManagePlus.Utils.SourceGenerators.EnvironmentSnapshotContext;

namespace GottaManagePlus.Services.GameEnvironmentServices;

public sealed class GameEnvironmentSnapshotReader(ILogger logger) : IGameEnvironmentSnapshotReader
{
    // ----- Private -----
    private readonly ILogger _logger = logger;

    // ----- Public -----
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