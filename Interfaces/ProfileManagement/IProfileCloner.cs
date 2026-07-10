using GottaManagePlus.Models;
using GottaManagePlus.Models.GameEnvironments;

namespace GottaManagePlus.Interfaces.ProfileManagement;

public interface IProfileCloner
{
    ProfileMetadata? CloneProfile(ProfileMetadata metadata, string newName);
}