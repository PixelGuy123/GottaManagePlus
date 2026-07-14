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

using GottaManagePlus.Interfaces.ProfileManagement;
using GottaManagePlus.Models;
using GottaManagePlus.Services.GameEnvironmentServices;
using GottaManagePlus.Services.ModServices;
using GottaManagePlus.Services.ProfileServices.Writers;
using GottaManagePlus.Utils;
using Serilog;

namespace GottaManagePlus.Services.ProfileServices.Management;

public sealed class EnvironmentToProfileSaver(
    GameEnvironmentController controller,
    ProfileZipWriter zipWriter,
    ModRepositoryScanner modRepositoryScanner,
    ILogger logger)
    : IEnvironmentToLocalParser
{
    // ---- Private -----
    private readonly GameEnvironmentController _controller = controller;
    private readonly ProfileZipWriter _zipWriter = zipWriter;
    private readonly ModRepositoryScanner _modRepositoryScanner = modRepositoryScanner;
    private readonly ILogger _logger = logger;
    
    // ---- Public -----
    /// <summary>
    /// Saves the environment to a given profile.
    /// </summary>
    /// <param name="metadata">The metadata to be updated.</param>
    /// <param name="progress">The progress report.</param>
    /// <param name="cancellationToken">The cancellation token in case this whole process is canceled.</param>
    public async Task SaveEnvironmentToProfileAsync(
        ProfileMetadata metadata,
        IProgress<ProgressReport>? progress,
        CancellationToken cancellationToken = default)
    {
        var pathToSave = _controller.GetOrCreateProfilesFolderPath();
        try
        {
            // Collect configuration and patcher files
            metadata.ConfigurationFiles.Clear();
            var configPath = _controller.SearchAbsolutePath(Constants.BepInExFolderName, Constants.ConfigFolder);
            if (Directory.Exists(configPath))
                foreach (var config in Directory.EnumerateFiles(configPath, "*.cfg", SearchOption.AllDirectories))
                    metadata.ConfigurationFiles.Add(_controller.SearchRelativePath(config));

            var patcherPath = _controller.SearchAbsolutePath(Constants.BepInExFolderName, Constants.PatchersFolder);
            if (Directory.Exists(patcherPath))
                foreach (var patcher in Directory.EnumerateFiles(patcherPath, "*.dll", SearchOption.AllDirectories))
                    metadata.PatcherFiles.Add(_controller.SearchRelativePath(patcher));
            
            // Add mods too through the scanner
            await _modRepositoryScanner.ScanRepository(metadata, progress, cancellationToken);

            // Writes the profile back.
            await _zipWriter.WriteProfileToAsync(pathToSave, metadata, _controller, progress, cancellationToken);
        }
        catch (Exception e)
        {
            _logger.Error(e, "Failed to save environment to profile {ProfileName}", metadata.Name);
            throw; // or handle as needed
        }
    }
}