using System;
using System.IO;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;
using Avalonia.Platform;

namespace GottaManagePlus.Modules.AvaloniaUtils;

/// <summary>
/// Helper class that provides methods for loading Assembly Resources as images.
/// </summary>
public static class ImageHelper
{
    public static Bitmap? LoadAsBitmap(string assemblyPath)
    {
        using Stream? stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(assemblyPath);
        if (stream is not null)
            return new(stream);
        return null;
    }
}