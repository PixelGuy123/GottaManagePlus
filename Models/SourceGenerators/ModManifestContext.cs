using System.Text.Json.Serialization;

namespace GottaManagePlus.Models.SourceGenerators;

[JsonSourceGenerationOptions(WriteIndented = true)]
[JsonSerializable(typeof(ModManifest))]
[JsonSerializable(typeof(ModMetadata))]
[JsonSerializable(typeof(DestinedAsset))]
internal partial class ModManifestContext : JsonSerializerContext;