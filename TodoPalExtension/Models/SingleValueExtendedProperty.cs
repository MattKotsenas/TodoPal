using System.Text.Json.Serialization;

namespace TodoPalExtension;

public sealed class SingleValueExtendedProperty
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("value")]
    public string? Value { get; set; }
}
