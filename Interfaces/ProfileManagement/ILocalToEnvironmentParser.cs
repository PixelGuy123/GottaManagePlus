using System;
using System.Threading;
using System.Threading.Tasks;
using GottaManagePlus.Models;

namespace GottaManagePlus.Interfaces.ProfileManagement;

public interface ILocalToEnvironmentParser
{
    Task<bool> ExtractProfileToEnvironmentAsync(
        ProfileMetadata metadata,
        IProgress<ProgressReport>? progress,
        CancellationToken cancellationToken = default);
}