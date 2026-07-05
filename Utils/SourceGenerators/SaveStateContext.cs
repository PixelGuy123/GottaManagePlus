using System.Text.Json.Serialization;
using GottaManagePlus.Models.UI;

namespace GottaManagePlus.Utils.SourceGenerators;

[JsonSourceGenerationOptions(WriteIndented = true)]
[JsonSerializable(typeof(SaveState))]
internal partial class SaveStateContext : JsonSerializerContext;