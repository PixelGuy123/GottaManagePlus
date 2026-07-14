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
using Serilog;

namespace GottaManagePlus.Services.ProfileServices;

public sealed class ProfileManager(
    IEnvironmentToLocalParser environmentSaver,
    ILocalToEnvironmentParser profileExtractor,
    IProfileStorageScanner profileScanner,
    IProfileCreator creator,
    ProfileRepository repository,
    ILogger logger)
{
    // ---- Private -----
    private readonly IEnvironmentToLocalParser _environmentSaver = environmentSaver;
    private readonly ILocalToEnvironmentParser _profileExtractor = profileExtractor;
    private readonly IProfileStorageScanner _profileScanner = profileScanner;
    private readonly IProfileCreator _creator = creator;
    private readonly ProfileRepository _repository = repository;
    private readonly ILogger _logger = logger;

    // ---- Public ----
    /// <summary>
    /// The currently active <see cref="ProfileMetadata"/>.
    /// </summary>
    public ProfileMetadata? ActiveProfile { get; private set; }

    /// <summary>
    /// Invoked every time the <see cref="ProfileManager.ActiveProfile"/> is changed.
    /// </summary>
    public event ProfileUpdateListener? OnActiveProfileUpdate;
    public delegate void ProfileUpdateListener(ProfileMetadata? newActiveProfile);

    /// <summary>
    /// Sets a new profile to the <see cref="ProfileManager"/>.
    /// </summary>
    /// <param name="newProfile">The new profile instance.</param>
    /// <param name="progress">The progress to be reported.</param>
    /// <returns><see langword="true"/> if the exchanging is a success; otherwise, <see langword="false"/>.</returns>
    public async Task<bool> SetActiveProfile(ProfileMetadata newProfile, IProgress<ProgressReport>? progress)
    {
        _logger.Information("Switching from profile '{oldProfile}' to '{newProfile}'...", ActiveProfile?.Name, newProfile.Name);
        // Saves the current profile.
        await SaveActiveProfile(progress);

        // Tries to extract new profile.
        var success = await _profileExtractor.ExtractProfileToEnvironmentAsync(newProfile, progress);
        if (!success)
        {
            _logger.Warning("Profile switch failed!");
            return false;
        }

        // Set new profile as active.
        ActiveProfile = newProfile;
        OnActiveProfileUpdate?.Invoke(newProfile);
        
        // Important Post-Update.
        await _environmentSaver.SaveEnvironmentToProfileAsync(newProfile, progress);
        
        _logger.Information("Profile switch done successfully!");
        return true;
    }

    /// <summary>
    /// Saves the current environment to the active profile.
    /// </summary>
    /// <param name="progress">The progress to be reported.</param>
    public async Task SaveActiveProfile(IProgress<ProgressReport>? progress)
    {
        if (ActiveProfile == null || !_repository.TryGet(ActiveProfile.Name, out _)) return;
        await _environmentSaver.SaveEnvironmentToProfileAsync(ActiveProfile, progress);
    }

    /// <summary>
    /// Updates the repository of profiles by checking the local storage.
    /// </summary>
    /// <param name="preferredProfile">The preferred profile to be chosen after the process if possible.</param>
    /// <param name="updateProfileDataBeforeSwitch">If <see langword="true"/>, the switched profile will be updated before being loaded-in</param>
    /// <param name="progress">The progress to be reported.</param>
    public async Task<bool> UpdateProfileRepository(string? preferredProfile, bool updateProfileDataBeforeSwitch, IProgress<ProgressReport>? progress)
    {
        // Progress report.
        progress?.Report(new ProgressReport("Updating Profiles", "Scanning local storage..."));
        
        // Make a copy of the profiles in case of error.
        var safeProfileListCopy = new List<ProfileMetadata>(_repository.GetAll());

        try
        {
            // Reloads the repository.
            _profileScanner.ScanAndLoadProfiles();

            // If the repository is empty, make a default profile.
            if (_repository.IsEmpty)
            {
                ActiveProfile = null;
                var profile = await _creator.CreateProfileFromCurrentEnvironment(ProfileMetadata.DefaultName, progress);
                if (profile != null) // Reminder that SetActiveProfile automatically saves the profile too.
                    await SetActiveProfile(profile, progress);
                else
                    throw new InvalidOperationException("No profile has been selected!");
            }
            
            // If the profile is already set, search for it.
            if (!string.IsNullOrEmpty(preferredProfile) &&
                _repository.TryGet(preferredProfile, out var profileMetadata))
            {
                // If this is true, the profile receives the new environment before loading in.
                if (updateProfileDataBeforeSwitch)
                {
                    _logger.Information("Updating profile's data before switch...");
                    await _environmentSaver.SaveEnvironmentToProfileAsync(profileMetadata, progress);
                }

                // Switch to that profile.
                await SetActiveProfile(profileMetadata, progress);
                return true;
            }

            // Get the current profile.
            var currentProfile = _repository.GetAll()[0];
            
            // If this is true, the profile receives the new environment before loading in.
            if (updateProfileDataBeforeSwitch)
            {
                _logger.Information("Updating profile's data before switch...");
                await _environmentSaver.SaveEnvironmentToProfileAsync(currentProfile, progress);
            }

            // If the repository isn't empty, pick the first profile available.
            await SetActiveProfile(currentProfile, progress);
            return true;
        }
        catch (Exception e)
        {
            _logger.Error(e, "Error while updating profile list.");
            
            // Roll back on the repository's profiles.
            _repository.Clear();
            safeProfileListCopy.ForEach(p => _repository.Add(p));
            return false;
        }
    }
}