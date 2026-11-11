using System.Text.Json.Nodes;
using AxKit.Core;
using AxKit.Core.Mcp;
using AxTask.Core;

namespace AxTask;

/// <summary>
/// axTask's AI tools (docs/axKit/mcp.md MC-8, SPEC.md TK-8, 2026-11-11): <c>axkit_task_lists</c>, <c>axkit_task_items</c>
/// and <c>axkit_task_add</c>, answered on the window's thread after the window base's refusals
/// (<c>AxKitForm.McpApp</c>). Only this window's top folder and the projects it found are reached (MC-D12); an item is
/// added through <see cref="Commit"/> as New item adds one, so that Ctrl+Z takes it back and the window says who added it.
/// </summary>
public sealed partial class TaskForm
{
    protected override object? McpAppRequest(string tool, JsonObject arguments, string client) => tool switch
    {
        McpTools.TaskLists => McpLists(),
        McpTools.TaskItems => McpItems(arguments),
        McpTools.TaskAdd => McpAdd(arguments, client),
        _ => null,
    };

    /// <summary>The lists the tools reach: the user's own top folder, never a project shown as the top, and the projects found so far.</summary>
    private IReadOnlyList<ReachableList> McpReachable() => TaskTools.Lists(_settings.TopFolder, _projects ?? []);

    /// <summary>TK-8.1: every list with its counts; a note while the projects are still being looked for.</summary>
    private object McpLists()
    {
        var lists = McpReachable().Select(list =>
        {
            var items = ItemFolder.Read(list.Folder).Items;
            return new
            {
                name = list.Name,
                place = list.Place,
                items = items.Count,
                notDone = items.Count(item => TaskRules.IsTask(item.Document) && !TaskRules.IsDone(item.Document)),
            };
        }).ToArray();
        return new
        {
            lists,
            projects = _projects is null && _settings.ProjectFolders.Count > 0 ? "The window is still looking for the lists of its project folders: ask again in a moment for theirs." : null,
        };
    }

    /// <summary>TK-8.1: one list's items with their fields; done ones only when asked for.</summary>
    private object McpItems(JsonObject arguments)
    {
        var (list, refused) = TaskTools.Find(McpReachable(), McpProtocol.Text(arguments["list"]));
        if (list is null)
        {
            return new { error = refused };
        }

        var withDone = arguments["done"] is JsonValue flag && ((flag.TryGetValue<bool>(out var on) && on) || (flag.TryGetValue<string>(out var text) && text.Equals("true", StringComparison.OrdinalIgnoreCase)));
        var items = ItemFolder.Read(list.Folder).Items;
        var shown = items.Where(item => withDone || !TaskRules.IsDone(item.Document)).ToList();
        return new
        {
            list = list.Name,
            items = new JsonArray([.. shown.Select(item => (JsonNode)TaskTools.ItemOf(item))]),
            doneLeftOut = items.Count - shown.Count,
        };
    }

    /// <summary>
    /// TK-8.3: a new item in the named list, made from its template with the values given (<see cref="TaskTools.NewItem"/>),
    /// at a free name (<see cref="ItemFiles.FreePath"/>), through <see cref="Commit"/>: one step Ctrl+Z takes back, the
    /// panes filled again (the new item selected when its list is the one shown), "Claude added Call the dentist to
    /// Personal" said, a chime when the window is behind. The switch, a notice, a dialog and the user's typing were
    /// refused by the window base; a rename open here is refused too, since filling the list would end it.
    /// </summary>
    private object McpAdd(JsonObject arguments, string client)
    {
        if (!NotRenaming())
        {
            return new { error = "The user is renaming in the axTask window: ask again once that is done." };
        }

        var (list, refused) = TaskTools.Find(McpReachable(), McpProtocol.Text(arguments["list"]));
        if (list is null)
        {
            return new { error = refused };
        }

        var values = new List<KeyValuePair<string, string>>();
        if (arguments["fields"] is JsonObject fields)
        {
            foreach (var (name, node) in fields)
            {
                if (node is not JsonValue value)
                {
                    return new { error = $"{name}: a field's value is a text, such as \"2026-11-18\"." };
                }

                values.Add(new(name, value.TryGetValue<string>(out var text) ? text : value.ToJsonString()));
            }
        }
        else if (arguments["fields"] is not null)
        {
            return new { error = "fields is an object of field titles and texts, such as {\"Due\": \"2026-11-18\"}." };
        }

        var template = TaskRules.TemplateOf(ItemFolder.ReadList(list.Folder));
        var (item, wrong) = TaskTools.NewItem(McpProtocol.Text(arguments["title"]), template, values, DateOnly.FromDateTime(DateTime.Now));
        if (item is null)
        {
            return new { error = wrong };
        }

        var title = item.Title;
        var path = ItemFiles.FreePath(list.Folder, ItemFiles.FileStem(title), ".json");
        var said = $"{McpRun.Speaker(client)} added {title} to {list.Name}";
        var shownHere = _smart is null && !Gathered && _folder.Equals(list.Folder, StringComparison.OrdinalIgnoreCase);
        if (!Commit(new Step([FileChange.Write(path, null, item.ToText())], said), shownHere ? path : SelectedKey()))
        {
            return new { error = $"{title} could not be written; the window shows why." };
        }

        ChimeForMcp();
        return new { added = title, list = list.Name, said, item = TaskTools.ItemOf(new ItemFile(path, item, item.ToText())) };
    }
}
