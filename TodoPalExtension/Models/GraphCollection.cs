using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace TodoPalExtension;

[SuppressMessage("Naming", "CA1711:Identifiers should not have incorrect suffix", Justification = "Named after Graph API response shape")]
public sealed class GraphCollection<T>
{
    [JsonPropertyName("value")]
    public List<T> Value { get; set; } = [];

    [JsonPropertyName("@odata.nextLink")]
    public string? NextLink { get; set; }
}
