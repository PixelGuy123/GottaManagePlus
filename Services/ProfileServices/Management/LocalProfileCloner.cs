using GottaManagePlus.Interfaces.ProfileManagement;
using GottaManagePlus.Models;
using GottaManagePlus.Services.GameEnvironmentServices;
using GottaManagePlus.Services.ProfileServices.Writers;
using GottaManagePlus.Utils;

namespace GottaManagePlus.Services.ProfileServices.Management;

public class LocalProfileCloner(
    ProfileRepository repository,
    GameEnvironmentController controller
) : IProfileCloner
{
    // ---- Private API ----
    private readonly ProfileRepository _repository = repository;
    private readonly GameEnvironmentController _controller = controller;

    // ---- Public API ----
    public ProfileMetadata? CloneProfile(ProfileMetadata metadata, string newName)
    {
        // Get Profiles' folder.
        var profilesFolder = _controller.GetOrCreateProfilesFolderPath();
        
        // Get the metadata inside the storage.
        var metadataPath = metadata.GetPhysicalPath(_controller);
        
        // Get a copy of the metadata.
        var newMetadata = new ProfileMetadata(metadata, false)
        {
            Name = newName
        };
        
        // Registers the new metadata.
        if (!_repository.Add(newMetadata))
            return null;
        
        // Copy the directory under a new name.
        var newProfileDir = Directory.CreateDirectory(Path.Combine(profilesFolder, newName));
        foreach (var file in Directory.EnumerateFiles(metadataPath, "*", SearchOption.AllDirectories))
        {
            // Copy the file to the new directory.
            File.Copy(file, 
                Path.Combine(newProfileDir.FullName, Path.GetFileName(file)), 
                true);
        }
        
        // Write the metadata to the new directory.
        File.WriteAllText(
            Path.Combine(newProfileDir.FullName, Constants.ProfileMetadataFileName),
            newMetadata.Serialize());
        
        // Locate the compacted file and rename it.
        var zipFilePath = Path.Combine(newProfileDir.FullName, metadata.Name + Constants.ProfileDefaultExtension);
        if (File.Exists(zipFilePath))
            File.Move(zipFilePath,
                Path.Combine(newProfileDir.FullName,
                    newName + Constants.ProfileDefaultExtension));
        
        return newMetadata;
    }
}