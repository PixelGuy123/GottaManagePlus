using System;
using System.IO;
using System.Threading.Tasks;
using GottaManagePlus.Interfaces.ProfileManagement;
using GottaManagePlus.Models;
using GottaManagePlus.Services.GameEnvironmentServices;
using GottaManagePlus.Utils;
using Serilog;

namespace GottaManagePlus.Services.ProfileServices.Management;

public sealed class LocalProfileDestructor(
    GameEnvironmentController controller,
    ProfileManager manager,
    ProfileRepository repository,
    ILogger logger) : IProfileDestructor
{
    // ----- Private API -----
    private readonly GameEnvironmentController _controller = controller;
    private readonly ProfileManager _manager = manager;
    private readonly ProfileRepository _repository = repository;
    private readonly ILogger _logger = logger;

    // ----- Public API -----
    /// <summary>
    /// Deletes a profile from the repository and physically.
    /// </summary>
    /// <param name="metadata">The metadata to be deleted.</param>
    /// <param name="progress">The progress to be reported.</param>
    public async Task DeleteProfile(ProfileMetadata metadata, IProgress<ProgressReport>? progress)
    {
        // The metadata must exist inside the repository and contain more than one metadata.
        if (_repository.Count <= 1 || !_repository.TryGet(metadata.Name, out _)) return;
        
        // Delete physically the profile from the profiles' folder.
        _logger.Information("Deleting \'{profile}\'...", metadata.Name);
        var metadataPath = metadata.GetPhysicalPath(_controller);
        Directory.Delete(metadataPath, recursive: true);
        
        // Then, delete the profile from the repository.
        _repository.Delete(metadata);
        _logger.Information("Profile deleted successfully.");
        
        // Afterward, reload the profiles.
        await _manager.UpdateProfileRepository(null, false, progress);
    }
}