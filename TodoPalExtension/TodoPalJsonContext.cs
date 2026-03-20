using System.Text.Json.Serialization;

namespace TodoPalExtension;

[JsonSerializable(typeof(GraphCollection<TodoTask>))]
[JsonSerializable(typeof(GraphCollection<TodoTaskList>))]
[JsonSerializable(typeof(TodoTask))]
[JsonSerializable(typeof(TodoTaskList))]
[JsonSerializable(typeof(DateTimeTimeZone))]
[JsonSourceGenerationOptions(
    PropertyNameCaseInsensitive = true,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
internal sealed partial class TodoPalJsonContext : JsonSerializerContext;
