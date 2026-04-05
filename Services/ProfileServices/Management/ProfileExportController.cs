using System.Threading.Tasks;
using GottaManagePlus.Interfaces.ProfileManagement;
using GottaManagePlus.Models;
using GottaManagePlus.Services.GameEnvironmentServices;
using GottaManagePlus.Services.ProfileServices.Extractors;
using GottaManagePlus.Services.ProfileServices.Readers;
using GottaManagePlus.Services.ProfileServices.Writers;
using GottaManagePlus.Utils;

namespace GottaManagePlus.Services.ProfileServices.Management;

public class ProfileExportController(
    ProfileExporter exporter,
    ProfileExportReader exportReader,
    ProfileExportExtractor exportExtractor,
    GameEnvironmentController controller
    ) : IProfileExportController
{
    // ----- Private API -----
    private readonly ProfileExporter _exporter = exporter;
    private readonly ProfileExportReader _exportReader = exportReader;
    private readonly ProfileExportExtractor _exportExtractor = exportExtractor;
    private readonly GameEnvironmentController _controller = controller;

    // ----- Public API -----
    /// <summary>
    /// Exports a <see cref="ProfileMetadata"/> instance in the format of <c>.gmpProfile</c>.
    /// </summary>
    /// <param name="profile">The profile to be compressed.</param>
    public void ExportProfile(ProfileMetadata profile)
    {
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
    public ProfileMetadata? ReadExportedProfile(string path) =>
        _exportReader.ReadExportedProfile(path);
    
    /// <summary>
    /// Extracts a <c>.gmpProfile</c> file to its respective location in the game's directory.
    /// </summary>
    /// <param name="path">The path to the <c>.gmpProfile</c> file.</param>
    public void ExtractExportedProfile(string path)
    {
        // Get the profiles' folder.
        var profilesFolder = _controller.GetOrCreateProfilesFolderPath();
        
        // Extract there.
        _exportExtractor.ExtractExportedProfile(profilesFolder, path);
    }
}