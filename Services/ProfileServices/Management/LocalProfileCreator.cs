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
    // ---- Private ----
    private readonly ProfileRepository _repository = repository;
    private readonly ProfileZipWriter _zipWriter = zipWriter;
    private readonly GameEnvironmentController _controller = controller;
    private readonly ILogger _logger = logger;

    // ---- Public ----
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
    
    /// <summary>
    /// Creates a new profile by using the current environment's data.
    /// </summary>
    /// <param name="name">The name of the new profile.</param>
    /// <param name="progress">The progress report.</param>
    /// <returns>A new instance of <see cref="ProfileMetadata"/> if it is successfully written; otherwise <see langword="null"/>.</returns>
    public async Task<ProfileMetadata?> CreateProfileFromCurrentEnvironment(string name, IProgress<ProgressReport>? progress)
    {
        _logger.Information("Creating new profile '{Name}' from current environment", name);
    
        var profilesFolder = _controller.GetOrCreateProfilesFolderPath(_logger);
        var metadata = new ProfileMetadata { Name = name };
    
        // Add to repository first so that WriteProfileToAsync can find it if needed.
        if (!_repository.Add(metadata))
        {
            _logger.Warning("Failed to add profile '{Name}' to repository", name);
            return null;
        }
    
        // Write the current game state into the profile.
        await _zipWriter.WriteProfileToAsync(profilesFolder, metadata, _controller, progress);
    
        // Re-fetch from repository to get the fully populated metadata (mods, configs, etc.).
        return _repository.Get(name);
    }
}