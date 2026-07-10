using System.Text.Json;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using GottaManagePlus.Models;
using GottaManagePlus.Models.ModManagement;
using GottaManagePlus.Services.GameEnvironmentServices;
using Serilog;
using ModManifestContext = GottaManagePlus.Utils.SourceGenerators.ModManifestContext;

namespace GottaManagePlus.Utils;

public static class ModManifestUtils
{
    // Cache for thumbnail images
    private static readonly Dictionary<string, Bitmap> BitmapThumbnailCache = [];
    
    // Enum for a planed ModManifest's assets
    public enum AssetType
    {
        Asset,
        Plugin,
        Patcher
    }

    /// <param name="metadata">The <see cref="ModMetadata"/> to be used.</param>
    extension(ModMetadata metadata)
    {
        /// <summary>
        /// Attempts to load the thumbnail URI of a <see cref="ModMetadata"/> and caches it into a global <see cref="Dictionary{string,Bitmap}"/>. 
        /// </summary>
        /// <returns>Returns an instance of <see cref="Bitmap"/> if successful, or return <see langword="null"/> if anything goes wrong.</returns>
        public Bitmap? GetThumbnailImageAsBitmap()
        {
            if (string.IsNullOrEmpty(metadata.Thumbnail)) return null;

            // Try to get a cached Bitmap
            if (BitmapThumbnailCache.TryGetValue(metadata.Thumbnail, out var image))
                return image;

            // Otherwise, just load it and cache it
            try
            {
                image = new Bitmap(AssetLoader.Open(new Uri(metadata.Thumbnail)));
                BitmapThumbnailCache.Add(metadata.Thumbnail, image);
                return image;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Checks whether the image is a file that is valid (png, bmp and jpg).
        /// </summary>
        /// <returns>A path of the file if it exists; otherwise, null.</returns>
        public string? DetermineImageThroughCheck()
        {
            var path = Path.GetDirectoryName(metadata.Path);
            if (string.IsNullOrEmpty(path)) return null;
        
            if (File.Exists(Path.Combine(path, "thumbnail.png")))
                return (string)Path.Combine(path, "thumbnail.png");
            if (File.Exists(Path.Combine(path, "thumbnail.bmp")))
                return (string)Path.Combine(path, "thumbnail.bmp");
            return File.Exists(Path.Combine(path, "thumbnail.jpg")) ? Path.Combine(path, "thumbnail.jpg") : null;
        }
    }

    /// <param name="manifest">The manifest to be exposed.</param>
    extension(ModManifest manifest)
    {
        /// <summary>
        /// Get the path from a <see cref="ModManifest"/> instance using its own attributes for the Plugins folder.
        /// </summary>
        /// <param name="controller">The controller for controlled search.</param>
        /// <returns>A <see cref="string"/> with the proper path to the Plugins folder.</returns>
        public string GetPluginDirectoryFromManifest(GameEnvironmentController controller) =>
            controller.SearchAbsolutePath(Constants.BepInExFolderName, Constants.PluginsFolder, manifest.ToString());
        
        /// <summary>
        /// Gets the absolute file system path where the mod's metadata file should be stored.
        /// </summary>
        /// <param name="controller">The controller for controlled search.</param>
        /// <returns>The full path to the .metadata file.</returns>
        public string GetMetadataFilePath(GameEnvironmentController controller) =>
            Path.Combine(
                manifest.GetPluginDirectoryFromManifest(controller),
                Constants.App_SpecialFolderForMods_Name,
                Constants.ModMetadataDefaultFileName);

        /// <summary>
        /// Gather all the assets and plugins registered in the manifest in a single array.
        /// </summary>
        /// <param name="controller">The controller to gather the destination location.</param>
        /// <param name="relativeBasePath">The base path the relative paths from the manifest will address.</param>
        /// <returns>An array filled with paths and whether they
        /// are an asset (<see langword="true"/>) or a plugin (<see langword="false"/>).</returns>
        public (AssetType assetType, DestinedAsset destinedAsset)[] GetAllResources(GameEnvironmentController controller, string relativeBasePath)
        {
            // Sum up of files to gather (Plugins have the size bigger due to .xml and .pdb files).
            var max = manifest.Plugins.Count * 3 + manifest.Patchers.Count + manifest.Assets.Count;
            var array = new (AssetType, DestinedAsset)[max];
            var index = 0;
        
            // Plugins retrieval
            foreach (var plugin in manifest.Plugins)
            {
                // .dll
                array[index++] = (AssetType.Plugin, new DestinedAsset
                {
                    LocalPath = Path.Combine(relativeBasePath, plugin),
                });
                // Get the directory name too.
                var directoryName = Path.GetDirectoryName(plugin);
                // .xml
                array[index++] = (AssetType.Plugin, new DestinedAsset
                {
                    LocalPath = Path.Combine(relativeBasePath, GetPathFromPlugin(".xml")),
                });
                // .pdb
                array[index++] = (AssetType.Plugin, new DestinedAsset
                {
                    LocalPath = Path.Combine(relativeBasePath, GetPathFromPlugin(".pdb")),
                });
            
                continue;

                string GetPathFromPlugin(string newExtension) =>
                    !string.IsNullOrEmpty(directoryName)
                        ? (string)Path.Combine(directoryName, Path.GetFileNameWithoutExtension(plugin) + newExtension) : 
                        Path.GetFileNameWithoutExtension(plugin) + newExtension;
            }
        
            // Patchers retrieval
            foreach (var patcher in manifest.Patchers)
            {
                // .dll
                array[index++] = (AssetType.Patcher, new DestinedAsset
                {
                    LocalPath = Path.Combine(relativeBasePath, patcher),
                });
            }

            // Assets retrieval
            foreach (var asset in manifest.Assets)
                array[index++] = (AssetType.Asset, new DestinedAsset
                {
                    LocalPath = Path.Combine(relativeBasePath, asset.LocalPath), 
                    Destination = (string.IsNullOrEmpty(asset.Destination) ? null! : controller.SearchAbsolutePath(asset.Destination!.Value))
                });
            return array;
        }
        /// <summary>
        /// Saves the <see cref="ModMetadata"/> stored in <see cref="ModManifest"/> to disk.
        /// </summary>
        /// <param name="controller">For accessing the environment safely.</param>
        /// <param name="logger">For logging the metadata saving process.</param>
        public void SaveMetadataToDisk(GameEnvironmentController? controller, ILogger? logger = null)
        {
            logger?.Information("Writing .metadata file to disk...");
    
            // Ensure Path is set
            if (string.IsNullOrEmpty(manifest.Metadata.Path))
            {
                if (controller == null)
                    throw new InvalidOperationException("Cannot save metadata: Path is null and no controller provided.");
                
                manifest.Metadata.Path = manifest.GetMetadataFilePath(controller);
            }
    
            var metadataDirectory = Path.GetDirectoryName(manifest.Metadata.Path);
            if (string.IsNullOrEmpty(metadataDirectory))
            {
                logger?.Error("Failed to get the directory name of the metadata file! Path: '{path}'", 
                    manifest.Metadata.Path);
                return;
            }
            DirectoryUtils.GetOrCreate(metadataDirectory);
            File.WriteAllText(manifest.Metadata.Path!,
                JsonSerializer.Serialize(manifest.Metadata, ModManifestContext.Default.ModMetadata));
    
            logger?.Information("Saved .metadata to '{path}' successfully!", manifest.Metadata.Path);
        }
        /// <summary>
        /// Loads from disk the <see cref="ModMetadata"/> instance stored in disk, based on the <paramref name="manifest"/> data.
        /// </summary>
        /// <param name="controller">For accessing the <see cref="ModManifest"/> path.</param>
        /// <param name="logger">For logging the metadata loading process.</param>
        /// <param name="cancellationToken">For cancelling the metadata procedure.</param>
        /// <returns><see langword="true"/> if the metadata was successfully loaded into the <paramref name="manifest"/>; otherwise, <see langword="false"/>.</returns>
        public async Task<bool> LoadMetadataFromDiskAsync(GameEnvironmentController controller, ILogger? logger = null, CancellationToken cancellationToken = default)
        {
            logger?.Information("Loading .metadata file from disk...");
    
            // Get the right path for the .metadata file.
            var metadataPath = manifest.GetMetadataFilePath(controller);
    
            logger?.Debug("Retrieving from path '{path}'...", metadataPath);
            // Look for .metadata file if it is available
            if (!File.Exists(metadataPath))
            {
                logger?.Information("The metadata file does not exist!");
                return false;
            }
    
            // Load the metadata from disk.
            var newMetadata =
                JsonSerializer.Deserialize<ModMetadata>(
                    await File.ReadAllTextAsync(metadataPath, cancellationToken), ModManifestContext.Default.ModMetadata);
    
            // If valid, continue.
            if (newMetadata == null)
            {
                logger?.Information("Failed to load the metadata! Nullable instance found.");
                return false;
            }
    
            // Set up the metadata for the manifest.
            manifest.Metadata = newMetadata;
            newMetadata.Path = metadataPath;
            newMetadata.Thumbnail = newMetadata.DetermineImageThroughCheck();
            logger?.Information("Successfully loaded the metadata file!");
    
            return true;
        }
        
        /// <summary>
        /// Loads from disk the <see cref="ModMetadata"/> instance stored in disk, based on the <paramref name="manifest"/> data.
        /// </summary>
        /// <param name="controller">For accessing the <see cref="ModManifest"/> path.</param>
        /// <param name="logger">For logging the metadata loading process.</param>
        /// <returns><see langword="true"/> if the metadata was successfully loaded into the <paramref name="manifest"/>; otherwise, <see langword="false"/>.</returns>
        public bool LoadMetadataFromDisk(GameEnvironmentController controller, ILogger? logger = null)
        {
            logger?.Information("Loading .metadata file from disk...");
            
            // Get the right path for the .metadata file.
            var metadataPath = manifest.GetMetadataFilePath(controller);
            
            logger?.Debug("Retrieving from path '{path}'...", metadataPath);
            // Look for .metadata file if it is available
            if (!File.Exists(metadataPath))
            {
                logger?.Information("The metadata file does not exist!");
                return false;
            }
            
            // Load the metadata from disk.
            var newMetadata =
                JsonSerializer.Deserialize<ModMetadata>(
                    File.ReadAllText(metadataPath), ModManifestContext.Default.ModMetadata);
            
            // If valid, continue.
            if (newMetadata == null)
            {
                logger?.Information("Failed to load the metadata! Nullable instance found.");
                return false;
            }
            
            // Set up the metadata for the manifest.
            manifest.Metadata = newMetadata;
            newMetadata.Path = metadataPath;
            newMetadata.Thumbnail = newMetadata.DetermineImageThroughCheck();
            logger?.Information("Successfully loaded the metadata file!");
            
            return true;

        }
    }
}