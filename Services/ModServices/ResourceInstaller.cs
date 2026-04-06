using System;
using System.IO;
using System.Text.Json;
using GottaManagePlus.Models;
using GottaManagePlus.Models.SourceGenerators;
using GottaManagePlus.Services.GameEnvironmentServices;
using GottaManagePlus.Utils;
using Serilog;

namespace GottaManagePlus.Services.ModServices;

public sealed class ResourceInstaller(ILogger logger, GameEnvironmentController gameEnvironmentController)
{
    private readonly ILogger _logger = logger;
    private readonly GameEnvironmentController _gameEnvironmentController = gameEnvironmentController;
    
    private static readonly JsonSerializerOptions Options = new() { TypeInfoResolver = ModManifestContext.Default };
    
    /// <summary>
    /// Installs the mod into the assigned folder and attempts to install a metadata file inside too.
    /// </summary>
    /// <param name="modRootPath">The root path of the mod's folder.</param>
    /// <param name="manifest">The manifest to receive a new metadata.</param>
    /// <returns>The metadata of the resource installation.</returns>
    public void InstallResources(
        string modRootPath,
        ModManifest manifest)
    {
        ModMetadata newMetadata = null!;
        // Form the path that plugins will go to.
        try
        {
            _logger.Information("Initializing metadata and installation...");
            // Setup Plugin requirements.
            var pluginDir =
                new DirectoryInfo(manifest.GetPluginDirectoryFromManifest(_gameEnvironmentController));
            if (!pluginDir.Exists) pluginDir.Create(); // Try and create directory.
            
            // Setup metadata.
            manifest.Metadata.Activated = true;
            manifest.Metadata.Path = _gameEnvironmentController.SearchAbsolutePath(pluginDir.FullName,
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
                        _logger.Information("Moved {localPath} to {newDir}", resource.LocalPath, pluginDir.FullName);
                        File.Move(resource.LocalPath, pluginDir.FullName);
                    }
                    continue;
                }
                
                // Try to move directory asset to the right destination.
                if (!Directory.Exists(resource.LocalPath) || !Directory.Exists(resource.Destination)) continue;
                
                _logger.Information("Moved {localPath} to {newDir}", resource.LocalPath, resource.Destination);
                Directory.Move(resource.LocalPath, resource.Destination);
            }
            
            _logger.Information("Creating .metadata file...");
            
            // Serialize metadata, then create it in the files.
            File.WriteAllText(newMetadata.Path!, 
                JsonSerializer.Serialize(newMetadata, Options));
            newMetadata.Thumbnail = newMetadata.DetermineImageThroughCheck();
            
            _logger.Information("Installation completed!");
        }
        catch (Exception e)
        {
            _logger.Error("Error during mod installation.\n{exception}", e);
        }
    }
}