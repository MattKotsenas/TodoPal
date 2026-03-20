using System.Text.Json.Serialization;

namespace TodoPalExtension;

public sealed class DateTimeTimeZone
{
    [JsonPropertyName("dateTime")]
    public string? DateTime { get; set; }

    [JsonPropertyName("timeZone")]
    public string? TimeZone { get; set; }
}
