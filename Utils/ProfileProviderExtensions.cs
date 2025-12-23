using GottaManagePlus.Interfaces;
using GottaManagePlus.Models;

namespace GottaManagePlus.Utils;

public static class ProfileProviderExtensions
{
    public static ProfileItem GetInstanceActiveProfile(this IProfileProvider provider) =>
        provider.GetLoadedProfiles()[provider.GetActiveProfile()];
}