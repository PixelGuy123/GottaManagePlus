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
using GottaManagePlus.Models.UI;
using GottaManagePlus.Utils;

namespace GottaManagePlus.Services.GameEnvironmentServices;

/// <summary>
/// A service responsible for determining the currently active environment.
/// </summary>
public sealed class GameEnvironmentController(
    IEnumerable<IGameEnvironmentFactory> factories,
    IGameEnvironmentSnapshotWriter gameEnvironmentSnapshotWriter,
    IGameEnvironmentSnapshotReader gameEnvironmentSnapshotReader,
    IGameEnvironmentSnapshotComparer gameEnvironmentSnapshotComparer
    )
{
    // ----- Private -----
    private readonly IEnumerable<IGameEnvironmentFactory> _factories = factories;
    private readonly IGameEnvironmentSnapshotWriter _gameEnvironmentSnapshotWriter = gameEnvironmentSnapshotWriter;
    private readonly IGameEnvironmentSnapshotReader _gameEnvironmentSnapshotReader = gameEnvironmentSnapshotReader;
    private readonly IGameEnvironmentSnapshotComparer _gameEnvironmentSnapshotComparer = gameEnvironmentSnapshotComparer;

    // ----- Public -----
    /// <summary>
    /// The current <see cref="IGameEnvironment"/>.
    /// </summary>
    public IGameEnvironment? CurrentEnvironment
    {
        get;
        private set
        {
            var previousEnvironment = field;
            field = value;
            OnEnvironmentUpdate?.Invoke(value, previousEnvironment);
        }
    }

    /// <summary>
    /// An event that is raised every time the current environment is updated.
    /// </summary>
    public event EnvironmentUpdateHandler? OnEnvironmentUpdate;
    public delegate void EnvironmentUpdateHandler(IGameEnvironment? newEnvironment, IGameEnvironment? previousEnvironment);
    
    /// <summary>
    /// The currently active snapshot of the environment.
    /// </summary>
    public EnvironmentSnapshot? CurrentSnapshot { get; private set; }

    /// <summary>
    /// Sets a new environment to the controller based on the available factories.
    /// </summary>
    /// <param name="executablePath">The path of the game's executable.</param>
    public void SetNewEnvironment(string executablePath)
    {
        // Try to get the right factory.
        foreach (var factory in _factories)
        {
            var env = factory.CreateEnvironment(executablePath);
            if (env == null) continue;
            
            // Set the new Current Environment
            CurrentEnvironment = env;
            
            // Delete the Temps' content (if it exists)
            var tempPath = this.SearchAbsolutePath(Constants.App_RootFolder,
                Constants.App_TemporaryFolder);
            if (Directory.Exists(tempPath))
                foreach (var dir in Directory.EnumerateDirectories(tempPath))
                    Directory.Delete(dir, true);
            return;
        }
        CurrentEnvironment = null; // No factory could handle this path.
    }

    /// <summary>
    /// Checks whether the current environment is valid or not.
    /// </summary>
    /// <returns><see langword="true"/> if the environment is valid; otherwise, <see langword="false"/>.</returns>
    /// <remarks>
    /// This method is only needed to check if the current environment is still valid in storage.
    /// If <see cref="SetNewEnvironment"/> was used previously, <c>CurrentEnvironment != null</c> is a better alternative.
    /// </remarks>
    public bool IsEnvironmentValid() => CurrentEnvironment != null && IsEnvironmentValid(CurrentEnvironment.ExecutablePath);

    /// <summary>
    /// Checks whether the environment is valid or not through executable path.
    /// </summary>
    /// <returns><see langword="true"/> if the environment is valid; otherwise, <see langword="false"/>.</returns>
    /// <remarks>
    /// This method is only needed to check if the current environment is still valid in storage.
    /// If <see cref="SetNewEnvironment"/> was used previously, <c>CurrentEnvironment != null</c> is a better alternative.
    /// </remarks>
    public bool IsEnvironmentValid(string executablePath)
    {
        // Try to find the right factory.
        IGameEnvironment? newEnvironment = null;
        foreach (var factory in _factories)
        {
            var env = factory.CreateEnvironment(executablePath);
            if (env == null) continue;
            newEnvironment = env;
        }
        return newEnvironment != null;
    }

    /// <summary>
    /// Updates the snapshot of the current environment.
    /// </summary>
    /// <returns>A <see cref="SnapshotChangeReport"/> with a detailed report of the snapshot changes.</returns>
    public async Task<SnapshotChangeReport> UpdateEnvironmentSnapshot()
    {
        // Get the path for the snapshot.
        var pathForSnapshot = this.SearchAbsolutePath(Constants.App_RootFolder, Constants.App_IndexFile);
        
        // Tries to load the snapshot from storage.
        var oldSnapshot = _gameEnvironmentSnapshotReader.ReadSnapshot(pathForSnapshot);
        if (oldSnapshot == null)
        {
            // No snapshot found, write a new one.
            // Writes a new snapshot and update in memory.
            await _gameEnvironmentSnapshotWriter.WriteSnapshotAsync(CurrentEnvironment!.RootPath, pathForSnapshot);
            CurrentSnapshot = _gameEnvironmentSnapshotReader.ReadSnapshot(pathForSnapshot);
            
            return new SnapshotChangeReport(false);
        }
        
        // Writes a new snapshot and update in memory.
        await _gameEnvironmentSnapshotWriter.WriteSnapshotAsync(CurrentEnvironment!.RootPath, pathForSnapshot);
        CurrentSnapshot = _gameEnvironmentSnapshotReader.ReadSnapshot(pathForSnapshot);

        var hasChanges = _gameEnvironmentSnapshotComparer.Compare(CurrentSnapshot!, oldSnapshot);
        return new SnapshotChangeReport(hasChanges, CurrentSnapshot!, oldSnapshot);
    }
}