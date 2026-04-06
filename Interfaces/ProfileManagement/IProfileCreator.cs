using System.Threading.Tasks;
using GottaManagePlus.Models;

namespace GottaManagePlus.Interfaces.ProfileManagement;

public interface IProfileCreator
{
    Task<ProfileMetadata?> CreateProfile(ProfileMetadata basicMetadataReference);
}