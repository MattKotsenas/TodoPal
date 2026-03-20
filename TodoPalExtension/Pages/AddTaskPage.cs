using System.Text.Json;
using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;

namespace TodoPalExtension;

internal sealed partial class AddTaskCommand : InvokableCommand
{
    private readonly GraphTodoClient _client;
    private readonly List<TodoTaskList> _lists;
    private readonly TodoPalExtensionPage _page;

    public AddTaskCommand(GraphTodoClient client, List<TodoTaskList> lists, TodoPalExtensionPage page)
    {
        _client = client;
        _lists = lists;
        _page = page;
        Name = "Add a task";
    }

    public override ICommandResult Invoke()
    {
        var addTaskPage = new AddTaskPage(_client, _lists, _page);
        return CommandResult.GoToPage(new GoToPageArgs() { PageId = addTaskPage.Id });
    }
}

internal sealed partial class AddTaskPage : ContentPage
{
    private readonly GraphTodoClient _client;
    private readonly List<TodoTaskList> _lists;
    private readonly TodoPalExtensionPage _parentPage;
    private readonly FormContent _form;

    public AddTaskPage(GraphTodoClient client, List<TodoTaskList> lists, TodoPalExtensionPage parentPage)
    {
        _client = client;
        _lists = lists;
        _parentPage = parentPage;

        Icon = new IconInfo(new FontIconData("\uE710", "Segoe Fluent Icons"));
        Title = "Add a task";
        Name = "Add Task";

        _form = new AddTaskFormContent(client, lists, parentPage);
    }

    public override IContent[] GetContent() => [_form];
}

internal sealed partial class AddTaskFormContent : FormContent
{
    private readonly GraphTodoClient _client;
    private readonly List<TodoTaskList> _lists;
    private readonly TodoPalExtensionPage _parentPage;

    public AddTaskFormContent(GraphTodoClient client, List<TodoTaskList> lists, TodoPalExtensionPage parentPage)
    {
        _client = client;
        _lists = lists;
        _parentPage = parentPage;

        TemplateJson = BuildTemplate();
        DataJson = "{}";
    }

    public override ICommandResult SubmitForm(string inputs, string data)
    {
        _ = CreateTaskAsync(inputs);
        return CommandResult.GoBack();
    }

    private async Task CreateTaskAsync(string inputsJson)
    {
        using var doc = JsonDocument.Parse(inputsJson);
        var root = doc.RootElement;

        var title = root.GetProperty("title").GetString();
        if (string.IsNullOrWhiteSpace(title)) return;

        DateOnly? dueDate = null;
        if (root.TryGetProperty("dueDate", out var dueDateEl))
        {
            var dueDateStr = dueDateEl.GetString();
            if (!string.IsNullOrEmpty(dueDateStr) && DateOnly.TryParse(dueDateStr, out var parsed))
            {
                dueDate = parsed;
            }
        }

        // Find the default list (or first list)
        var targetList = _lists.FirstOrDefault(l => l.WellknownListName == "defaultList") ?? _lists.FirstOrDefault();
        if (targetList?.Id is null) return;

        await _client.CreateTaskAsync(targetList.Id, title, dueDate);
        _parentPage.Refresh();
    }

    private static string BuildTemplate()
    {
        return """
        {
            "type": "AdaptiveCard",
            "$schema": "http://adaptivecards.io/schemas/adaptive-card.json",
            "version": "1.5",
            "body": [
                {
                    "type": "Input.Text",
                    "id": "title",
                    "label": "Task title",
                    "placeholder": "What needs to be done?",
                    "isRequired": true,
                    "errorMessage": "Title is required"
                },
                {
                    "type": "Input.Date",
                    "id": "dueDate",
                    "label": "Due date (optional)"
                }
            ],
            "actions": [
                {
                    "type": "Action.Submit",
                    "title": "Add task",
                    "style": "positive"
                }
            ]
        }
        """;
    }
}
