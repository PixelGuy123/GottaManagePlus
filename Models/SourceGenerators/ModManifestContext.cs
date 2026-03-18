using System.Text.Json.Serialization;

namespace GottaManagePlus.Models.SourceGenerators;

[JsonSourceGenerationOptions(WriteIndented = true)]
[JsonSerializable(typeof(ModManifest))]
[JsonSerializable(typeof(ModAsset))]
[JsonSerializable(typeof(DestinedAsset))]
internal partial class ModManifestContext : JsonSerializerContext;