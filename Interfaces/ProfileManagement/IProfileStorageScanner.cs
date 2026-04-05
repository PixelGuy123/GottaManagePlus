using GottaManagePlus.Services.ProfileServices;

namespace GottaManagePlus.Interfaces.ProfileManagement;

public interface IProfileStorageScanner
{
    void ScanAndLoadProfiles();
}