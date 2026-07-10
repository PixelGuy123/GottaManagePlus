using GottaManagePlus.Models;
using GottaManagePlus.Models.GameEnvironments;

namespace GottaManagePlus.Interfaces.ProfileManagement;

public interface ILocalToEnvironmentParser
{
    Task<bool> ExtractProfileToEnvironmentAsync(
        ProfileMetadata metadata,
        IProgress<ProgressReport>? progress,
        CancellationToken cancellationToken = default);
}