using System.IO;
using GottaManagePlus.Models;

namespace GottaManagePlus.Utils;

public static class ModMetadataUtils
{
    public static string? GetPluginDirectory(this ModManifest metadata) =>
        Path.GetDirectoryName(metadata.MetadataPath);
}