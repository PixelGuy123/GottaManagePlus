using GottaManagePlus.Models.UI;
using Tomlyn.Serialization;

namespace GottaManagePlus.Models.SourceGenerators;

[TomlSerializable(typeof(ProfileMetadata))]
[TomlSerializable(typeof(ModManifest))]
[TomlSerializable(typeof(ItemWithPath))]
internal partial class ProfileMetadataContext : TomlSerializerContext;