using Tomlyn.Serialization;

namespace GottaManagePlus.Models.SourceGenerators;

[TomlSerializable(typeof(ProfileMetadata))]
[TomlSerializable(typeof(ModManifest))]
[TomlSerializable(typeof(ModMetadata))]
[TomlSerializable(typeof(DestinedAsset))]
internal partial class ProfileMetadataContext : TomlSerializerContext;