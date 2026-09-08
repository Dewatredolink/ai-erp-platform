using System.Text.Json;
using System.Text.Json.Serialization;

namespace ErpApi.Utilities
{
    /// <summary>
    /// Custom JSON converter that ensures all DateTime values have Kind=Utc.
    /// Fixes PostgreSQL timestamp with time zone compatibility.
    /// </summary>
    public class UtcDateTimeConverter : JsonConverter<DateTime>
    {
        public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var value = reader.GetDateTime();
            // Force Kind to Utc, even if deserialized as Unspecified
            return DateTime.SpecifyKind(value, DateTimeKind.Utc);
        }

        public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
        {
            // Ensure we're writing UTC
            var utcValue = value.Kind == DateTimeKind.Utc ? value : value.ToUniversalTime();
            writer.WriteStringValue(utcValue);
        }
    }
}
