using System.Text.Json;
using System.Text.Json.Serialization;
using GottaManagePlus.Models.System;

namespace GottaManagePlus.Utils.JsonConverters;

public class OsPathJsonConverter : JsonConverter<OsPath>
{
    public override OsPath Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.String)
            throw new JsonException("Expected a string for OsPath");
        
        var path = reader.GetString() ?? "";
        return new OsPath(path);
    }

    public override void Write(Utf8JsonWriter writer, OsPath value, JsonSerializerOptions options) => 
        writer.WriteStringValue(value.NormalizedPath);
}