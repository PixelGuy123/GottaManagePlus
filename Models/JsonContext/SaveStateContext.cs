using System.Text.Json.Serialization;

namespace GottaManagePlus.Models.JsonContext;

[JsonSourceGenerationOptions(WriteIndented = true)]
[JsonSerializable(typeof(SaveState))]
internal partial class SaveStateContext : JsonSerializerContext;