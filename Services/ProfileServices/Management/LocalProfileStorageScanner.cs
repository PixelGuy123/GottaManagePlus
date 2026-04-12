using System.IO;
using GottaManagePlus.Interfaces.ProfileManagement;
using GottaManagePlus.Services.GameEnvironmentServices;
using GottaManagePlus.Services.ProfileServices.Readers;
using GottaManagePlus.Utils;
using Serilog;

namespace GottaManagePlus.Services.ProfileServices.Management;

public sealed class LocalProfileStorageScanner(
    GameEnvironmentController controller,
    ProfileZipReader zipReader,
    ProfileRepository repository,
    IProfileCreator creator,
    ILogger logger)
    : IProfileStorageScanner
{
    // ---- Private API -----
    private readonly GameEnvironmentController _controller = controller;
    private readonly ProfileZipReader _zipReader = zipReader;
    private readonly ProfileRepository _profileRepository = repository;
    private readonly IProfileCreator _creator = creator;
    private readonly ILogger _logger = logger;

    /// <summary>
    /// Scans the profiles folder and reloads the repository.
    /// </summary>
    public void ScanAndLoadProfiles()
    {
        // Clear the repository beforehand.
        _profileRepository.Clear();

        // Get the folder to scan.
        var profilesFolder = _controller.GetOrCreateProfilesFolderPath();

        // Search every directory, read the metadata and register to the repo.
        foreach (var profileDir in Directory.EnumerateDirectories(profilesFolder))
        {
            var metadata = _zipReader.ReadProfile(profileDir);
            if (metadata == null)
            {
                _logger.Warning("Could not read metadata from {ProfileDir}", profileDir);
                continue;
            }
            _profileRepository.Add(metadata);
        }
    }
}