using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace TodoPalExtension;

public sealed class GraphTodoClient
{
    private static readonly JsonSerializerOptions s_readOptions = new() { PropertyNameCaseInsensitive = true };
    private static readonly JsonSerializerOptions s_writeOptions = new() { DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull };

    private const string BaseUrl = "https://graph.microsoft.com/v1.0";

    private readonly HttpClient _httpClient;
    private readonly Func<Task<string>> _getAccessToken;

    public GraphTodoClient(HttpClient httpClient, Func<Task<string>> getAccessToken)
    {
        _httpClient = httpClient;
        _getAccessToken = getAccessToken;
    }

    public async Task<List<TodoTaskList>> GetTaskListsAsync(CancellationToken cancellationToken = default)
    {
        using var request = await CreateRequest(HttpMethod.Get, $"{BaseUrl}/me/todo/lists", cancellationToken);
        using var response = await _httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        var collection = await JsonSerializer.DeserializeAsync<GraphCollection<TodoTaskList>>(
            await response.Content.ReadAsStreamAsync(cancellationToken), s_readOptions, cancellationToken);

        return collection?.Value ?? [];
    }

    public async Task<List<TodoTask>> GetTasksAsync(string listId, bool includeCompleted = false, CancellationToken cancellationToken = default)
    {
        var url = $"{BaseUrl}/me/todo/lists/{listId}/tasks";
        if (!includeCompleted)
        {
            url += "?$filter=status ne 'completed'";
        }

        using var request = await CreateRequest(HttpMethod.Get, url, cancellationToken);
        using var response = await _httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        var collection = await JsonSerializer.DeserializeAsync<GraphCollection<TodoTask>>(
            await response.Content.ReadAsStreamAsync(cancellationToken), s_readOptions, cancellationToken);

        return collection?.Value ?? [];
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
        request.Content = new StringContent(JsonSerializer.Serialize(body, s_writeOptions), Encoding.UTF8, "application/json");

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        return (await JsonSerializer.DeserializeAsync<TodoTask>(
            await response.Content.ReadAsStreamAsync(cancellationToken), s_readOptions, cancellationToken))!;
    }

    public Task CompleteTaskAsync(string listId, string taskId, CancellationToken cancellationToken = default)
        => UpdateTaskStatusAsync(listId, taskId, "completed", cancellationToken);

    public Task UncompleteTaskAsync(string listId, string taskId, CancellationToken cancellationToken = default)
        => UpdateTaskStatusAsync(listId, taskId, "notStarted", cancellationToken);

    private async Task UpdateTaskStatusAsync(string listId, string taskId, string status, CancellationToken cancellationToken)
    {
        using var request = await CreateRequest(HttpMethod.Patch, $"{BaseUrl}/me/todo/lists/{listId}/tasks/{taskId}", cancellationToken);
        request.Content = new StringContent(
            JsonSerializer.Serialize(new TodoTask { Status = status }, s_writeOptions),
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
}
