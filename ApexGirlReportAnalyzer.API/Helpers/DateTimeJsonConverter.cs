using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ApexGirlReportAnalyzer.API.Helpers;

/// <summary>
/// Custom JSON converter that serializes DateTime values as ISO 8601 strings in UTC format.
/// </summary>
public class DateTimeJsonConverter : JsonConverter<DateTime>
{
    /// <inheritdoc />
    public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return DateTime.Parse(reader.GetString()!, CultureInfo.InvariantCulture);
    }

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString("yyyy-MM-dd'T'HH:mm:ss'Z'", CultureInfo.InvariantCulture));
    }
}
