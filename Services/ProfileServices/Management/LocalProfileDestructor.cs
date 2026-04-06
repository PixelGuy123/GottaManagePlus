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
    IProfileCreator creator,
    ILogger logger) : IProfileDestructor
{
    // ----- Private API -----
    private readonly GameEnvironmentController _controller = controller;
    private readonly ProfileManager _manager = manager;
    private readonly ProfileRepository _repository = repository;
    private readonly IProfileCreator _creator = creator;
    private readonly ILogger _logger = logger;

    // ----- Public API -----
    /// <summary>
    /// Deletes a profile from the repository and physically.
    /// </summary>
    /// <param name="metadata">The metadata to be deleted.</param>
    /// <param name="progress">The progress to be reported.</param>
    public async Task DeleteProfile(ProfileMetadata metadata, IProgress<ProgressReport>? progress)
    {
        // The metadata must exist inside the repository.
        if (!_repository.Exists(metadata)) return;
        
        // Delete physically the profile from the profiles' folder.
        _logger.Information("Deleting \'{profile}\'...", metadata.Name);
        var metadataPath = metadata.GetPhysicalPath(_controller);
        Directory.Delete(metadataPath, recursive: true);
        
        // Then, delete the profile from the repository.
        _repository.Delete(metadata);
        _logger.Information("Profile deleted successfully.");
        
        // Afterward, if the repository becomes empty, create a default profile.
        if (_repository.IsEmpty)
        {
            var profile = await _creator.CreateProfile(ProfileMetadata.Default);
            await _manager.SetActiveProfile(profile!, progress);
        }
        else // Otherwise, follow up with a quick swap.
            await _manager.SetActiveProfile(_repository.GetAll()[0], progress);
        
    }
}