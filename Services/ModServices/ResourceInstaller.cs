using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using GottaManagePlus.Interfaces;
using GottaManagePlus.Models;
using GottaManagePlus.Models.SourceGenerators;
using GottaManagePlus.Models.UI;
using GottaManagePlus.Services.PlusFolderServices;
using GottaManagePlus.Utils;
using Serilog.Core;

namespace GottaManagePlus.Services.ModServices;

/// <summary>
/// Service that handles copying mod resources to their appropriate
/// destinations within the game folder structure.
/// </summary>
public static class ResourceInstaller
{
    private static readonly JsonSerializerOptions Options = new() { TypeInfoResolver = ModManifestContext.Default };
    /// <summary>
    /// Installs the mod into the assigned folder and attempts to install a metadata file inside too.
    /// </summary>
    /// <param name="modRootPath">The root path of the mod's folder.</param>
    /// <param name="logger">The logger to log the occurrences.</param>
    /// <param name="browser">The game's browser service.</param>
    /// <param name="manifest">The manifest to receive a new metadata.</param>
    public static void InstallResources(
        string modRootPath, Logger logger,
        PlusFolderBrowser browser,
        ModManifest manifest)
    {
        // Form the path that plugins will go to.
        try
        {
            logger.Information("Initializing metadata and installation...");
            // Setup Plugin requirements.
            var pluginDir =
                new DirectoryInfo(manifest.GetPluginDirectoryFromManifest(browser));
            if (!pluginDir.Exists) pluginDir.Create(); // Try and create directory.
            
            // Setup metadata.
            manifest.Metadata.Activated = true;
            manifest.Metadata.Path = browser.SearchAbsolutePath(pluginDir.FullName,
                Constants.App_SpecialFolderForMods_Name,
                ".metadata");

            // First, get the full paths.
            foreach (var (isPlugin, resource) in manifest.GetAllResources(modRootPath))
            {
                // If it is a plugin, it's easier to move.
                if (isPlugin)
                {
                    // Move the plugin to the new directory.
                    if (File.Exists(resource.LocalPath))
                    {
                        logger.Information("Moved {localPath} to {newDir}", resource.LocalPath, pluginDir.FullName);
                        File.Move(resource.LocalPath, pluginDir.FullName);
                    }
                    continue;
                }
                
                // Try to move directory asset to the right destination.
                if (!Directory.Exists(resource.LocalPath) || !Directory.Exists(resource.Destination)) continue;
                
                logger.Information("Moved {localPath} to {newDir}", resource.LocalPath, resource.Destination);
                Directory.Move(resource.LocalPath, resource.Destination);
            }
            
            logger.Information("Creating .metadata file...");
            
            // Serialize metadata, then create it in the files.
            File.WriteAllText(manifest.Metadata.Path, 
                JsonSerializer.Serialize(manifest.Metadata, Options));
            manifest.Metadata.Thumbnail = manifest.Metadata.DetermineImageThroughCheck();
            
            logger.Information("Installation completed!");
        }
        catch (Exception e)
        {
            logger.Error("Error during mod installation.\n{exception}", e);
        }
    }
}