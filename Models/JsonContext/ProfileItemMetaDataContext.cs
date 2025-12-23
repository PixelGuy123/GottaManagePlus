using System.Text.Json.Serialization;

namespace GottaManagePlus.Models.JsonContext;

[JsonSourceGenerationOptions(WriteIndented = true)]
[JsonSerializable(typeof(ProfileItemMetaData))]
internal partial class ProfileItemMetaDataContext : JsonSerializerContext { }