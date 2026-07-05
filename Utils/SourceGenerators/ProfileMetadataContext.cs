using GottaManagePlus.Models;
using Tomlyn.Serialization;

namespace GottaManagePlus.Utils.SourceGenerators;

[TomlSerializable(typeof(ProfileMetadata))]
[TomlSerializable(typeof(ModManifest))]
[TomlSerializable(typeof(ModMetadata))]
[TomlSerializable(typeof(DestinedAsset))]
internal partial class ProfileMetadataContext : TomlSerializerContext;