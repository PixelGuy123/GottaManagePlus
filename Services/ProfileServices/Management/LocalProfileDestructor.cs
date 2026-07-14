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
using GottaManagePlus.Utils;
using Serilog;

namespace GottaManagePlus.Services.ProfileServices.Management;

public sealed class LocalProfileDestructor(
    GameEnvironmentController controller,
    ProfileManager manager,
    ProfileRepository repository,
    ILogger logger) : IProfileDestructor
{
    // ----- Private -----
    private readonly GameEnvironmentController _controller = controller;
    private readonly ProfileManager _manager = manager;
    private readonly ProfileRepository _repository = repository;
    private readonly ILogger _logger = logger;

    // ----- Public -----
    /// <summary>
    /// Deletes a profile from the repository and physically.
    /// </summary>
    /// <param name="metadata">The metadata to be deleted.</param>
    /// <param name="progress">The progress to be reported.</param>
    public async Task DeleteProfile(ProfileMetadata metadata, IProgress<ProgressReport>? progress)
    {
        // The metadata must exist inside the repository and contain more than one metadata.
        if (_repository.Count <= 1 || !_repository.TryGet(metadata.Name, out _)) return;
        
        // Delete physically the profile from the profiles' folder.
        _logger.Information("Deleting '{profile}'...", metadata.Name);
        var metadataPath = metadata.GetPhysicalPath(_controller);
        Directory.Delete(metadataPath, recursive: true);
        
        // Then, delete the profile from the repository.
        _repository.Delete(metadata);
        _logger.Information("Profile deleted successfully.");
        
        // Afterward, reload the profiles.
        await _manager.UpdateProfileRepository(null, false, progress);
    }
}