using System;
using System.Threading.Tasks;
using GottaManagePlus.Models;
using GottaManagePlus.Utils;

namespace GottaManagePlus.Services.ProfileServices;

public sealed class ProfileManager(ProfileStorage storage)
{
    // ----- Private API -----
    private readonly ProfileStorage _storage = storage;
    // ----- Public API -----
    /// <summary>
    /// The currently active profile in the environment.
    /// </summary>
    public ProfileMetadata? ActiveProfile { get; private set; }

    /// <summary>
    /// An event that is triggered once the active profile changes.
    /// </summary>
    public event Action<ProfileMetadata?>? OnActiveProfileUpdate;

    /// <summary>
    /// Set a new profile from the database to be active in the environment.
    /// </summary>
    /// <param name="newProfile">The new profile to be active.</param>
    /// <param name="progress">The progress to be reported.</param>
    /// <returns><see langword="true"/> if the save/load operations were a success; otherwise, <see langword="false"/>.</returns>
    public async Task<bool> SetActiveProfile(ProfileMetadata newProfile, IProgress<ProgressReport>? progress)
    {
        // If there's an active profile, save it first.
        if (ActiveProfile != null)
            await _storage.SaveEnvironmentDataToProfile(ActiveProfile, progress);
        
        // Set the new profile as main and request storage to unpack it.
        ActiveProfile = newProfile;

        // Trigger the profile extraction.
        var success = await _storage.ExtractProfileToEnvironment(ActiveProfile, progress);
        
        // Invoke the event.
        OnActiveProfileUpdate?.Invoke(newProfile);
            
        return success;
    }
}