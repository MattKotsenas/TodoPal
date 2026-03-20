using System.Text.Json;

namespace TodoPalExtension.Tests;

[TestClass]
public sealed class GraphModelsTests
{
    [TestMethod]
    public void Deserialize_TaskList_ParsesAllFields()
    {
        var json = """
        {
            "id": "AAMkADIyAAAAABrJAAA=",
            "displayName": "Tasks",
            "isOwner": true,
            "isShared": false,
            "wellknownListName": "defaultList"
        }
        """;

        var list = JsonSerializer.Deserialize<TodoTaskList>(json);

        Assert.IsNotNull(list);
        Assert.AreEqual("AAMkADIyAAAAABrJAAA=", list.Id);
        Assert.AreEqual("Tasks", list.DisplayName);
        Assert.AreEqual(true, list.IsOwner);
        Assert.AreEqual(false, list.IsShared);
        Assert.AreEqual("defaultList", list.WellknownListName);
    }

    [TestMethod]
    public void Deserialize_TaskListCollection_ParsesValueArray()
    {
        var json = """
        {
            "value": [
                {
                    "id": "list-1",
                    "displayName": "Tasks",
                    "isOwner": true,
                    "isShared": false,
                    "wellknownListName": "defaultList"
                },
                {
                    "id": "list-2",
                    "displayName": "Shopping",
                    "isOwner": true,
                    "isShared": true,
                    "wellknownListName": "none"
                }
            ]
        }
        """;

        var response = JsonSerializer.Deserialize<GraphCollection<TodoTaskList>>(json);

        Assert.IsNotNull(response);
        Assert.AreEqual(2, response.Value.Count);
        Assert.AreEqual("list-1", response.Value[0].Id);
        Assert.AreEqual("Shopping", response.Value[1].DisplayName);
    }

    [TestMethod]
    public void Deserialize_Task_ParsesRequiredFields()
    {
        var json = """
        {
            "id": "task-123",
            "title": "Buy groceries",
            "status": "notStarted",
            "importance": "normal",
            "isReminderOn": false,
            "createdDateTime": "2024-01-15T10:30:00Z",
            "lastModifiedDateTime": "2024-01-15T10:30:00Z"
        }
        """;

        var task = JsonSerializer.Deserialize<TodoTask>(json);

        Assert.IsNotNull(task);
        Assert.AreEqual("task-123", task.Id);
        Assert.AreEqual("Buy groceries", task.Title);
        Assert.AreEqual("notStarted", task.Status);
        Assert.AreEqual("normal", task.Importance);
    }

    [TestMethod]
    public void Deserialize_Task_ParsesDueDateTime()
    {
        var json = """
        {
            "id": "task-456",
            "title": "Submit report",
            "status": "inProgress",
            "importance": "high",
            "isReminderOn": true,
            "dueDateTime": {
                "dateTime": "2024-03-20T00:00:00.0000000",
                "timeZone": "UTC"
            },
            "createdDateTime": "2024-01-15T10:30:00Z",
            "lastModifiedDateTime": "2024-01-15T10:30:00Z"
        }
        """;

        var task = JsonSerializer.Deserialize<TodoTask>(json);

        Assert.IsNotNull(task);
        Assert.IsNotNull(task.DueDateTime);
        Assert.AreEqual("2024-03-20T00:00:00.0000000", task.DueDateTime.DateTime);
        Assert.AreEqual("UTC", task.DueDateTime.TimeZone);
    }

    [TestMethod]
    public void Deserialize_Task_NullDueDateTime_WhenAbsent()
    {
        var json = """
        {
            "id": "task-789",
            "title": "No due date task",
            "status": "notStarted",
            "importance": "low",
            "isReminderOn": false,
            "createdDateTime": "2024-01-15T10:30:00Z",
            "lastModifiedDateTime": "2024-01-15T10:30:00Z"
        }
        """;

        var task = JsonSerializer.Deserialize<TodoTask>(json);

        Assert.IsNotNull(task);
        Assert.IsNull(task.DueDateTime);
    }

    [TestMethod]
    public void Deserialize_Task_CompletedStatus()
    {
        var json = """
        {
            "id": "task-done",
            "title": "Completed task",
            "status": "completed",
            "importance": "normal",
            "isReminderOn": false,
            "completedDateTime": {
                "dateTime": "2024-03-19T14:00:00.0000000",
                "timeZone": "UTC"
            },
            "createdDateTime": "2024-01-15T10:30:00Z",
            "lastModifiedDateTime": "2024-03-19T14:00:00Z"
        }
        """;

        var task = JsonSerializer.Deserialize<TodoTask>(json);

        Assert.IsNotNull(task);
        Assert.AreEqual("completed", task.Status);
        Assert.IsNotNull(task.CompletedDateTime);
    }

    [TestMethod]
    public void Deserialize_TaskCollection_ParsesValueArray()
    {
        var json = """
        {
            "value": [
                {
                    "id": "task-1",
                    "title": "First",
                    "status": "notStarted",
                    "importance": "low",
                    "isReminderOn": false,
                    "createdDateTime": "2024-01-15T10:30:00Z",
                    "lastModifiedDateTime": "2024-01-15T10:30:00Z"
                },
                {
                    "id": "task-2",
                    "title": "Second",
                    "status": "completed",
                    "importance": "high",
                    "isReminderOn": true,
                    "createdDateTime": "2024-01-16T08:00:00Z",
                    "lastModifiedDateTime": "2024-01-16T08:00:00Z"
                }
            ]
        }
        """;

        var response = JsonSerializer.Deserialize<GraphCollection<TodoTask>>(json);

        Assert.IsNotNull(response);
        Assert.AreEqual(2, response.Value.Count);
        Assert.AreEqual("First", response.Value[0].Title);
        Assert.AreEqual("completed", response.Value[1].Status);
    }

    [TestMethod]
    public void Deserialize_EmptyTaskCollection_ReturnsEmptyList()
    {
        var json = """
        {
            "value": []
        }
        """;

        var response = JsonSerializer.Deserialize<GraphCollection<TodoTask>>(json);

        Assert.IsNotNull(response);
        Assert.AreEqual(0, response.Value.Count);
    }

    [TestMethod]
    public void Deserialize_Task_IgnoresUnknownProperties()
    {
        var json = """
        {
            "id": "task-extra",
            "title": "Extra fields task",
            "status": "notStarted",
            "importance": "normal",
            "isReminderOn": false,
            "createdDateTime": "2024-01-15T10:30:00Z",
            "lastModifiedDateTime": "2024-01-15T10:30:00Z",
            "body": { "content": "some body", "contentType": "text" },
            "recurrence": null,
            "hasAttachments": false,
            "@odata.etag": "W/\"xzyPKP0BiUGgld+lMKXwbQAAgdhkVw==\""
        }
        """;

        var task = JsonSerializer.Deserialize<TodoTask>(json);

        Assert.IsNotNull(task);
        Assert.AreEqual("task-extra", task.Id);
        Assert.AreEqual("Extra fields task", task.Title);
    }

    [TestMethod]
    public void Serialize_NewTask_ProducesCorrectJson()
    {
        var task = new TodoTask
        {
            Title = "New task from CmdPal",
            Importance = "high"
        };

        var json = JsonSerializer.Serialize(task);
        var doc = JsonDocument.Parse(json);

        Assert.AreEqual("New task from CmdPal", doc.RootElement.GetProperty("title").GetString());
        Assert.AreEqual("high", doc.RootElement.GetProperty("importance").GetString());
    }

    [TestMethod]
    public void Serialize_TaskStatusUpdate_ProducesCorrectJson()
    {
        var task = new TodoTask
        {
            Status = "completed"
        };

        var options = new JsonSerializerOptions { DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull };
        var json = JsonSerializer.Serialize(task, options);
        var doc = JsonDocument.Parse(json);

        Assert.AreEqual("completed", doc.RootElement.GetProperty("status").GetString());
        Assert.IsFalse(doc.RootElement.TryGetProperty("id", out _), "Null id should be omitted");
    }
}
