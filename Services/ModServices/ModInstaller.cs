using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GottaManagePlus.Interfaces;
using GottaManagePlus.Models;
using GottaManagePlus.Models.UI;
using GottaManagePlus.Services.PlusFolderServices;
using Serilog;

namespace GottaManagePlus.Services.ModServices;

/// <summary>
/// Coordinates the complete end-to-end process of installing a mod from a compressed archive file
/// into the target game folder.
/// </summary>
public class ModInstaller
{
    public static async Task<ModInstallationResult> InstallModAsync(
        string archivePath,
        PlusFolderBrowser browser,
        IProgress<ProgressReport>? progress = null,
        CancellationToken cancellationToken = default)
    {
        // 1. Create a logger for this phase.
        await using var modLogger = new LoggerConfiguration()
#if DEBUG
            .WriteTo.Console()
#endif
            .WriteTo.File(Path.Combine(Constants.ApplicationLocation, "Logs", "ModInstallation_" + DateTime.Now.ToLongTimeString() + ".log"))
            .CreateLogger();

        // 2. First, we need to physically extract the archive to somewhere, so that
        // file manipulation can be performed.

    }
}