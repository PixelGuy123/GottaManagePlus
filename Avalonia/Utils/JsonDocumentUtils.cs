using System.Text.Json;

namespace GottaManagePlus.Utils;

public static class JsonDocumentUtils
{
    /// <summary>
    /// Gets a DateTime from a Unix timestamp stored in the <see cref="JsonElement"/>.
    /// </summary>
    /// <param name="rootElement">The <see cref="JsonElement"/> containing the Unix timestamp value.</param>
    /// <returns>A <see cref="DateTime"/> in UTC representing the converted Unix timestamp.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the <see cref="JsonElement"/> is not a number or cannot be converted to Int64.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the Unix timestamp is outside the valid range for <see cref="DateTimeOffset"/> (years 0001-9999).</exception>
    public static DateTime GetUnixDateTime(this JsonElement rootElement) =>
        DateTimeOffset.FromUnixTimeSeconds(rootElement.GetInt64()).UtcDateTime;
}