using Tomlyn.Serialization;

namespace GottaManagePlus.Models.SourceGenerators;

[TomlSerializable(typeof(ProfileMetadata))]
[TomlSerializable(typeof(ModManifest))]
internal partial class ProfileMetadataContext : TomlSerializerContext;