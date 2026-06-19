using GottaManagePlus.Models;

namespace GottaManagePlus.Interfaces.ProfileManagement;

public interface IEnvironmentToLocalParser
{
    Task SaveEnvironmentToProfileAsync(
        ProfileMetadata metadata,
        IProgress<ProgressReport>? progress,
        CancellationToken cancellationToken = default);
}