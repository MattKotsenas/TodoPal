using System.Text.Json;
using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;

namespace TodoPalExtension;

internal sealed partial class TodoPalExtensionPage : ListPage
{
    private readonly GraphAuthService _authService = new();
    private GraphTodoClient? _client;
    private IListItem[] _items = [];

    public TodoPalExtensionPage()
    {
        Icon = IconHelpers.FromRelativePath("Assets\\StoreLogo.png");
        Title = "Microsoft To Do";
        Name = "Open";
    }

    public override IListItem[] GetItems()
    {
        _ = LoadItemsAsync();
        return _items;
    }

    private async Task LoadItemsAsync()
    {
        IsLoading = true;
        try
        {
            _client ??= await CreateClientAsync();

            var lists = await _client.GetTaskListsAsync();
            var items = new List<IListItem>();

            // "Add Task" command at the top
            items.Add(new ListItem(new AddTaskCommand(_client, lists, this))
            {
                Title = "Add a task",
                Subtitle = "Create a new task in Microsoft To Do",
                Icon = new IconInfo("\uE710", "Segoe Fluent Icons") // + icon
            });

            foreach (var list in lists)
            {
                if (list.Id is null) continue;

                var tasks = await _client.GetTasksAsync(list.Id);
                foreach (var task in tasks)
                {
                    if (task.Id is null || task.Title is null) continue;

                    var subtitle = FormatSubtitle(task, list);
                    var command = new ToggleCompleteCommand(_client, list.Id, task, this);

                    var item = new ListItem(command)
                    {
                        Title = task.Title,
                        Subtitle = subtitle,
                        Section = list.DisplayName ?? "Tasks",
                        Tags = GetTags(task)
                    };

                    items.Add(item);
                }
            }

            _items = [.. items];
        }
        catch (HttpRequestException)
        {
            _items = [new ListItem(new SignInCommand(_authService, this))
            {
                Title = "Sign in to Microsoft To Do",
                Subtitle = "Connect your Microsoft account to view tasks"
            }];
        }
        finally
        {
            IsLoading = false;
            RaiseItemsChanged(_items.Length);
        }
    }

    private async Task<GraphTodoClient> CreateClientAsync()
    {
        var token = await _authService.GetAccessTokenAsync();
        return new GraphTodoClient(new HttpClient(), () => _authService.GetAccessTokenAsync());
    }

    internal void Refresh()
    {
        _items = [];
        RaiseItemsChanged(0);
    }

    private static string FormatSubtitle(TodoTask task, TodoTaskList list)
    {
        var parts = new List<string>();

        if (task.DueDateTime?.DateTime is { } due && DateTime.TryParse(due, out var dueDate))
        {
            parts.Add($"Due {dueDate:MMM d}");
        }

        if (task.Status == "completed")
        {
            parts.Add("✓ Done");
        }

        return string.Join(" · ", parts);
    }

    private static ITag[] GetTags(TodoTask task)
    {
        var tags = new List<ITag>();

        if (task.Importance == "high")
        {
            tags.Add(new Tag("!") { ToolTip = "High importance" });
        }

        return [.. tags];
    }
}

internal sealed partial class ToggleCompleteCommand : InvokableCommand
{
    private readonly GraphTodoClient _client;
    private readonly string _listId;
    private readonly TodoTask _task;
    private readonly TodoPalExtensionPage _page;

    public ToggleCompleteCommand(GraphTodoClient client, string listId, TodoTask task, TodoPalExtensionPage page)
    {
        _client = client;
        _listId = listId;
        _task = task;
        _page = page;
        Name = task.Status == "completed" ? "Mark incomplete" : "Mark complete";
    }

    public override ICommandResult Invoke()
    {
        _ = ToggleAsync();
        return CommandResult.KeepOpen();
    }

    private async Task ToggleAsync()
    {
        if (_task.Id is null) return;

        if (_task.Status == "completed")
        {
            await _client.UncompleteTaskAsync(_listId, _task.Id);
        }
        else
        {
            await _client.CompleteTaskAsync(_listId, _task.Id);
        }

        _page.Refresh();
    }
}

internal sealed partial class SignInCommand : InvokableCommand
{
    private readonly GraphAuthService _authService;
    private readonly TodoPalExtensionPage _page;

    public SignInCommand(GraphAuthService authService, TodoPalExtensionPage page)
    {
        _authService = authService;
        _page = page;
        Name = "Sign in";
    }

    public override ICommandResult Invoke()
    {
        _ = SignInAsync();
        return CommandResult.KeepOpen();
    }

    private async Task SignInAsync()
    {
        await _authService.GetAccessTokenAsync();
        _page.Refresh();
    }
}
