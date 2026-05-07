using GottaManagePlus.Interfaces.ProfileManagement;
using GottaManagePlus.Models;
using GottaManagePlus.Services.GameEnvironmentServices;
using GottaManagePlus.Services.ProfileServices.Extractors;
using GottaManagePlus.Services.ProfileServices.Readers;
using GottaManagePlus.Services.ProfileServices.Writers;
using GottaManagePlus.Utils;
using Serilog;

namespace GottaManagePlus.Services.ProfileServices.Management;

public class ProfileExportController(
    ProfileExporter exporter,
    ProfileExportReader exportReader,
    ProfileExportExtractor exportExtractor,
    GameEnvironmentController controller,
    ProfileRepository repository,
    ILogger logger
    ) : IProfileExportController
{
    // ----- Private API -----
    private readonly ProfileExporter _exporter = exporter;
    private readonly ProfileExportReader _exportReader = exportReader;
    private readonly ProfileExportExtractor _exportExtractor = exportExtractor;
    private readonly GameEnvironmentController _controller = controller;
    private readonly ProfileRepository _repository = repository;
    private readonly ILogger _logger = logger;

    // ----- Public API -----
    /// <summary>
    /// Exports a <see cref="ProfileMetadata"/> instance in the format of <c>.gmpProfile</c>.
    /// </summary>
    /// <param name="profile">The profile to be compressed.</param>
    public void ExportProfile(ProfileMetadata profile)
    {
        _logger.Information("Exporting profile '{profile}'...", profile.Name);
        // Get the path to export the profile to.
        var exportFolder = _controller.GetOrCreateProfilesExportFolderPath();
        
        // Export profile to directory.
        _exporter.ExportProfileTo(exportFolder, profile, _controller);
    }

    /// <summary>
    /// Reads a <c>.gmpProfile</c> file and returns its metadata if it has one.
    /// </summary>
    /// <param name="path">The path to be scanned.</param>
    /// <returns><see cref="ProfileMetadata"/> object if a metadata is found on the archive; otherwise, <see langword="null"/>.</returns>
    public ProfileMetadata? ReadExportedProfile(string path)
    {
        _logger.Information("Reading exported profile '{profile}'...", Path.GetFileName(path));
        return _exportReader.ReadExportedProfile(path);
    }

    /// <summary>
    /// Extracts a <c>.gmpProfile</c> file to its respective location in the game's directory.
    /// </summary>
    /// <param name="path">The path to the <c>.gmpProfile</c> file.</param>
    public void ExtractExportedProfile(string path)
    {
        // Get the ProfileMetadata.
        _logger.Information("Extracting .gmpProfile from '{profilePath}'...", Path.GetFileName(path));
        var metadata = ReadExportedProfile(path);
        if (metadata == null) 
            throw new InvalidOperationException("Given .gmpProfile file has no metadata.");
        
        // Add it to the repository.
        if (!_repository.Add(metadata))
        {
            _logger.Warning("Failed to include '{profile}' in repository!", metadata.Name);
            return;
        }
        
        // Get the profiles' folder.
        var profilesFolder = _controller.GetOrCreateProfilesFolderPath();
        
        // Extract there.
        _exportExtractor.ExtractExportedProfile(
            _controller.SearchAbsolutePath(profilesFolder, metadata.Name), path);
    }
}