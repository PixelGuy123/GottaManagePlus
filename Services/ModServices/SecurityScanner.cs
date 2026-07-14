/*
This file is part of GottaManagePlus (https://github.com/PixelGuy123/GottaManagePlus)

    Copyright (C) 2026 PixelGuy123

    This program is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with this program.  If not, see <https://www.gnu.org/licenses/>.

*/

using FileTypeChecker.Extensions;
using FileTypeChecker.Types;
using GottaManagePlus.Models;
using GottaManagePlus.Models.ModManagement;
using GottaManagePlus.Services.GameEnvironmentServices;
using GottaManagePlus.Utils;
using Serilog;

namespace GottaManagePlus.Services.ModServices;

/// <summary>
/// A service for scanning a mod's archive and detecting any suspicious files.
/// </summary>
public sealed class SecurityScanner(ILogger logger)
{
    // ---- Private -----
    private readonly ILogger _logger = logger;
    
    // ---- Public ----
    /// <summary>
    /// Scans a mod's file structure in order to find any suspicious file in the assets or plugins.
    /// </summary>
    /// <param name="modRootPath">The root path of the mod folder structure.</param>
    /// <param name="controller">The controller for the environment.</param>
    /// <param name="result">The result report that needs to be updated with this function.</param>
    /// <param name="progress">The progress to be reported.</param>
    /// <param name="manifest">The mod's manifest itself.</param>
    /// <param name="cancellationToken">The token in case the action is canceled.</param>
    /// <returns><see langword="true"/> if the manifest is safe for loading; otherwise, <see langword="false"/>.</returns>
    public async Task<bool> ScanAsync(string modRootPath, GameEnvironmentController controller, ModInstallationResult result, IProgress<ProgressReport>? progress,
        ModManifest manifest, CancellationToken cancellationToken = default)
    {
        _logger.Information("Starting security scan on '{modRootPath}'", modRootPath);
        // Get a flatted out array of every asset to be scanned
        var allAssets = manifest.GetAllResources(controller, modRootPath);
        var safeForLoading = true;
        var numOfTasks = 0;

        // Go through each asset and scan them
        foreach (var (assetType, resource) in allAssets)
        {
            if (!controller.IsPathSafetyValid(resource.LocalPath) ||
                (!string.IsNullOrEmpty(resource.Destination) &&
                 !controller.IsPathSafetyValid(resource.Destination!.Value)))
            {
                _logger.Warning(
                    "SAFETY WARNING: The path {0} attempts to access outside boundaries from the environment.",
                    resource.ToString());
                result.SecurityIssues.Add($"SAFETY WARNING: The path {resource.ToString()} attempts to access outside boundaries from the environment.");
                safeForLoading = false;
            }

            var resourcePath = resource.LocalPath;
            progress?.Report(new ProgressReport(numOfTasks, allAssets.Length, "Scanning files",
                $"Checking '{resource}'"));

            // Make the file info and check if it exists.
            var file = new FileInfo(resourcePath);
            if (!file.Exists)
            {
                numOfTasks++;
                continue;
            }

            // Check if the plugin is suspicious
            switch (assetType)
            {
                // Is Plugin or Patcher suspicious?
                case ModManifestUtils.AssetType.Patcher: 
                case ModManifestUtils.AssetType.Plugin:
                    numOfTasks++;
                    if (await IsPluginSuspicious(file))
                    {
                        WarnSecurityIssue(resourcePath);
                    }
                    break;
                
                // Is asset suspicious?
                case ModManifestUtils.AssetType.Asset:
                    if (await IsAssetSuspicious(file))
                    {
                        WarnSecurityIssue(resourcePath);
                    }
                    numOfTasks++;
                    break;
                default:
                    throw new InvalidOperationException("AssetType is invalid.");
            }
        }
        
        _logger.Information("Finished scan!");
        return safeForLoading;

        void WarnSecurityIssue(string resource)
        {
            result.SecurityIssues.Add($"'{resource}' was detected as an executable!");
            _logger.Warning("'{resource}' was detected as an executable!", resource);
        }

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
