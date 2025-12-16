using System.Text.Json.Serialization;

namespace GottaManagePlus.Models.JsonContext;

[JsonSourceGenerationOptions(WriteIndented = true)]
[JsonSerializable(typeof(AppSettings))]
[JsonSerializable(typeof(AppSettingsWrapper))]
internal partial class AppSettingsContext : JsonSerializerContext { }