using System.Threading.Tasks;
using GottaManagePlus.Interfaces.ProfileManagement;
using GottaManagePlus.Models;
using GottaManagePlus.Services.GameEnvironmentServices;
using GottaManagePlus.Services.ProfileServices.Writers;
using GottaManagePlus.Utils;

namespace GottaManagePlus.Services.ProfileServices.Management;

public sealed class LocalProfileCreator(
    ProfileRepository repository,
    ProfileZipWriter zipWriter,
    GameEnvironmentController controller
    ) : IProfileCreator
{
    // ---- Private API ----
    private readonly ProfileRepository _repository = repository;
    private readonly ProfileZipWriter _zipWriter = zipWriter;
    private readonly GameEnvironmentController _controller = controller;

    // ---- Public API ----
    /// <summary>
    /// Creates a new empty profile to the local storage.
    /// </summary>
    /// <param name="basicMetadataReference">
    /// The basic <see cref="ProfileMetadata"/> instance to represent
    /// non-storage data.
    /// </param>
    public async Task<ProfileMetadata> CreateProfile(ProfileMetadata basicMetadataReference)
    {
        // Get the directory, if it exists.
        var profilesFolder = _controller.GetOrCreateProfilesFolderPath();
        
        // Create a new empty profile.
        var clearedMetadata = await _zipWriter.WriteEmptyProfileToAsync(profilesFolder, basicMetadataReference, _controller);
        
        // Add that profile to the database.
        _repository.Add(clearedMetadata);

        return clearedMetadata;
    }
}