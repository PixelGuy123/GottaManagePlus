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
    ILogger logger)
    : IProfileStorageScanner
{
    // ---- Private -----
    private readonly GameEnvironmentController _controller = controller;
    private readonly ProfileZipReader _zipReader = zipReader;
    private readonly ProfileRepository _profileRepository = repository;
    private readonly ILogger _logger = logger;

    /// <summary>
    /// Scans the profiles folder and reloads the repository.
    /// </summary>
    public void ScanAndLoadProfiles()
    {
        _logger.Information("Re-scanning local storage...");
        // Clear the repository beforehand.
        _profileRepository.Clear();

        // Get the folder to scan.
        var profilesFolder = _controller.GetOrCreateProfilesFolderPath();

        // Search every directory, read the metadata and register to the repo.
        foreach (var profileDir in Directory.EnumerateDirectories(profilesFolder))
        {
            _logger.Information("Found profile '{profileDir}'", profileDir);
            var metadata = _zipReader.ReadProfile(profileDir);
            if (metadata == null)
            {
                _logger.Warning("Could not read metadata from profile.");
                continue;
            }
            _profileRepository.Add(metadata);
        }
    }
}