using GottaManagePlus.Models;

namespace GottaManagePlus.Interfaces.ProfileManagement;

public interface IProfileCloner
{
    ProfileMetadata? CloneProfile(ProfileMetadata metadata, string newName);
}