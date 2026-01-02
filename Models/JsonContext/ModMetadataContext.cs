using System.Text.Json.Serialization;

namespace GottaManagePlus.Models.JsonContext;

[JsonSourceGenerationOptions(WriteIndented = true)]
[JsonSerializable(typeof(ModMetadata))]
[JsonSerializable(typeof(ModMetadata.ModResourcesMetaData))]
internal partial class ModMetadataContext : JsonSerializerContext;