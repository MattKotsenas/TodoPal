using System.Text.Json.Serialization;

namespace TodoPalExtension;

public sealed class TodoTask
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("title")]
    public string? Title { get; set; }

    [JsonPropertyName("status")]
    public string? Status { get; set; }

    [JsonPropertyName("importance")]
    public string? Importance { get; set; }

    [JsonPropertyName("isReminderOn")]
    public bool? IsReminderOn { get; set; }

    [JsonPropertyName("dueDateTime")]
    public DateTimeTimeZone? DueDateTime { get; set; }

    [JsonPropertyName("completedDateTime")]
    public DateTimeTimeZone? CompletedDateTime { get; set; }

    [JsonPropertyName("createdDateTime")]
    public string? CreatedDateTime { get; set; }

    [JsonPropertyName("lastModifiedDateTime")]
    public string? LastModifiedDateTime { get; set; }

    [JsonPropertyName("singleValueExtendedProperties")]
    public List<SingleValueExtendedProperty>? SingleValueExtendedProperties { get; set; }
}
