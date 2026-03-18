using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GottaManagePlus.Models.UI;
using GottaManagePlus.Utils;

namespace GottaManagePlus.Services.ProfileServices;

public sealed class ProfileMemoryDb
{
    // ----- Private API ------
    private readonly List<ProfileItem> _profiles = new(8);
    
    // ------ Public API ------
    public event Action<ProfileMemoryDb>? OnProfilesUpdate;
    
    /// <summary>
    /// Returns a readonly collection of profiles inside the database.
    /// </summary>
    /// <returns>Returns an instance of <see cref="IReadOnlyList{ProfileItem}"/>.</returns>
    public IReadOnlyList<ProfileItem> GetProfiles() => _profiles;

    /// <summary>
    /// Clears out the list of profiles in-memory.
    /// </summary>
    public void ClearProfiles()
    {
        // Clear profiles list
        _profiles.Clear();
        
        // Invoke update
        OnProfilesUpdate?.Invoke(this);
    }

    /// <summary>
    /// Adds a profile to the database.
    /// </summary>
    /// <param name="profile">The profile to be included.</param>
    /// <returns><see langword="true"/> if the inclusion was successful; otherwise, <see langword="false"/>.</returns>
    public bool AddProfile(ProfileItem profile)
    {
        // Check if there's already a profile with same name
        if (_profiles.Exists(p =>
                p.FullOsPath?.Equals(profile.FullOsPath, StringComparison.OrdinalIgnoreCase) == true || 
                 p.ProfileName.Equals(profile.ProfileName, StringComparison.OrdinalIgnoreCase)))
            return false;
        
        // Register it
        _profiles.Add(profile);
        
        // Invoke update
        OnProfilesUpdate?.Invoke(this);
        return true;
    }
    
    /// <summary>
    /// Removes the profile from the database.
    /// </summary>
    /// <param name="profileIndex">The index to select and remove.</param>
    /// <exception cref="IndexOutOfRangeException">In case the index value is invalid to the database.</exception>
    public void DeleteProfile(int profileIndex)
    {
        if (!_profiles.IsIndexInBounds(profileIndex))
            throw new IndexOutOfRangeException($"profileIndex ({profileIndex}) is out of range.");

        // Invoke update
        OnProfilesUpdate?.Invoke(this);
        
        _profiles.RemoveAt(profileIndex);
    }
}