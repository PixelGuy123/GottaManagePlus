using System;
using GottaManagePlus.Models.UI;
using GottaManagePlus.Utils;

namespace GottaManagePlus.Services.ProfileServices;

public sealed class ProfileManager(ProfileMemoryDb profileMemoryDb, ProfileStorage profileStorage)
{
    // ----- Private API -----
    private readonly ProfileMemoryDb _memoryDb = profileMemoryDb;
    private readonly ProfileStorage _storage = profileStorage;
    
    // ----- Public API -----
    /// <summary>
    /// The currently active profile in the environment.
    /// </summary>
    public ProfileItem? ActiveProfile { get; private set; }

    /// <summary>
    /// Set a new profile from the database to be active in the environment.
    /// </summary>
    /// <param name="profileIndex">The index of the new profile to select.</param>
    /// <exception cref="IndexOutOfRangeException">If the index given is out of bounds, this is thrown.</exception>
    public void SetActiveProfile(int profileIndex)
    {
        if (!_memoryDb.GetProfiles().IsIndexInBounds(profileIndex))
            throw new IndexOutOfRangeException($"{nameof(profileIndex)} ({profileIndex}) is out of bounds.");
        if (ActiveProfile != null)
            ActiveProfile.IsSelectedProfile = false;
        
        ActiveProfile = _memoryDb.GetProfiles()[profileIndex];
        ActiveProfile.IsSelectedProfile = true;
    }

    /// <summary>
    /// When triggered, a proper refresh is done through <see cref="ProfileStorage"/> and <see cref="ProfileMemoryDb"/> to scan the folder with the current context.
    /// </summary>
    public void RefreshProfileStorage()
    {
        
    }
}