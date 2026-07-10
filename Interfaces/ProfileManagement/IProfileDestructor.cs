using GottaManagePlus.Models;
using GottaManagePlus.Models.GameEnvironments;

namespace GottaManagePlus.Interfaces.ProfileManagement;

public interface IProfileDestructor
{
    Task DeleteProfile(ProfileMetadata metadata, IProgress<ProgressReport>? progress);
}