using GottaManagePlus.Models;

namespace GottaManagePlus.Interfaces.ProfileManagement;

public interface IProfileExportController
{
    void ExportProfile(ProfileMetadata profile);
    ProfileMetadata? ReadExportedProfile(string path);
    void ExtractExportedProfile(string path);
}