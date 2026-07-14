/*
This file is part of GottaManagePlus (https://github.com/PixelGuy123/GottaManagePlus)

    Copyright (C) 2026 PixelGuy123

    This program is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with this program.  If not, see <https://www.gnu.org/licenses/>.

*/

using System.Text.Json;
using System.Text.Json.Serialization;
using GottaManagePlus.Models;

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