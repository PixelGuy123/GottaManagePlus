using System;
using System.Collections.Generic;
using GottaManagePlus.Models;
using GottaManagePlus.Utils;
using Serilog;

namespace GottaManagePlus.Services.ProfileServices;

public sealed class ProfileMemoryDb
{
    // ----- Private API ------
    private readonly List<ProfileMetadata> _profiles = new(8);
    
    // ------ Public API ------
    /// <summary>
    /// Raised when any action (clear, add, remove) is invoked.
    /// </summary>
    public event Action<ProfileMemoryDb>? OnProfilesUpdate;
    
    /// <summary>
    /// Returns a readonly collection of profiles inside the database.
    /// </summary>
    /// <returns>Returns an instance of <see cref="IReadOnlyList{ProfileItem}"/>.</returns>
    public IReadOnlyList<ProfileMetadata> GetProfiles() => _profiles;

    /// <summary>
    /// Whether the database is empty or not.
    /// </summary>
    public bool IsEmpty => _profiles.Count == 0;

    /// <summary>
    /// Clears out the list of profiles in-memory.
    /// </summary>
    public void ClearProfiles()
    {
        Log.Logger.Information("Cleared out profiles global list.");
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
    public bool AddProfile(ProfileMetadata profile)
    {
        // Check if there's already a profile with same name
        if (ProfileExists(profile))
            return false;
        
        // Register it
        Log.Logger.Information("Added profile \'{Profile}\' to the database.", profile.Name);
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
        
        Log.Logger.Information("Removed profile \'{Profile}\' from database.", _profiles[profileIndex].Name);
        _profiles.RemoveAt(profileIndex);
    }

    /// <summary>
    /// Tells whether a profile exists 
    /// </summary>
    /// <param name="metadata"></param>
    /// <returns></returns>
    public bool ProfileExists(ProfileMetadata metadata) =>
        _profiles.Exists(p =>
            p.Name.Equals(metadata.Name, StringComparison.OrdinalIgnoreCase));
}