using GottaManagePlus.Interfaces.ProfileManagement;
using GottaManagePlus.Models;
using GottaManagePlus.Services.GameEnvironmentServices;
using GottaManagePlus.Services.ModServices;
using GottaManagePlus.Services.ProfileServices.Writers;
using GottaManagePlus.Utils;
using Serilog;

namespace GottaManagePlus.Services.ProfileServices.Management;

public sealed class EnvironmentToProfileSaver(
    GameEnvironmentController controller,
    ProfileZipWriter zipWriter,
    ModRepositoryScanner modRepositoryScanner,
    ILogger logger)
    : IEnvironmentToLocalParser
{
    // ---- Private API -----
    private readonly GameEnvironmentController _controller = controller;
    private readonly ProfileZipWriter _zipWriter = zipWriter;
    private readonly ModRepositoryScanner _modRepositoryScanner = modRepositoryScanner;
    private readonly ILogger _logger = logger;
    
    // ---- Public API -----
    /// <summary>
    /// Saves the environment to a given profile.
    /// </summary>
    /// <param name="metadata">The metadata to be updated.</param>
    /// <param name="progress">The progress report.</param>
    /// <param name="cancellationToken">The cancellation token in case this whole process is canceled.</param>
    public async Task SaveEnvironmentToProfileAsync(
        ProfileMetadata metadata,
        IProgress<ProgressReport>? progress,
        CancellationToken cancellationToken = default)
    {
        var pathToSave = _controller.GetOrCreateProfilesFolderPath();
        try
        {
            // Collect configuration and patcher files
            metadata.ConfigurationFiles.Clear();
            var configPath = _controller.SearchAbsolutePath(Constants.BepInExFolderName, Constants.ConfigFolder);
            if (Directory.Exists(configPath))
                foreach (var config in Directory.EnumerateFiles(configPath, "*.cfg", SearchOption.AllDirectories))
                    metadata.ConfigurationFiles.Add(_controller.SearchRelativePath(config));

            var patcherPath = _controller.SearchAbsolutePath(Constants.BepInExFolderName, Constants.PatchersFolder);
            if (Directory.Exists(patcherPath))
                foreach (var patcher in Directory.EnumerateFiles(patcherPath, "*.dll", SearchOption.AllDirectories))
                    metadata.PatcherFiles.Add(_controller.SearchRelativePath(patcher));
            
            // Add mods too through the scanner
            await _modRepositoryScanner.ScanRepository(metadata, progress, cancellationToken);

            // Writes the profile back.
            await _zipWriter.WriteProfileToAsync(pathToSave, metadata, _controller, progress, cancellationToken);
        }
        catch (Exception e)
        {
            _logger.Error(e, "Failed to save environment to profile {ProfileName}", metadata.Name);
            throw; // or handle as needed
        }
    }
}