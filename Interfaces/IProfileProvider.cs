using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using GottaManagePlus.Models;

namespace GottaManagePlus.Interfaces;

/// <summary>
/// Defines the contract for a service that manages user profiles within the mod manager.
/// </summary>
public interface IProfileProvider
{
    /// <summary>
    /// The implementation should attempt to add a new profile item to the managed collection.
    /// </summary>
    /// <param name="profileName">The profile name to be included.</param>
    /// <param name="deleteExistingStorage">If <see langword="true"/>, the implementation should delete the storage before generating the profile.</param>
    /// <param name="progress">Reports back the process (both percentage and status message) if needed.</param>
    /// <returns><see langword="true"/> if the profile was successfully added; otherwise, <see langword="false"/>.</returns>
    public Task<bool> AddProfile(string profileName, bool deleteExistingStorage, IProgress<(int, int, string?)>? progress = null);
    
    /// <summary>
    /// The implementation should attempt to add a new profile item to the managed collection.
    /// </summary>
    /// <param name="newProfileName">The profile name of the clone.</param>
    /// <param name="profileToCloneIndex">The profile to be cloned.</param>
    /// <returns><see langword="true"/> if the profile was successfully cloned; otherwise, <see langword="false"/>.</returns>
    public Task<bool> CloneProfile(string newProfileName, int profileToCloneIndex);

    /// <summary>
    /// The implementation should remove a profile at the specified index and delete its associated storage resources.
    /// </summary>
    /// <param name="profileIndex">The zero-based index of the profile to delete.</param>
    /// <param name="progress">Reports back the process (both percentage and status message) if needed.</param>
    /// <returns><see langword="true"/> if the deletion was successful; otherwise, <see langword="false"/>.</returns>
    /// <exception cref="IndexOutOfRangeException">Thrown when the index is outside the bounds of the profile list.</exception>
    public Task<bool> DeleteProfile(int profileIndex, IProgress<(int, int, string?)>? progress = null);
    
    /// <summary>
    /// The implementation should export a special format for a profile at the specified index.
    /// </summary>
    /// <param name="profileIndex">The zero-based index of the profile to export.</param>
    /// <returns><see langword="true"/> if the exportation was successful; otherwise, <see langword="false"/>.</returns>
    public Task<bool> ExportProfile(int profileIndex);
    
    /// <summary>
    /// The implementation should import a special format for a profile.
    /// </summary>
    /// <param name="importPath">The path to grab the file to be imported.</param>
    /// <returns><see langword="true"/> if the exportation was successful; otherwise, <see langword="false"/>.</returns>
    public Task<bool> ImportProfile(string importPath);

    /// <summary>
    /// The implementation should provide a read-only collection of all currently loaded profiles.
    /// </summary>
    /// <returns>A collection of <see cref="ProfileItem"/> objects.</returns>
    public IReadOnlyList<ProfileItem> GetLoadedProfiles();

    /// <summary>
    /// The implementation should refresh the profile list by scanning the underlying storage.
    /// </summary>
    /// <param name="defaultSelection">The profile to be selected by default.</param>
    /// <param name="progress">Reports back the process (both percentage and status message) if needed.</param>
    /// /// <returns><see langword="true"/> if the operation was a success; otherwise, <see langword="false"/>.</returns>
    public Task<bool> UpdateProfilesData(string defaultSelection = "", IProgress<(int, int, string?)>? progress = null);

    /// <summary>
    /// The implementation should return the profile currently marked as active.
    /// </summary>
    /// <returns>The index of the active <see cref="ProfileItem"/>.</returns>
    /// <exception cref="NullReferenceException">Thrown if no profile is currently active.</exception>
    public int GetActiveProfile();

    /// <summary>
    /// The implementation should update the current active profile to the one at the specified index, while also move the profile content into the game folder.
    /// </summary>
    /// <param name="profileIndex">The zero-based index of the profile to activate.</param>
    /// <param name="progress">Reports back the process (both percentage and status message) if needed.</param>
    public Task<bool> SetActiveProfile(int profileIndex, IProgress<(int, int, string?)>? progress = null);
    
    /// <summary>
    /// The implementation should forcefully generate a new content file for the profile.
    /// </summary>
    /// <param name="progress">Reports back the process (both percentage and status message) if needed.</param>
    public Task<bool> SaveActiveProfile(IProgress<(int, int, string?)>? progress = null);

    /// <summary>
    /// Occurs when the profiles collection is updated or the active profile changes.
    /// </summary>
    public event Action<IProfileProvider>? OnProfilesUpdate;
}