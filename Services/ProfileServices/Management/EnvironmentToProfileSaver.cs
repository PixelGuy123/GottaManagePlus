using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using GottaManagePlus.Interfaces.ProfileManagement;
using GottaManagePlus.Models;
using GottaManagePlus.Services.GameEnvironmentServices;
using GottaManagePlus.Services.ProfileServices.Writers;
using GottaManagePlus.Utils;
using Serilog;

namespace GottaManagePlus.Services.ProfileServices.Management;

public sealed class EnvironmentToProfileSaver(
    GameEnvironmentController controller,
    ProfileZipWriter zipWriter,
    ILogger logger)
    : IEnvironmentToLocalParser
{
    // ---- Private API -----
    private readonly GameEnvironmentController _controller = controller;
    private readonly ProfileZipWriter _zipWriter = zipWriter;
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
        var pathToSave = metadata.GetPhysicalPath(_controller);
        try
        {
            var profileDir = new DirectoryInfo(pathToSave);
            if (!profileDir.Exists)
                profileDir.Create();

            // Collect configuration and patcher files
            metadata.ConfigurationFiles.Clear();
            var configPath = _controller.SearchRelativePath(Constants.BepInExFolderName, Constants.ConfigFolder);
            foreach (var config in Directory.EnumerateFiles(configPath, "*.cfg", SearchOption.AllDirectories))
                metadata.ConfigurationFiles.Add(config);

            var patcherPath = _controller.SearchRelativePath(Constants.BepInExFolderName, Constants.PatchersFolder);
            foreach (var patcher in Directory.EnumerateFiles(patcherPath, "*.dll", SearchOption.AllDirectories))
                metadata.PatcherFiles.Add(patcher);

            await _zipWriter.WriteProfileToAsync(pathToSave, metadata, _controller, progress, cancellationToken);
        }
        catch (Exception e)
        {
            _logger.Error(e, "Failed to save environment to profile {ProfileName}", metadata.Name);
            throw; // or handle as needed
        }
    }
}