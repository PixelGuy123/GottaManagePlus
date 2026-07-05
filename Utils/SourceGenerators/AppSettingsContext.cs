using System.Text.Json.Serialization;
using GottaManagePlus.Models;

namespace GottaManagePlus.Utils.SourceGenerators;

[JsonSourceGenerationOptions(WriteIndented = true)]
[JsonSerializable(typeof(AppSettings))]
internal partial class AppSettingsContext : JsonSerializerContext;