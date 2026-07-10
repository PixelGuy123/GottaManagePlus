using Avalonia.Data.Converters;
using Avalonia.Media.Imaging;
using GottaManagePlus.Models;
using GottaManagePlus.Models.ModManagement;
using GottaManagePlus.Utils;

namespace GottaManagePlus.Styles.Converters;

public static class ModMetadataConverters
{
    /// <summary>
    /// Gets a Converter that turns a <see cref="ModMetadata"/> data into a stringified version.
    /// </summary>
    public static FuncValueConverter<ModMetadata, string> MetadataToStringifiedVersion { get; } = 
        new(metadata =>
        {
            if (metadata == null || metadata.SupportedPlusVersions.Count == 0)
                return "0.0.0";
            return metadata.SupportedPlusVersions[^1].ToString();
        });

    /// <summary>
    /// Gets a converter that attempts to load the thumbnail from a <see cref="ModMetadata"/>.
    /// </summary>
    public static FuncValueConverter<ModMetadata, Bitmap?> MetadataToThumbnail { get; } =
        new(metadata => metadata?.GetThumbnailImageAsBitmap());
}