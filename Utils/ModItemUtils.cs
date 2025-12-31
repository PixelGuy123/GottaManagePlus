using System;
using System.Collections.Generic;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using GottaManagePlus.Models;

namespace GottaManagePlus.Utils;

public static class ModItemUtils
{
    // Cache for thumbnail images
    private static readonly Dictionary<string, Bitmap> BitmapThumbnailCache = [];

    public static Bitmap? GetThumbnailImageAsBitmap(this ModItem item)
    {
        if (string.IsNullOrEmpty(item.ThumbnailFullPath)) return null;

        // Try to get a cached Bitmap
        if (BitmapThumbnailCache.TryGetValue(item.ThumbnailFullPath, out var image))
            return image;

        // Otherwise, just load it and cache it
        image = new Bitmap(AssetLoader.Open(new Uri(item.ThumbnailFullPath)));
        BitmapThumbnailCache.Add(item.ThumbnailFullPath, image);
        return image;
    }
}