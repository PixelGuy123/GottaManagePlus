using System.Collections.ObjectModel;
using System.Linq;
using GottaManagePlus.Models;
using GottaManagePlus.Models.SourceGenerators;
using GottaManagePlus.Models.UI;
using Tomlyn;

namespace GottaManagePlus.Utils;

/// <summary>
/// A reader specialized in retrieving data from a profile's metadata file.
/// </summary>
public static class ProfileMetadataUtils
{
    /// <summary>
    /// Attempts to read the metadata content (TOML format) and return back an instance of <see cref="ProfileMetadata"/>.
    /// </summary>
    /// <param name="tomlContent">The content used by the <see cref="TomlSerializer"/>.</param>
    /// <returns>An instance of <see cref="ProfileMetadata"/>.</returns>
    public static ProfileMetadata? ReadMetadata(string tomlContent) =>
        TomlSerializer.Deserialize<ProfileMetadata>(tomlContent, ProfileMetadataContext.Default);

    /// <summary>
    /// Serializes the <see cref="ProfileMetadata"/> into a TOML table.
    /// </summary>
    /// <param name="metadata">The instance to be serialized.</param>
    /// <returns>A <see cref="string"/> containing all the content of the metadata.</returns>
    public static string Serialize(this ProfileMetadata metadata) =>
        TomlSerializer.Serialize(metadata, ProfileMetadataContext.Default);
}