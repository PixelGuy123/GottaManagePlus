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
using GottaManagePlus.Services.GameEnvironmentServices;
using GottaManagePlus.Services.ProfileServices.Readers;
using GottaManagePlus.Utils;
using Serilog;

namespace GottaManagePlus.Services.ProfileServices.Management;

public sealed class LocalProfileStorageScanner(
    GameEnvironmentController controller,
    ProfileZipReader zipReader,
    ProfileRepository repository,
    ILogger logger)
    : IProfileStorageScanner
{
    // ---- Private -----
    private readonly GameEnvironmentController _controller = controller;
    private readonly ProfileZipReader _zipReader = zipReader;
    private readonly ProfileRepository _profileRepository = repository;
    private readonly ILogger _logger = logger;

    /// <summary>
    /// Scans the profiles folder and reloads the repository.
    /// </summary>
    public void ScanAndLoadProfiles()
    {
        _logger.Information("Re-scanning local storage...");
        // Clear the repository beforehand.
        _profileRepository.Clear();

        // Get the folder to scan.
        var profilesFolder = _controller.GetOrCreateProfilesFolderPath();

        // Search every directory, read the metadata and register to the repo.
        foreach (var profileDir in Directory.EnumerateDirectories(profilesFolder))
        {
            _logger.Information("Found profile '{profileDir}'", profileDir);
            var metadata = _zipReader.ReadProfile(profileDir);
            if (metadata == null)
            {
                _logger.Warning("Could not read metadata from profile.");
                continue;
            }
            _profileRepository.Add(metadata);
        }
    }
}