using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using GottaManagePlus.Models;
using GottaManagePlus.Models.SourceGenerators;
using GottaManagePlus.Models.UI;
using GottaManagePlus.Services.GameEnvironmentServices;
using Tomlyn;

namespace GottaManagePlus.Utils;

/// <summary>
/// A class with utilities for interacting with <see cref="ProfileMetadata"/>.
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
    /// <summary>
    /// Returns the physical path of the <see cref="ProfileMetadata"/>.
    /// </summary>
    /// <param name="metadata">The metadata to be searched for.</param>
    /// <param name="controller">The controller to indicate the correct location.</param>
    /// <returns>A <see cref="string"/> of the correct location of the <see cref="ProfileMetadata"/>.</returns>
    public static string GetPhysicalPath(this ProfileMetadata metadata, GameEnvironmentController controller) => 
        controller.SearchAbsolutePath(controller.GetOrCreateProfilesFolderPath(), metadata.Name);
}