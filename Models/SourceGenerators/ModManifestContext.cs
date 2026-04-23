using System.Text.Json.Serialization;
using GottaManagePlus.Utils.Collections;

namespace GottaManagePlus.Models.SourceGenerators;

[JsonSourceGenerationOptions(WriteIndented = true)]
[JsonSerializable(typeof(ModManifest))]
[JsonSerializable(typeof(ModMetadata))]
[JsonSerializable(typeof(DestinedAsset))]
[JsonSerializable(typeof(AutoSortedList<WrappedGameVersion>))]
internal partial class ModManifestContext : JsonSerializerContext;