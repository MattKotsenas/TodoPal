using System.Net;
using System.Text.Json;
using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;
using Microsoft.Identity.Client;

namespace TodoPalExtension;

internal sealed partial class TodoPalExtensionPage : ListPage
{
    private readonly GraphAuthService _authService = new();
    private GraphTodoClient? _client;
    private IListItem[] _items = [];

    public TodoPalExtensionPage()
    {
        Icon = IconHelpers.FromRelativePath("Assets\\StoreLogo.png");
        Title = "TodoPal";
        Name = "Open";
    }

    private bool _isLoaded;

    public override IListItem[] GetItems()
    {
        if (!_isLoaded)
        {
            _isLoaded = true;
            _ = LoadItemsAsync();
        }
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

            // "Add Task" command at the top - wrap the ContentPage directly
            // so CmdPal navigates to it natively (no GoToPage resolution needed)
            items.Add(new ListItem(new AddTaskPage(_client, lists, this))
            {
                Title = "Add a task",
                Subtitle = "Create a new task in Microsoft To Do",
                Icon = new IconInfo(new FontIconData("\uE710", "Segoe Fluent Icons")) // + icon
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

                    var section = IsDueToday(task)
                        ? "Due Today"
                        : list.DisplayName ?? "Tasks";

                    var item = new ListItem(command)
                    {
                        Title = task.Title,
                        Subtitle = subtitle,
                        Section = section,
                        Tags = GetTags(task)
                    };

                    items.Add(item);
                }
            }

            // Sort so "Due Today" appears first
            _items = [.. items.OrderBy(i => ((ListItem)i).Section == "Due Today" ? 0 : 1)];
        }
        catch (MsalUiRequiredException)
        {
            _items = [new ListItem(new SignInCommand(_authService, this))
            {
                Title = "Sign in to Microsoft To Do",
                Subtitle = "Connect your Microsoft account to view tasks"
            }];
        }
        catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.Unauthorized)
        {
            // Token expired or revoked - need to re-authenticate
            _client = null;
            _items = [new ListItem(new SignInCommand(_authService, this))
            {
                Title = "Sign in to Microsoft To Do",
                Subtitle = "Session expired - please sign in again"
            }];
        }
        catch (OperationCanceledException)
        {
            // Normal cancellation, don't show an error
        }
        catch (Exception ex)
        {
            ShowError($"Failed to load tasks: {ex.Message}");
            return;
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
        _isLoaded = false;
        _items = [];
        RaiseItemsChanged(0);
    }

    internal void ShowError(string message)
    {
        _items = [new ListItem(new NoOpCommand())
        {
            Title = "Error",
            Subtitle = message,
            Icon = new IconInfo(new FontIconData("\uE783", "Segoe Fluent Icons")) // warning icon
        }];
        RaiseItemsChanged(_items.Length);
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

    private static bool IsDueToday(TodoTask task)
    {
        return task.DueDateTime?.DateTime is { } due
            && DateTime.TryParse(due, out var dueDate)
            && dueDate.Date == DateTime.Today;
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
        try
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
        catch (Exception ex)
        {
            _page.ShowError($"Failed to update task: {ex.Message}");
        }
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
        try
        {
            await _authService.GetAccessTokenAsync();
            _page.Refresh();
        }
        catch (MsalClientException ex) when (ex.ErrorCode == "authentication_canceled")
        {
            // User cancelled the auth dialog, nothing to do
        }
        catch (Exception ex)
        {
            _page.ShowError($"Sign-in failed: {ex.Message}");
        }
    }
}
