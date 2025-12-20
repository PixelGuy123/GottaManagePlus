using System.Collections.Generic;
using GottaManagePlus.Models;

namespace GottaManagePlus.Interfaces;

public interface IProfileProvider : IFilesService
{
    public IReadOnlyCollection<ProfileItem> GetLoadedProfiles();
    public void UpdateActiveProfile();
    public ProfileItem GetActiveProfile();
}