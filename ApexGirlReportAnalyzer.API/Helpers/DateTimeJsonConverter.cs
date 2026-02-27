using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ApexGirlReportAnalyzer.API.Helpers;

public class DateTimeJsonConverter : JsonConverter<DateTime>
{
    public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return DateTime.Parse(reader.GetString()!, CultureInfo.InvariantCulture);
    }

    public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString("yyyy-MM-dd'T'HH:mm:ss'z'", CultureInfo.InvariantCulture));
    }
}
