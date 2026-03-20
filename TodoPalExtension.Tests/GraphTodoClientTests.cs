using System.Net;
using System.Text.Json;

namespace TodoPalExtension.Tests;

[TestClass]
public sealed class GraphTodoClientTests
{
    private static readonly JsonSerializerOptions s_options = new() { PropertyNameCaseInsensitive = true };

    [TestMethod]
    public async Task GetTaskListsAsync_ReturnsLists()
    {
        var json = """
        {
            "value": [
                { "id": "list-1", "displayName": "Tasks", "wellknownListName": "defaultList" },
                { "id": "list-2", "displayName": "Shopping" }
            ]
        }
        """;

        var handler = new FakeHttpHandler(json);
        var client = CreateClient(handler);

        var lists = await client.GetTaskListsAsync();

        Assert.HasCount(2, lists);
        Assert.AreEqual("Tasks", lists[0].DisplayName);
        Assert.AreEqual("Shopping", lists[1].DisplayName);
    }

    [TestMethod]
    public async Task GetTaskListsAsync_SendsCorrectRequest()
    {
        var handler = new FakeHttpHandler("""{ "value": [] }""");
        var client = CreateClient(handler);

        await client.GetTaskListsAsync();

        Assert.AreEqual(HttpMethod.Get, handler.LastRequest!.Method);
        Assert.AreEqual("https://graph.microsoft.com/v1.0/me/todo/lists", handler.LastRequest.RequestUri!.ToString());
        Assert.AreEqual("Bearer fake-token", handler.LastRequest.Headers.Authorization!.ToString());
    }

    [TestMethod]
    public async Task GetTasksAsync_ReturnsTasks()
    {
        var json = """
        {
            "value": [
                { "id": "task-1", "title": "Buy milk", "status": "notStarted" },
                { "id": "task-2", "title": "Walk dog", "status": "completed" }
            ]
        }
        """;

        var handler = new FakeHttpHandler(json);
        var client = CreateClient(handler);

        var tasks = await client.GetTasksAsync("list-1");

        Assert.HasCount(2, tasks);
        Assert.AreEqual("Buy milk", tasks[0].Title);
        Assert.AreEqual("completed", tasks[1].Status);
    }

    [TestMethod]
    public async Task GetTasksAsync_SendsCorrectRequest()
    {
        var handler = new FakeHttpHandler("""{ "value": [] }""");
        var client = CreateClient(handler);

        await client.GetTasksAsync("list-abc");

        Assert.AreEqual("https://graph.microsoft.com/v1.0/me/todo/lists/list-abc/tasks", handler.LastRequest!.RequestUri!.ToString());
    }

    [TestMethod]
    public async Task CreateTaskAsync_SendsCorrectBody()
    {
        var responseJson = """{ "id": "new-task-1", "title": "Test task", "status": "notStarted" }""";
        var handler = new FakeHttpHandler(responseJson, HttpStatusCode.Created);
        var client = CreateClient(handler);

        var task = await client.CreateTaskAsync("list-1", "Test task");

        Assert.AreEqual(HttpMethod.Post, handler.LastRequest!.Method);
        Assert.AreEqual("https://graph.microsoft.com/v1.0/me/todo/lists/list-1/tasks", handler.LastRequest.RequestUri!.ToString());

        var body = JsonSerializer.Deserialize<JsonElement>(handler.LastRequestBody!);
        Assert.AreEqual("Test task", body.GetProperty("title").GetString());
    }

    [TestMethod]
    public async Task CreateTaskAsync_WithDueDate_IncludesDueDateTime()
    {
        var responseJson = """{ "id": "new-task-1", "title": "Test task", "status": "notStarted" }""";
        var handler = new FakeHttpHandler(responseJson, HttpStatusCode.Created);
        var client = CreateClient(handler);

        var dueDate = new DateOnly(2025, 3, 20);
        var task = await client.CreateTaskAsync("list-1", "Test task", dueDate);

        var body = JsonSerializer.Deserialize<JsonElement>(handler.LastRequestBody!);
        var dueDateTime = body.GetProperty("dueDateTime");
        Assert.AreEqual("2025-03-20T00:00:00.0000000", dueDateTime.GetProperty("dateTime").GetString());
        Assert.AreEqual("UTC", dueDateTime.GetProperty("timeZone").GetString());
    }

    [TestMethod]
    public async Task CreateTaskAsync_ReturnsCreatedTask()
    {
        var responseJson = """{ "id": "new-task-1", "title": "Test task", "status": "notStarted" }""";
        var handler = new FakeHttpHandler(responseJson, HttpStatusCode.Created);
        var client = CreateClient(handler);

        var task = await client.CreateTaskAsync("list-1", "Test task");

        Assert.AreEqual("new-task-1", task.Id);
        Assert.AreEqual("Test task", task.Title);
    }

    [TestMethod]
    public async Task CompleteTaskAsync_SendsPatchWithCompletedStatus()
    {
        var responseJson = """{ "id": "task-1", "title": "Buy milk", "status": "completed" }""";
        var handler = new FakeHttpHandler(responseJson);
        var client = CreateClient(handler);

        await client.CompleteTaskAsync("list-1", "task-1");

        Assert.AreEqual(HttpMethod.Patch, handler.LastRequest!.Method);
        Assert.AreEqual("https://graph.microsoft.com/v1.0/me/todo/lists/list-1/tasks/task-1", handler.LastRequest.RequestUri!.ToString());

        var body = JsonSerializer.Deserialize<JsonElement>(handler.LastRequestBody!);
        Assert.AreEqual("completed", body.GetProperty("status").GetString());
    }

    [TestMethod]
    public async Task UncompleteTaskAsync_SendsPatchWithNotStartedStatus()
    {
        var responseJson = """{ "id": "task-1", "title": "Buy milk", "status": "notStarted" }""";
        var handler = new FakeHttpHandler(responseJson);
        var client = CreateClient(handler);

        await client.UncompleteTaskAsync("list-1", "task-1");

        var body = JsonSerializer.Deserialize<JsonElement>(handler.LastRequestBody!);
        Assert.AreEqual("notStarted", body.GetProperty("status").GetString());
    }

    [TestMethod]
    public async Task GetTasksAsync_EmptyList_ReturnsEmptyCollection()
    {
        var handler = new FakeHttpHandler("""{ "value": [] }""");
        var client = CreateClient(handler);

        var tasks = await client.GetTasksAsync("list-1");

        Assert.IsEmpty(tasks);
    }

    [TestMethod]
    public async Task GetTaskListsAsync_HttpError_ThrowsHttpRequestException()
    {
        var handler = new FakeHttpHandler("", HttpStatusCode.Unauthorized);
        var client = CreateClient(handler);

        await Assert.ThrowsExactlyAsync<HttpRequestException>(() => client.GetTaskListsAsync());
    }

    [TestMethod]
    public async Task GetTasksAsync_HttpError_ThrowsHttpRequestException()
    {
        var handler = new FakeHttpHandler("", HttpStatusCode.InternalServerError);
        var client = CreateClient(handler);

        await Assert.ThrowsExactlyAsync<HttpRequestException>(() => client.GetTasksAsync("list-1"));
    }

    [TestMethod]
    public async Task CompleteTaskAsync_HttpError_ThrowsHttpRequestException()
    {
        var handler = new FakeHttpHandler("", HttpStatusCode.NotFound);
        var client = CreateClient(handler);

        await Assert.ThrowsExactlyAsync<HttpRequestException>(() => client.CompleteTaskAsync("list-1", "task-1"));
    }

    [TestMethod]
    public async Task CreateTaskAsync_HttpError_ThrowsHttpRequestException()
    {
        var handler = new FakeHttpHandler("", HttpStatusCode.Forbidden);
        var client = CreateClient(handler);

        await Assert.ThrowsExactlyAsync<HttpRequestException>(() => client.CreateTaskAsync("list-1", "Test"));
    }

    [TestMethod]
    public async Task GetTaskListsAsync_Unauthorized_IncludesStatusCode()
    {
        var handler = new FakeHttpHandler("", HttpStatusCode.Unauthorized);
        var client = CreateClient(handler);

        var ex = await Assert.ThrowsExactlyAsync<HttpRequestException>(() => client.GetTaskListsAsync());
        Assert.AreEqual(HttpStatusCode.Unauthorized, ex.StatusCode);
    }

    private static GraphTodoClient CreateClient(FakeHttpHandler handler)
    {
        var httpClient = new HttpClient(handler);
        return new GraphTodoClient(httpClient, () => Task.FromResult("fake-token"));
    }
}

/// <summary>
/// Captures HTTP requests and returns canned responses for testing.
/// </summary>
internal sealed class FakeHttpHandler : HttpMessageHandler
{
    private readonly string _responseBody;
    private readonly HttpStatusCode _statusCode;

    public HttpRequestMessage? LastRequest { get; private set; }
    public string? LastRequestBody { get; private set; }

    public FakeHttpHandler(string responseBody, HttpStatusCode statusCode = HttpStatusCode.OK)
    {
        _responseBody = responseBody;
        _statusCode = statusCode;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        LastRequest = request;
        if (request.Content is not null)
        {
            LastRequestBody = await request.Content.ReadAsStringAsync(cancellationToken);
        }

        return new HttpResponseMessage(_statusCode)
        {
            Content = new StringContent(_responseBody, System.Text.Encoding.UTF8, "application/json")
        };
    }
}
