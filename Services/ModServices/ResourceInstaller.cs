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

using System.Text.Json;
using GottaManagePlus.Models;

using GottaManagePlus.Services.GameEnvironmentServices;
using GottaManagePlus.Utils;
using Serilog;

namespace GottaManagePlus.Services.ModServices;

public sealed class ResourceInstaller(ILogger logger, GameEnvironmentController gameEnvironmentController)
{
    private readonly ILogger _logger = logger;
    private readonly GameEnvironmentController _gameEnvironmentController = gameEnvironmentController;
    private readonly PatcherIndexManager _patcherIndexManager = new(logger, gameEnvironmentController);

    /// <summary>
    /// Installs the mod into the assigned folder and attempts to install a metadata file inside too.
    /// </summary>
    /// <param name="modRootPath">The root path of the mod's folder.</param>
    /// <param name="manifest">The manifest to receive a new metadata.</param>
    /// <returns>The metadata of the resource installation.</returns>
    public Result InstallResources(
        string modRootPath,
        ModManifest manifest)
    {
        // Track moved files and directories for rollback in case of failure
        var movedItems = new List<(string Path, bool IsDirectory)>();

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
            
            // Get all resources.
            var resources = manifest.GetAllResources(_gameEnvironmentController, modRootPath);

            // First, get the full paths.
            foreach (var (assetType, resource) in resources)
            {
                switch (assetType)
                {
                    // Try to move directory asset to the right destination.
                    case ModManifestUtils.AssetType.Asset:
                        // Check asset locations.
                        // * If LocalPath does not exist OR
                        // * If the destination is empty OR
                        // * If the destination is not a directory
                        if (!Directory.Exists(resource.LocalPath) || string.IsNullOrEmpty(resource.Destination) ||
                            !File.GetAttributes(resource.Destination).HasFlag(FileAttributes.Directory))
                        {
                            var message = !Path.EndsInDirectorySeparator(resource.LocalPath)
                                ? $"Skipped asset '{resource}' due to localPath being a file, not a directory."
                                : $"Skipped asset '{resource}' for lacking path.";
                            _logger.Warning("{msg}", message);
                            throw new InvalidOperationException("Asset does not contain a proper path. Check logs.");
                        }
                        
                        // Then move the asset.
                        _logger.Information("Moved {localPath} to {newDir}", resource.LocalPath, resource.MovedAsset);
                        new DirectoryInfo(resource.LocalPath).AtomicallyMoveTo(resource.MovedAsset);
                        movedItems.Add((resource.MovedAsset, true));
                        break;
                    // Move the plugin to the new directory.
                    case ModManifestUtils.AssetType.Plugin:
                        if (File.Exists(resource.LocalPath))
                        {
                            var pluginDestinationPath =
                                Path.Combine(pluginDir.FullName, Path.GetFileName(resource.LocalPath));
                            _logger.Information("Moved {localPath} to {newDir}", resource.LocalPath,
                                pluginDestinationPath);
                            File.Move(resource.LocalPath, pluginDestinationPath, true);
                            movedItems.Add((pluginDestinationPath, false));
                        }

                        break;
                    
                    // Move the patcher to the right directory.
                    case ModManifestUtils.AssetType.Patcher:
                        if (File.Exists(resource.LocalPath))
                        {
                            var patcherFileName = Path.GetFileName(resource.LocalPath);
                            
                            // Register the patcher in the .index system
                            _patcherIndexManager.RegisterPatcher(manifest, patcherFileName);
                            
                            var patcherDestinationPath =
                                (string)Path.Combine(patcherDir.FullName, patcherFileName);
                            
                            _logger.Information("Moved {localPath} to {newDir}", resource.LocalPath,
                                patcherDestinationPath);
                            File.Move(resource.LocalPath, patcherDestinationPath, true);
                            movedItems.Add((patcherDestinationPath, false));
                        }

                        break;
                    default:
                        throw new InvalidOperationException("Invalid AssetType value.");
                }
            }
            
            // Move the internal .gmp folder to the right place.
            var gmpRootPath = (string)Path.Combine(modRootPath, Constants.App_SpecialFolderForMods_Name);
            var gmpDestinationPath = _gameEnvironmentController.SearchAbsolutePath(
                    pluginDir.FullName, Constants.App_SpecialFolderForMods_Name);
            
            DirectoryUtils.AtomicallyMoveTo(gmpRootPath, gmpDestinationPath);
            movedItems.Add((gmpDestinationPath, true));

            // Save the metadata.
            manifest.SaveMetadataToDisk(_gameEnvironmentController, _logger);

            _logger.Information("Resource Installation completed!");
            return Result.Success();
        }
        catch (Exception e)
        {
            _logger.Error(e, "Error during mod installation.");
            
            // Rollback moved files and directories in reverse order
            for (var i = movedItems.Count - 1; i >= 0; i--)
            {
                var item = movedItems[i];
                try
                {
                    if (item.IsDirectory)
                    {
                        if (Directory.Exists(item.Path))
                            Directory.Delete(item.Path, true);
                    }
                    else
                    {
                        if (File.Exists(item.Path))
                            File.Delete(item.Path);
                    }
                }
                catch (Exception rollbackEx)
                {
                    // Log rollback failures but don't let them interrupt the rest of the rollback process
                    _logger.Error(rollbackEx, "Failed to rollback file/directory: '{path}'", item.Path);
                }
            }
            
            return Result.Failure(e.Message);
        }
    }
}