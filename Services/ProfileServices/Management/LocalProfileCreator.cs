using System.Threading.Tasks;
using GottaManagePlus.Interfaces.ProfileManagement;
using GottaManagePlus.Models;
using GottaManagePlus.Services.GameEnvironmentServices;
using GottaManagePlus.Services.ProfileServices.Writers;
using GottaManagePlus.Utils;
using Serilog;

namespace GottaManagePlus.Services.ProfileServices.Management;

public sealed class LocalProfileCreator(
    ProfileRepository repository,
    ProfileZipWriter zipWriter,
    GameEnvironmentController controller,
    ILogger logger
    ) : IProfileCreator
{
    // ---- Private API ----
    private readonly ProfileRepository _repository = repository;
    private readonly ProfileZipWriter _zipWriter = zipWriter;
    private readonly GameEnvironmentController _controller = controller;
    private readonly ILogger _logger = logger;

    // ---- Public API ----
    /// <summary>
    /// Creates a new empty profile to the local storage.
    /// </summary>
    /// <param name="basicMetadataReference">
    /// The basic <see cref="ProfileMetadata"/> instance to represent
    /// non-storage data.
    /// </param>
    public async Task<ProfileMetadata?> CreateProfile(ProfileMetadata basicMetadataReference)
    {
        _logger.Information("Creating new empty Profile '{Name}'", basicMetadataReference.Name);
        // Get the directory, if it exists.
        var profilesFolder = _controller.GetOrCreateProfilesFolderPath(_logger);
        
        // Create a new empty profile.
        _logger.Information("Writing profile to disk...");
        var clearedMetadata = await _zipWriter.WriteEmptyProfileToAsync(profilesFolder, basicMetadataReference, _controller);

        // If it's null, return null too.
        if (clearedMetadata == null) 
            return null;

        // Add that profile to the database.
        if (_repository.Add(clearedMetadata))
            return clearedMetadata;

        _logger.Information("ProfileMetadata already exists. Returning copy from repository...");
        // Retrieves copy inside the repository.
        return _repository.Get(clearedMetadata.Name);
    }
}