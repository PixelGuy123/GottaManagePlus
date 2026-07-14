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

using System.Diagnostics.CodeAnalysis;
using GottaManagePlus.Models;
using Serilog;

namespace GottaManagePlus.Services.ProfileServices;

/// <summary>
/// A singleton service responsible for holding in-memory every profile available.
/// </summary>
public sealed class ProfileRepository(ILogger logger)
{
    // ----- Private ------
    private readonly List<ProfileMetadata> _profiles = new(8);
    private readonly ILogger _logger = logger;
    
    // ------ Public ------
    /// <summary>
    /// Raised when any action (clear, add, remove) is invoked.
    /// </summary>
    public event Action<ProfileRepository>? OnProfilesUpdate;

    /// <summary>
    /// Returns a readonly collection of profiles inside the repository.
    /// </summary>
    /// <returns>Returns an instance of <see cref="IReadOnlyList{ProfileItem}"/>.</returns>
    public IReadOnlyList<ProfileMetadata> GetAll() => _profiles;

    /// <summary>
    /// Whether the repository is empty or not.
    /// </summary>
    public bool IsEmpty => _profiles.Count == 0;
    /// <summary>
    /// The amount of profiles inside the repository.
    /// </summary>
    public int Count => _profiles.Count;

    /// <summary>
    /// Clears out the list of profiles in-memory.
    /// </summary>
    public void Clear()
    {
        _logger.Information("Cleared out profiles global list.");
        // Clear profiles list
        _profiles.Clear();
        
        // Invoke update
        OnProfilesUpdate?.Invoke(this);
    }

    /// <summary>
    /// Adds a profile to the repository.
    /// </summary>
    /// <param name="profile">The profile to be included.</param>
    /// <returns><see langword="true"/> if the inclusion was successful; otherwise, <see langword="false"/>.</returns>
    public bool Add(ProfileMetadata profile)
    {
        // Check if there's already a profile with same name
        if (TryGet(profile.Name, out _))
            return false;
        
        // Register it
        _logger.Information("Added profile '{Profile}' to the repository.", profile.Name);
        _profiles.Add(profile);
        
        // Invoke update
        OnProfilesUpdate?.Invoke(this);
        return true;
    }
    
    /// <summary>
    /// Removes the profile from the repository.
    /// </summary>
    /// <param name="profile">The profile to be removed.</param>
    public void Delete(ProfileMetadata profile)
    {
        // Try to locate the profile.
        var index = _profiles.IndexOf(profile);
        if (index == -1)
        {
            _logger.Warning("Attempted to remove unexistent profile from repository ('{profile}')",profile.Name);
            return;
        }
        
        // Remove the profile
        var profileName = _profiles[index].Name;
        _profiles.RemoveAt(index);
        
        _logger.Information("Removed profile '{Profile}' from repository.", profileName);
        
        // Invoke update
        OnProfilesUpdate?.Invoke(this);
    }

    /// <summary>
    /// Searches for a profile based on name and returns it as an argument.
    /// </summary>
    /// <param name="profileName">The name of the profile.</param>
    /// <param name="profileMetadata">The <see cref="ProfileMetadata"/> instance retrieved.</param>
    /// <returns><see langword="true"/> if the profile is found; otherwise, <see langword="false"/>.</returns>
    public bool TryGet(string profileName, [NotNullWhen(true)] out ProfileMetadata? profileMetadata)
    {
        profileMetadata = _profiles.Find(p =>
            p.Name.Equals(profileName, StringComparison.OrdinalIgnoreCase));
        return profileMetadata != null;
    }

    /// <summary>
    /// Searches for a profile based on name and return it back.
    /// </summary>
    /// <param name="profileName">The name of the profile.</param>
    /// <returns>The <see cref="ProfileMetadata"/> inside the repository, if found.</returns>
    /// <exception cref="ArgumentOutOfRangeException">If the profile is not found, this exception is raised.</exception>
    public ProfileMetadata Get(string profileName)
    {
        var profileMetadata = _profiles.Find(p =>
            p.Name.Equals(profileName, StringComparison.OrdinalIgnoreCase));
        return profileMetadata ?? throw new ArgumentOutOfRangeException(profileName, "Profile not found.");
    }
}