using System.Diagnostics.CodeAnalysis;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace TodoPalExtension;

public sealed class GraphTodoClient
{
    private const string BaseUrl = "https://graph.microsoft.com/v1.0";

    private readonly HttpClient _httpClient;
    private readonly Func<Task<string>> _getAccessToken;
    private readonly JsonSerializerOptions _jsonOptions;

    // The DefaultJsonTypeInfoResolver fallback is only used by the test project (plain net9.0, no trimming).
    // The extension always passes TodoPalJsonContext.Default.Options which is source-generated.
    [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "Fallback resolver only used in tests, not in trimmed extension")]
    [UnconditionalSuppressMessage("AOT", "IL3050", Justification = "Fallback resolver only used in tests, not in trimmed extension")]
    public GraphTodoClient(HttpClient httpClient, Func<Task<string>> getAccessToken, JsonSerializerOptions? jsonOptions = null)
    {
        _httpClient = httpClient;
        _getAccessToken = getAccessToken;
        _jsonOptions = jsonOptions ?? new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            TypeInfoResolver = new System.Text.Json.Serialization.Metadata.DefaultJsonTypeInfoResolver()
        };
    }

    public async Task<List<TodoTaskList>> GetTaskListsAsync(CancellationToken cancellationToken = default)
    {
        return await GetAllPagesAsync($"{BaseUrl}/me/todo/lists", GetTypeInfo<GraphCollection<TodoTaskList>>(), cancellationToken);
    }

    public async Task<List<TodoTask>> GetTasksAsync(string listId, bool includeCompleted = false, CancellationToken cancellationToken = default)
    {
        var url = $"{BaseUrl}/me/todo/lists/{listId}/tasks";
        if (!includeCompleted)
        {
            url += "?$filter=status ne 'completed'";
        }

        return await GetAllPagesAsync(url, GetTypeInfo<GraphCollection<TodoTask>>(), cancellationToken);
    }

    public async Task<TodoTask> CreateTaskAsync(string listId, string title, DateOnly? dueDate = null, CancellationToken cancellationToken = default)
    {
        var body = new TodoTask { Title = title };

        if (dueDate is { } d)
        {
            body.DueDateTime = new DateTimeTimeZone
            {
                DateTime = $"{d:yyyy-MM-dd}T00:00:00.0000000",
                TimeZone = "UTC"
            };
        }

        using var request = await CreateRequest(HttpMethod.Post, $"{BaseUrl}/me/todo/lists/{listId}/tasks", cancellationToken);
        var taskTypeInfo = GetTypeInfo<TodoTask>();
        request.Content = new StringContent(JsonSerializer.Serialize(body, taskTypeInfo), Encoding.UTF8, "application/json");

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        return (await JsonSerializer.DeserializeAsync(
            await response.Content.ReadAsStreamAsync(cancellationToken), taskTypeInfo, cancellationToken))!;
    }

    public Task CompleteTaskAsync(string listId, string taskId, CancellationToken cancellationToken = default)
        => UpdateTaskStatusAsync(listId, taskId, "completed", cancellationToken);

    public Task UncompleteTaskAsync(string listId, string taskId, CancellationToken cancellationToken = default)
        => UpdateTaskStatusAsync(listId, taskId, "notStarted", cancellationToken);

    private async Task UpdateTaskStatusAsync(string listId, string taskId, string status, CancellationToken cancellationToken)
    {
        using var request = await CreateRequest(HttpMethod.Patch, $"{BaseUrl}/me/todo/lists/{listId}/tasks/{taskId}", cancellationToken);
        request.Content = new StringContent(
            JsonSerializer.Serialize(new TodoTask { Status = status }, GetTypeInfo<TodoTask>()),
            Encoding.UTF8, "application/json");

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    private async Task<HttpRequestMessage> CreateRequest(HttpMethod method, string url, CancellationToken cancellationToken)
    {
        var token = await _getAccessToken();
        var request = new HttpRequestMessage(method, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return request;
    }

    private async Task<List<T>> GetAllPagesAsync<T>(string url, JsonTypeInfo<GraphCollection<T>> typeInfo, CancellationToken cancellationToken)
    {
        var allItems = new List<T>();
        string? nextUrl = url;

        while (nextUrl is not null)
        {
            using var request = await CreateRequest(HttpMethod.Get, nextUrl, cancellationToken);
            using var response = await _httpClient.SendAsync(request, cancellationToken);
            response.EnsureSuccessStatusCode();

            var collection = await JsonSerializer.DeserializeAsync(
                await response.Content.ReadAsStreamAsync(cancellationToken), typeInfo, cancellationToken);

            if (collection is not null)
            {
                allItems.AddRange(collection.Value);
                nextUrl = collection.NextLink;
            }
            else
            {
                break;
            }
        }

        return allItems;
    }

    [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "Type info is resolved from source-generated context when available")]
    [UnconditionalSuppressMessage("AOT", "IL3050", Justification = "Type info is resolved from source-generated context when available")]
    private JsonTypeInfo<T> GetTypeInfo<T>()
    {
        return (JsonTypeInfo<T>)_jsonOptions.GetTypeInfo(typeof(T));
    }
}
