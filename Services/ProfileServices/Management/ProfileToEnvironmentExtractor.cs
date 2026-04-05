using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using GottaManagePlus.Interfaces.ProfileManagement;
using GottaManagePlus.Models;
using GottaManagePlus.Services.GameEnvironmentServices;
using GottaManagePlus.Services.ProfileServices.Extractors;
using GottaManagePlus.Utils;
using Serilog;

namespace GottaManagePlus.Services.ProfileServices.Management;

public sealed class ProfileToEnvironmentExtractor(
    GameEnvironmentController controller,
    ProfileZipExtractor zipExtractor,
    ILogger logger)
    : ILocalToEnvironmentParser
{
    // ---- Private API -----
    private readonly GameEnvironmentController _controller = controller;
    private readonly ProfileZipExtractor _zipExtractor = zipExtractor;
    private readonly ILogger _logger = logger;

    // ---- Public API -----
    /// <summary>
    /// Extracts a profile to the current environment.
    /// </summary>
    /// <param name="metadata">The metadata to be extracted.</param>
    /// <param name="progress">The progress reported.</param>
    /// <param name="cancellationToken">The cancellation token in case of a cancellation request.</param>
    /// <returns><see langword="true"/> if the operation was a success; otherwise, <see langword="false"/>.</returns>
    public async Task<bool> ExtractProfileToEnvironmentAsync(
        ProfileMetadata metadata,
        IProgress<ProgressReport>? progress,
        CancellationToken cancellationToken = default)
    {
        _logger.Information("Initiated profile extraction...");
        var profilePath = metadata.GetPhysicalPath(_controller);
        var profileDir = new DirectoryInfo(profilePath);
        if (!profileDir.Exists)
            return false;
        var success = await _zipExtractor.ExtractProfile(
            metadata,
            profileDir.FullName,
            _controller.CurrentEnvironment!.RootPath,
            _controller,
            progress,
            cancellationToken);
        
        if (success) _logger.Information("Successfully extracted profile!");
        else _logger.Information("Failed to extract profile!");
        
        return success;
    }
}