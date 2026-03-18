using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GottaManagePlus.Interfaces;
using GottaManagePlus.Models;
using GottaManagePlus.Utils;

namespace GottaManagePlus.Services.ModServices;

/// <summary>
/// Service that handles copying mod resources to their appropriate
/// destinations within the game folder structure.
/// </summary>
public static class ResourceInstaller
{
    public static async Task<ResourceInstallationSummary> InstallResourcesAsync(
        string modRootPath,
        InstallationProgressManager progress,
        ModManifest metadata,
        IGameFolderViewer gameViewer,
        CancellationToken cancellationToken = default)
    {
    }
}