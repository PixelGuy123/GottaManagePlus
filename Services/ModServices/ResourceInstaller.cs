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
            var pluginDir = DirectoryUtils.GetOrCreate(manifest.GetPluginDirectoryFromManifest(_gameEnvironmentController));
            var patcherDir = DirectoryUtils.GetOrCreate(manifest.GetPatchersDirectoryFromManifest(_gameEnvironmentController));
            
            // Setup metadata.
            manifest.Metadata.Activated = true;
            manifest.Metadata.Path = _gameEnvironmentController.SearchAbsolutePath(pluginDir.FullName,
                Constants.App_SpecialFolderForMods_Name,
                ".metadata");

            // First, get the full paths.
            foreach (var (assetType, resource) in manifest.GetAllResources(modRootPath))
            {
                switch (assetType)
                {
                    // Try to move directory asset to the right destination.
                    case ModManifestUtils.AssetType.Asset:
                        if (!Directory.Exists(resource.LocalPath) || !Directory.Exists(resource.Destination)) continue;
                
                        _logger.Information("Moved {localPath} to {newDir}", resource.LocalPath, resource.Destination);
                        Directory.Move(resource.LocalPath, resource.Destination);
                        break;
                    // Move the plugin to the new directory.
                    case ModManifestUtils.AssetType.Plugin:
                        if (File.Exists(resource.LocalPath))
                        {
                            _logger.Information("Moved {localPath} to {newDir}", resource.LocalPath, pluginDir.FullName);
                            File.Move(resource.LocalPath, pluginDir.FullName);
                        }
                        break;
                    case ModManifestUtils.AssetType.Patcher:
                        if (File.Exists(resource.LocalPath))
                        {
                            _logger.Information("Moved {localPath} to {newDir}", resource.LocalPath, patcherDir.FullName);
                            File.Move(resource.LocalPath, patcherDir.FullName);
                        }
                        break;
                    default:
                        throw new InvalidOperationException("Invalid AssetType value.");
                }
            }
            
            _logger.Information("Creating .metadata file...");
            
            // Serialize metadata, then create it in the files.
            File.WriteAllText(newMetadata.Path!, 
                JsonSerializer.Serialize(newMetadata, ModManifestContext.Default.ModMetadata));
            newMetadata.Thumbnail = newMetadata.DetermineImageThroughCheck();
            
            _logger.Information("Installation completed!");
        }
        catch (Exception e)
        {
            _logger.Error("Error during mod installation.\n{exception}", e);
        }
    }
}