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
using GottaManagePlus.Services.ProfileServices.Extractors;
using GottaManagePlus.Utils;
using Serilog;

namespace GottaManagePlus.Services.ProfileServices.Management;

public sealed class ProfileToEnvironmentExtractor(
    GameEnvironmentController controller,
    ProfileZipExtractor zipExtractor,
    ILogger logger)
    : ILocalToEnvironmentParser
{
    // ---- Private -----
    private readonly GameEnvironmentController _controller = controller;
    private readonly ProfileZipExtractor _zipExtractor = zipExtractor;
    private readonly ILogger _logger = logger;

    // ---- Public -----
    /// <summary>
    /// Extracts a profile to the current environment.
    /// </summary>
    /// <param name="metadata">The metadata to be extracted.</param>
    /// <param name="progress">The progress reported.</param>
    /// <param name="cancellationToken">The cancellation token in case of a cancellation request.</param>
    /// <returns><see langword="true"/> if the operation was a success; otherwise, <see langword="false"/>.</returns>
    public async Task<bool> ExtractProfileToEnvironmentAsync(
        ProfileMetadata metadata,
        IProgress<ProgressReport>? progress,
        CancellationToken cancellationToken = default)
    {
        
        // Get the profile's physical path.
        var profilePath = metadata.GetPhysicalPath(_controller);
        _logger.Information("Extracting profile '{profile}' from path '{path}'...",
            metadata.Name,
            profilePath);
        
        // If it doesn't exist, cancel.
        if (!Directory.Exists(profilePath))
        {
            _logger.Warning("Profile does not exist.");
            return false;
        }

        // Attempt to extract profile.
        var success = await _zipExtractor.ExtractProfile(
            metadata,
            profilePath,
            _controller.CurrentEnvironment!.RootPath,
            _controller,
            progress,
            cancellationToken);
        
        if (success) _logger.Information("Successfully extracted profile!");
        else _logger.Information("Failed to extract profile!");
        
        return success;
    }
}