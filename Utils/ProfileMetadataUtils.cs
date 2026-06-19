using GottaManagePlus.Models;
using GottaManagePlus.Models.SourceGenerators;
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
        TomlSerializer.Deserialize(tomlContent, ProfileMetadataContext.Default.ProfileMetadata);

    /// <param name="metadata">The instance to be serialized.</param>
    extension(ProfileMetadata metadata)
    {
        /// <summary>
        /// Serializes the <see cref="ProfileMetadata"/> into a TOML table.
        /// </summary>
        /// <returns>A <see cref="string"/> containing all the content of the metadata.</returns>
        public string Serialize() =>
            TomlSerializer.Serialize(metadata, ProfileMetadataContext.Default.ProfileMetadata);

        /// <summary>
        /// Returns the physical path of the <see cref="ProfileMetadata"/>.
        /// </summary>
        /// <param name="controller">The controller to indicate the correct location.</param>
        /// <returns>A <see cref="string"/> of the correct location of the <see cref="ProfileMetadata"/>.</returns>
        public string GetPhysicalPath(GameEnvironmentController controller) => 
            controller.SearchAbsolutePath(controller.GetOrCreateProfilesFolderPath(), metadata.Name);
    }
}