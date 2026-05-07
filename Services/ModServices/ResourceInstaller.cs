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
        // Form the path that plugins will go to.
        try
        {
            _logger.Information("Initializing metadata and installation...");
            // Setup Plugin requirements.
            var pluginDir =
                DirectoryUtils.GetOrCreate(manifest.GetPluginDirectoryFromManifest(_gameEnvironmentController));
            var patcherDir =
                DirectoryUtils.GetOrCreate(_gameEnvironmentController.SearchAbsolutePath(Constants.BepInExFolderName,
                    Constants.PatchersFolder));

            // Setup metadata.
            manifest.Metadata.Activated = true;

            // LastUpdateTime Setup
            var dateTime = DateTime.Now;
            manifest.Metadata.LastUpdateDate = new DateOnly(dateTime.Year, dateTime.Month, dateTime.Day);

            // First, get the full paths.
            foreach (var (assetType, resource) in manifest.GetAllResources(modRootPath))
            {
                switch (assetType)
                {
                    // Try to move directory asset to the right destination.
                    case ModManifestUtils.AssetType.Asset:
                        if (!Directory.Exists(resource.LocalPath) || string.IsNullOrEmpty(resource.Destination) ||
                            Directory.Exists(resource.Destination)) continue;

                        _logger.Information("Moved {localPath} to {newDir}", resource.LocalPath, resource.Destination);
                        Directory.Move(resource.LocalPath, resource.Destination!);
                        break;
                    // Move the plugin to the new directory.
                    case ModManifestUtils.AssetType.Plugin:
                        if (File.Exists(resource.LocalPath))
                        {
                            var pluginDestinationPath =
                                Path.Combine(pluginDir.FullName, Path.GetFileName(resource.LocalPath));
                            _logger.Information("Moved {localPath} to {newDir}", resource.LocalPath,
                                pluginDestinationPath);
                            File.Move(resource.LocalPath, pluginDestinationPath);
                        }

                        break;
                    
                    // Move the patcher to the right directory.
                    case ModManifestUtils.AssetType.Patcher:
                        if (File.Exists(resource.LocalPath))
                        {
                            var patcherDestinationPath =
                                Path.Combine(patcherDir.FullName, Path.GetFileName(resource.LocalPath));
                            
                            _logger.Information("Moved {localPath} to {newDir}", resource.LocalPath,
                                patcherDestinationPath);
                            File.Move(resource.LocalPath, patcherDestinationPath);
                        }

                        break;
                    default:
                        throw new InvalidOperationException("Invalid AssetType value.");
                }
            }
            
            // Move the internal _gmp folder to the right place.
            var gmpRootPath = Path.Combine(modRootPath, Constants.App_SpecialFolderForMods_Name);
            DirectoryUtils.AtomicallyMoveTo(gmpRootPath, 
                _gameEnvironmentController.SearchAbsolutePath(
                    pluginDir.FullName, Constants.App_SpecialFolderForMods_Name));

            // Save the metadata.
            manifest.SaveMetadataToDisk(_gameEnvironmentController, _logger);

            _logger.Information("Resource Installation completed!");
        }
        catch (Exception e)
        {
            _logger.Error(e, "Error during mod installation.");
        }
    }
}