using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FileTypeChecker;
using FileTypeChecker.Extensions;
using FileTypeChecker.Types;
using GottaManagePlus.Models;
using GottaManagePlus.Models.UI;
using GottaManagePlus.Utils;

namespace GottaManagePlus.Services.ModServices;

/// <summary>
/// A service for scanning a mod's archive and detecting any suspicious files.
/// </summary>
public static class SecurityScanner
{
    public static async Task ScanAsync(string modRootPath, ModInstallationResult result, IProgress<ProgressReport>? progress,
        ModManifest manifest, CancellationToken cancellationToken = default)
    {
        // Get a flatted out array of every asset to be scanned
        var allAssets = manifest.GetAllResources(modRootPath);

        var numOfTasks = 0;

        // Go through each asset and scan them
        foreach (var (isAPlugin, resource) in allAssets)
        {
            progress?.Report(new ProgressReport(numOfTasks, allAssets.Length, "Scanning files:", $"Checking \'{resource}\'"));
            var file = new FileInfo(resource);
            if (isAPlugin)
            {
                numOfTasks++;
                if (await IsPluginSuspicious(file))
                    result.SecurityIssues.Add($"\'{resource}\' was detected as an executable!");
                continue;
            }
            
            if (await IsAssetSuspicious(file))
                    result.SecurityIssues.Add($"\'{resource}\' was detected as an executable!");
            numOfTasks++;
        }

        return;
        
        // True if yes; False if no
        async Task<bool> IsPluginSuspicious(FileInfo file)
        {
            await using var stream = file.OpenRead();
            // Allowed plugin extensions: .xml, .dll, .pdb
            // Suspicious if the file is an executable (by content) but its extension is not .dll
            return file.Extension != ".dll" && (await stream.IsAsync<Executable>(cancellationToken) || await stream.IsAsync<ExecutableAndLinkableFormat>(cancellationToken));
        }

        async Task<bool> IsAssetSuspicious(FileInfo file)
        {
            // Allowed assets: images, audios, videos and JSON
            await using var stream = file.OpenRead();
            if (await stream.IsImageAsync(cancellationToken) ||
                await stream.IsAudio(cancellationToken) ||
                await stream.IsVideo(cancellationToken))
                return false;
            
            // Check if it's an executable itself
            return await stream.IsAsync<Executable>(cancellationToken) ||
                   await stream.IsAsync<ExecutableAndLinkableFormat>(cancellationToken);
        }
    }
}