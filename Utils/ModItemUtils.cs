using System;
using System.Collections.Generic;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using GottaManagePlus.Models;
using GottaManagePlus.Models.UI;

namespace GottaManagePlus.Utils;

public static class ModItemUtils
{
    // Cache for thumbnail images
    private static readonly Dictionary<string, Bitmap> BitmapThumbnailCache = [];

    /// <summary>
    /// Attempts to load the thumbnail URI of a <see cref="ModItem"/> and caches it into the instance itself and a global <see cref="Dictionary{string,Bitmap}"/>. 
    /// </summary>
    /// <param name="item">The <see cref="ModItem"/> to be used.</param>
    /// <returns>Returns an instance of <see cref="Bitmap"/> if successful, or return <see langword="null"/> if anything goes wrong.</returns>
    public static Bitmap? GetThumbnailImageAsBitmap(this ModItem item)
    {
        if (item.MetaData == null)
            return null;
        
        if (string.IsNullOrEmpty(item.MetaData.Thumbnail)) return null;

        // Try to get a cached Bitmap
        if (BitmapThumbnailCache.TryGetValue(item.MetaData.Thumbnail, out var image))
        {
            item.Thumbnail = image;
            return image;
        }

        // Otherwise, just load it and cache it
        try
        {
            image = new Bitmap(AssetLoader.Open(new Uri(item.MetaData.Thumbnail)));
            BitmapThumbnailCache.Add(item.MetaData.Thumbnail, image);
            item.Thumbnail = image;
            return image;
        }
        catch
        {
            return null;
        }
    }
}