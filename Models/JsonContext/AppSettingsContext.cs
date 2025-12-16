using System.Text.Json.Serialization;

namespace GottaManagePlus.Models.JsonContext;

[JsonSourceGenerationOptions(WriteIndented = true)]
[JsonSerializable(typeof(AppSettings))]
internal partial class AppSettingsContext : JsonSerializerContext { }