using System.Text.Json;
using System.Text.Json.Serialization;
using GottaManagePlus.Models;
using GottaManagePlus.Models.GameEnvironments;

namespace GottaManagePlus.Utils.JsonConverters;

public class WrappedGameVersionJsonConverter : JsonConverter<WrappedGameVersion>
{
    public override WrappedGameVersion Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.String)
            throw new JsonException("Expected a string for WrappedGameVersion");
        
        var path = reader.GetString() ?? "";
        return new WrappedGameVersion(path);
    }

    public override void Write(Utf8JsonWriter writer, WrappedGameVersion value, JsonSerializerOptions options) =>
        writer.WriteStringValue(value.ToString());
}