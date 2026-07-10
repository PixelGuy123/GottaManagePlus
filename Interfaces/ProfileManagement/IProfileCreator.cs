using GottaManagePlus.Models;
using GottaManagePlus.Models.GameEnvironments;

namespace GottaManagePlus.Interfaces.ProfileManagement;

public interface IProfileCreator
{
    Task<ProfileMetadata?> CreateProfile(ProfileMetadata basicMetadataReference);
    Task<ProfileMetadata?> CreateProfileFromCurrentEnvironment(string name, IProgress<ProgressReport>? progress);
}