using System;
using System.Threading.Tasks;
using GottaManagePlus.Models;

namespace GottaManagePlus.Interfaces.ProfileManagement;

public interface IProfileCreator
{
    Task<ProfileMetadata?> CreateProfile(ProfileMetadata basicMetadataReference);
    Task<ProfileMetadata?> CreateProfileFromCurrentEnvironment(string name, IProgress<ProgressReport>? progress);
}