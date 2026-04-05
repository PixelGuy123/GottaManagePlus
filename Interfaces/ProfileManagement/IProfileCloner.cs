using System.Threading.Tasks;
using GottaManagePlus.Models;

namespace GottaManagePlus.Interfaces.ProfileManagement;

public interface IProfileCloner
{
    Task CloneProfile(ProfileMetadata metadata, string newName);
}