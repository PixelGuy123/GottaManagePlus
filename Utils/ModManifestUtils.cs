using System;
using System.Collections.Generic;
using System.IO;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using GottaManagePlus.Models;
using GottaManagePlus.Models.UI;

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
    /// Gather all the assets and plugins registered in the manifest in a single array.
    /// </summary>
    /// <param name="manifest">The manifest to be scanned.</param>
    /// <param name="relativeBasePath">The base path the relative paths from the manifest will address.</param>
    /// <returns>An array filled with paths and whether they
    /// are an asset (<see langword="true"/>) or a plugin (<see langword="false"/>).</returns>
    public static (bool, string)[] GetAllResources(this ModManifest manifest, string relativeBasePath)
    {
        // Sum up of files to gather
        var max = manifest.Plugins.Count + manifest.Assets.Count;
        var array = new (bool, string)[max];
        var index = 0;
        
        // Plugins retrieval
        foreach (var plugin in manifest.Plugins)
            array[index++] = (false, Path.Combine(relativeBasePath, plugin));
        // Assets retrieval
        foreach (var asset in manifest.Assets)
            array[index++] = (true, Path.Combine(relativeBasePath, asset.LocalPath));
        return array;
    } 
}