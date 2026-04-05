using System;
using System.Threading.Tasks;
using GottaManagePlus.Interfaces.ProfileManagement;
using GottaManagePlus.Models;
using Serilog;

namespace GottaManagePlus.Services.ProfileServices;

public sealed class ProfileManager(
    IEnvironmentToLocalParser environmentSaver,
    ILocalToEnvironmentParser profileExtractor,
    ProfileRepository repository,
    ILogger logger)
{
    // ---- Private API -----
    private readonly IEnvironmentToLocalParser _environmentSaver = environmentSaver;
    private readonly ILocalToEnvironmentParser _profileExtractor = profileExtractor;
    private readonly ProfileRepository _repository = repository;
    private readonly ILogger _logger = logger;

    // ---- Public API ----
    /// <summary>
    /// The currently active <see cref="ProfileMetadata"/>.
    /// </summary>
    public ProfileMetadata? ActiveProfile { get; private set; }

    /// <summary>
    /// Invoked every time the <see cref="ProfileManager.ActiveProfile"/> is changed.
    /// </summary>
    public event Action<ProfileMetadata?>? OnActiveProfileUpdate;

    /// <summary>
    /// Sets a new profile to the <see cref="ProfileManager"/>.
    /// </summary>
    /// <param name="newProfile">The new profile instance.</param>
    /// <param name="progress">The progress to be reported.</param>
    /// <returns><see langword="true"/> if the exchanging is a success; otherwise, <see langword="false"/>.</returns>
    public async Task<bool> SetActiveProfile(ProfileMetadata newProfile, IProgress<ProgressReport>? progress)
    {
        _logger.Information("Switching from profile \'{oldProfile}\' to \'{newProfile}\'...", ActiveProfile?.Name, newProfile.Name);
        // Saves the current profile.
        await SaveActiveProfile(progress);

        // Tries to extract new profile.
        var success = await _profileExtractor.ExtractProfileToEnvironmentAsync(newProfile, progress);
        if (!success)
        {
            _logger.Warning("Profile switch failed!");
            return false;
        }

        ActiveProfile = newProfile;
        OnActiveProfileUpdate?.Invoke(newProfile);
        _logger.Information("Profile switch done successfully!");
        return true;
    }

    /// <summary>
    /// Saves the current environment to the active profile.
    /// </summary>
    /// <param name="progress">The progress to be reported.</param>
    public async Task SaveActiveProfile(IProgress<ProgressReport>? progress)
    {
        if (ActiveProfile == null || !_repository.Exists(ActiveProfile)) return;
        await _environmentSaver.SaveEnvironmentToProfileAsync(ActiveProfile, progress);
    }


}