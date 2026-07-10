using GottaManagePlus.Models;
using GottaManagePlus.Models.GameEnvironments;
using GottaManagePlus.Models.ModManagement;
using Tomlyn.Serialization;

namespace GottaManagePlus.Utils.SourceGenerators;

[TomlSerializable(typeof(ProfileMetadata))]
[TomlSerializable(typeof(ModManifest))]
[TomlSerializable(typeof(ModMetadata))]
[TomlSerializable(typeof(DestinedAsset))]
internal partial class ProfileMetadataContext : TomlSerializerContext;