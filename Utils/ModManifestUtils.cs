using System;
using System.Collections.Generic;
using System.IO;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using GottaManagePlus.Models;
using GottaManagePlus.Models.UI;
using GottaManagePlus.Services.GameEnvironmentServices;

namespace GottaManagePlus.Utils;

public static class ModManifestUtils
{
    // Cache for thumbnail images
    private static readonly Dictionary<string, Bitmap> BitmapThumbnailCache = [];

    /// <summary>
    /// Attempts to load the thumbnail URI of a <see cref="ModManifest"/> and caches it into a global <see cref="Dictionary{string,Bitmap}"/>. 
    /// </summary>
    /// <param name="item">The <see cref="ModManifest"/> to be used.</param>
    /// <returns>Returns an instance of <see cref="Bitmap"/> if successful, or return <see langword="null"/> if anything goes wrong.</returns>
    public static Bitmap? GetThumbnailImageAsBitmap(this ModManifest item)
    {
        if (string.IsNullOrEmpty(item.Metadata.Thumbnail)) return null;

        // Try to get a cached Bitmap
        if (BitmapThumbnailCache.TryGetValue(item.Metadata.Thumbnail, out var image))
            return image;

        // Otherwise, just load it and cache it
        try
        {
            image = new Bitmap(AssetLoader.Open(new Uri(item.Metadata.Thumbnail)));
            BitmapThumbnailCache.Add(item.Metadata.Thumbnail, image);
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
    /// <param name="metadata">The metadata to be scanned.</param>
    /// <returns>A path of the file if it exists; otherwise, null.</returns>
    public static string? DetermineImageThroughCheck(this ModMetadata metadata)
    {
        var path = Path.GetDirectoryName(metadata.Path);
        if (string.IsNullOrEmpty(path)) return null;
        
        if (File.Exists(Path.Combine(path, "thumbnail.png")))
            return Path.Combine(path, "thumbnail.png");
        if (File.Exists(Path.Combine(path, "thumbnail.bmp")))
            return Path.Combine(path, "thumbnail.bmp");
        return File.Exists(Path.Combine(path, "thumbnail.jpg")) ? Path.Combine(path, "thumbnail.jpg") : null;
    }

    /// <summary>
    /// Get the path from a <see cref="ModManifest"/> instance using its own attributes.
    /// </summary>
    /// <param name="manifest">The manifest to be exposed.</param>
    /// <param name="controller">The controller for controlled search.</param>
    /// <returns>A <see cref="string"/> with the proper path to the right folder.</returns>
    public static string GetPluginDirectoryFromManifest(this ModManifest manifest, GameEnvironmentController controller) =>
        controller.SearchAbsolutePath(Constants.BepInExFolderName, Constants.PluginsFolder, manifest.Name);

    /// <summary>
    /// Gather all the assets and plugins registered in the manifest in a single array.
    /// </summary>
    /// <param name="manifest">The manifest to be scanned.</param>
    /// <param name="relativeBasePath">The base path the relative paths from the manifest will address.</param>
    /// <returns>An array filled with paths and whether they
    /// are an asset (<see langword="true"/>) or a plugin (<see langword="false"/>).</returns>
    public static (bool, DestinedAsset)[] GetAllResources(this ModManifest manifest, string relativeBasePath)
    {
        // Sum up of files to gather (Plugins have the size bigger due to .xml and .pdb files).
        var max = manifest.Plugins.Count * 3 + manifest.Assets.Count;
        var array = new (bool, DestinedAsset)[max];
        var index = 0;
        
        // Plugins retrieval
        foreach (var plugin in manifest.Plugins)
        {
            // .dll
            array[index++] = (true, new DestinedAsset
            {
                LocalPath = Path.Combine(relativeBasePath, plugin),
            });
            // Get the directory name too.
            var directoryName = Path.GetDirectoryName(plugin);
            // .xml
            array[index++] = (true, new DestinedAsset
            {
                LocalPath = Path.Combine(relativeBasePath, GetPathFromPlugin(".xml")),
            });
            // .pdb
            array[index++] = (true, new DestinedAsset
            {
                LocalPath = Path.Combine(relativeBasePath, GetPathFromPlugin(".pdb")),
            });
            
            continue;

            string GetPathFromPlugin(string newExtension) =>
                !string.IsNullOrEmpty(directoryName)
                    ? Path.Combine(directoryName, Path.GetFileNameWithoutExtension(plugin) + newExtension) : 
                    Path.GetFileNameWithoutExtension(plugin) + newExtension;
        }

        // Assets retrieval
        foreach (var asset in manifest.Assets)
            array[index++] = (true, new DestinedAsset
            {
                LocalPath = Path.Combine(relativeBasePath, asset.LocalPath), 
                Destination = string.IsNullOrEmpty(asset.Destination) ? null : asset.Destination
            });
        return array;
    } 
}