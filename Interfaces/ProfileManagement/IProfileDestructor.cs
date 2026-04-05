using System;
using System.Threading.Tasks;
using GottaManagePlus.Models;

namespace GottaManagePlus.Interfaces.ProfileManagement;

public interface IProfileDestructor
{
    Task DeleteProfile(ProfileMetadata metadata, IProgress<ProgressReport>? progress);
}