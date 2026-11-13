using System.ComponentModel;
using System.Text.Json;
using System.Text.Json.Nodes;
using AxKit.Core;
using AxKit.Forms;
using AxMCP.Core;

namespace AxMCP;

/// <summary>
/// axMCP's window (SPEC.md; the shape is docs/axKit/item-apps.md's), on the item apps' shared window <see cref="ItemForm"/>,
/// which holds the panes, the moves between levels, the shared keys and every field command. This class holds what is
/// axMCP's: the servers from Claude Code's files and the user's own list (MP-1), a server started as a child process
/// and spoken to through <see cref="McpClient"/> (MP-2), its tools, resources and prompts as levels (MP-3 to MP-5), a
/// tool's arguments as the detail pane's fields, the call and its result as a notice text. Every wait on a server is
/// awaited on the window's thread through the base's <c>Do</c> (AX-7.5).
/// </summary>
public sealed class MCPForm : ItemForm
{
    /// <summary>A server as started: its process and client, what it offers, and the arguments typed for its tools and prompts (MP-D6).</summary>
    private sealed class Running
    {
        public required StdioServer Server { get; init; }

        public ServerInfo? Info { get; set; }

        public List<ServerTool> Tools { get; } = [];

        public List<ServerResource> Resources { get; } = [];

        public List<ServerPrompt> Prompts { get; } = [];

        /// <summary>The detail pane's documents by row key, kept while the window lives.</summary>
        public Dictionary<string, ItemDocument> Arguments { get; } = new(StringComparer.Ordinal);

        /// <summary>The exit code once the process ended; null while it runs.</summary>
        public int? ExitCode { get; set; }

        public bool Starting { get; set; } = true;

        public bool IsRunning => ExitCode is null && !Starting;
    }

    private sealed record SectionTag(string Name, string Key);

    private static readonly object StartTag = new();

    private static readonly SectionTag ToolsSection = new("Tools", "tools");
    private static readonly SectionTag ResourcesSection = new("Resources", "resources");
    private static readonly SectionTag PromptsSection = new("Prompts", "prompts");

    private static readonly TimeSpan Wait = TimeSpan.FromSeconds(60);

    private readonly MCPSettings _settings;
    private readonly string? _folder;
    private readonly MenuStrip _menu = new();
    private readonly ToolStripMenuItem _serverMenu = new("&Server");
    private readonly ToolStripMenuItem _itemMenu = new("&Item");
    private readonly ToolStripMenuItem _fieldMenu = new("Fiel&d");
    private readonly Dictionary<string, Running> _running = new(StringComparer.OrdinalIgnoreCase);
    private List<McpServer> _servers = [];

    /// <summary>The level: a server, or null at the top; inside a server, a section or null.</summary>
    private McpServer? _server;

    private SectionTag? _section;
    private JsonObject? _lastResult;
    private bool _calling;

    /// <summary>The one step Ctrl+Z takes back: the last change to Your servers, and what it was.</summary>
    private (Action Undo, string Said)? _undo;

    public MCPForm(string? folder, MCPSettings settings, string? settingsError)
        : base(AxMCPApp.Name, settings, MCPSettings.DefaultPath, settingsError)
    {
        _settings = settings;
        _folder = folder is null ? null : Path.GetFullPath(folder);

        // ---- menus and keys: one command list (docs/axKit/app-kit.md AK-2), each key from the tables (MP-6) ----
        // The shared keys (ItemKeys) are ItemForm's commands, registered after these by PlaceControls; the items here
        // for them only show them. Enter on a resource reads it, before Enter opens (ItemForm's, later on the list).
        AddKey(new Command("Read the resource", ItemKeys.Open, ReadSelectedResource) { When = () => InNavigator() && SelectedRow()?.Tag is ServerResource, Mark = CommandMark.Open });
        // Session, Actions and Context (AX-9.1, AX-9.11, B-19): the servers read again and Exit; what acts on Your
        // servers as a whole; what acts on the server, the item or the field the focus is on.
        var session = new ToolStripMenuItem("&Session");
        session.DropDownItems.Add(Item("&Refresh", () => Refresh(announce: true), ItemKeys.Refresh, CommandMark.Read));
        session.DropDownItems.Add(new ToolStripSeparator());
        session.DropDownItems.Add(CreateExitItem());

        var actions = new ToolStripMenuItem("");
        actions.DropDownItems.Add(Item("&Add server...", NewItem, ItemKeys.New, CommandMark.Change));
        actions.DropDownItems.Add(Item("&Undo", Undo, ItemKeys.Undo, CommandMark.Change));

        _serverMenu.DropDownItems.Add(Item("&Start, or open", OpenSelected, ItemKeys.Open, CommandMark.Act));
        Add(_serverMenu, new Command("S&top", AxMCPKeys.StopServer, StopSelected) { When = NotRenaming, Mark = CommandMark.Act });
        _serverMenu.DropDownItems.Add(Item("&Rename", RenameSelected, ItemKeys.Rename, CommandMark.Change));
        _serverMenu.DropDownItems.Add(Item("Re&move", () => WithRow(DeleteRow), ItemKeys.Delete, CommandMark.Act));

        Add(_itemMenu, new Command("&Call, or fetch the prompt", AxMCPKeys.Call, CallSelected) { When = NotRenaming, Mark = CommandMark.Act });
        _itemMenu.DropDownItems.Add(Item("&Open its arguments", OpenSelected, ItemKeys.Open, CommandMark.Open));
        _itemMenu.DropDownItems.Add(Item("&Read in full", ReadAll, ItemKeys.ReadAll, CommandMark.Read));
        Add(_itemMenu, new Command("The last result as &JSON", AxMCPKeys.LastResult, OpenLastResult) { When = NotRenaming, Mark = CommandMark.Open });

        _fieldMenu.DropDownItems.Add(Item("&Change...", ChangeField, ItemKeys.Open, CommandMark.Change));
        _fieldMenu.DropDownItems.Add(Item("&Read in full", ReadAll, ItemKeys.ReadAll, CommandMark.Read));

        var options = new ToolStripMenuItem("&Options");
        options.DropDownItems.Add(CreateTextMenu());
        options.DropDownItems.Add(CreateGeneralMenu());
        options.DropDownItems.Add(CreateDeveloperMenu());

        var help = CreateHelpMenu(HelpText.UserGuide, "MCP &resources",
        [
            ("The &Model Context Protocol", "https://modelcontextprotocol.io/"),
            ("MCP servers in &Claude Code", "https://code.claude.com/docs/en/mcp"),
        ]);

        PlaceMenuBar(_menu, session, actions, new ToolStripMenuItem(), CreateContextMenu(_serverMenu, _itemMenu, _fieldMenu), options, help);
        PlaceControls(_menu);
        _servers = ReadServers();
        Fill(select: null);
    }

    // ---- what the window base asks for ----

    protected override string Shortcuts => HelpText.Shortcuts;

    protected override IEnumerable<string> DiagnosticsLines => [$"Servers: {_servers.Count}, running: {_running.Values.Count(running => running.IsRunning)}, level: {(_server is null ? "top" : _section?.Name ?? _server.Name)}"];

    /// <summary>The folder axMCP was started on, for Recently closed (AX-9.4); the app alone when none.</summary>
    protected override string? PathInHand => _folder ?? string.Empty;

    protected override IReadOnlyList<string> RestartArguments() => _folder is null ? [Dispatch.AxMCP] : [Dispatch.AxMCP, _folder];

    /// <summary>MP-2.3: closing the window stops every server.</summary>
    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        foreach (var running in _running.Values)
        {
            running.Server.Dispose();
        }

        base.OnFormClosing(e);
    }

    /// <summary>MP-3.2: a tool's arguments are the server's.</summary>
    protected override string? FieldsFixed => "A tool's arguments are the server's: its schema says which";

    /// <summary>Enter on the Description reads it whole; it is never changed.</summary>
    protected override bool ChangeSpecial(ItemRow row, int index, ItemField field)
    {
        if (field.Title != ArgumentFields.DescriptionField)
        {
            return false;
        }

        var text = FieldTypes.TextOf(field.Value);
        Announce(text.Length == 0 ? "No description" : text, true);
        return true;
    }

    // ---- the levels (MP-1 to MP-5) ----

    protected override (IReadOnlyList<ItemRow> Rows, string Name, string Title, string Place) ReadLevel()
    {
        if (_server is null)
        {
            return ServersLevel();
        }

        var running = RunningOf(_server);
        if (_section is null)
        {
            var rows = new List<ItemRow>();
            if (running is null || running.ExitCode is not null)
            {
                var label = _server.Kind != ServerKind.Stdio ? $"{_server.KindText} servers are not started by this version"
                    : running is null ? "Not running: Enter starts it"
                    : $"Stopped (exit code {running.ExitCode}): Enter starts it again";
                rows.Add(new ItemRow(true, "start", label, label, StartTag));
            }
            else if (running.Starting)
            {
                rows.Add(new ItemRow(false, "starting", "Starting", $"Starting {_server.Name}"));
            }
            else
            {
                foreach (var (section, count, has) in new[]
                {
                    (ToolsSection, running.Tools.Count, running.Info!.HasTools),
                    (ResourcesSection, running.Resources.Count, running.Info.HasResources),
                    (PromptsSection, running.Prompts.Count, running.Info.HasPrompts),
                })
                {
                    if (has)
                    {
                        rows.Add(new ItemRow(true, section.Key, section.Name, $"{section.Name}, {count}", section));
                    }
                }

                if (rows.Count == 0)
                {
                    rows.Add(new ItemRow(false, "none", "Nothing offered", $"{_server.Name} offers no tools, resources or prompts"));
                }
            }

            return (rows, $"{_server.Name}, {StateOf(_server)}", _server.Name, StateOf(_server));
        }

        IReadOnlyList<ItemRow> items = _section.Key switch
        {
            "tools" => (running?.Tools ?? []).Select(tool => new ItemRow(false, "tool:" + tool.Name, tool.Title, $"{tool.Title}, tool, {FirstSentence(tool.Description)}", tool)).ToList(),
            "resources" => (running?.Resources ?? []).Select(resource => new ItemRow(false, "resource:" + resource.Uri, resource.Name, $"{resource.Name}, {(resource.IsTemplate ? "resource template" : "resource")}, {resource.Uri}", resource)).ToList(),
            _ => (running?.Prompts ?? []).Select(prompt => new ItemRow(false, "prompt:" + prompt.Name, prompt.Title, $"{prompt.Title}, prompt, {FirstSentence(prompt.Description)}", prompt)).ToList(),
        };
        var noun = _section.Key switch { "tools" => "tool", "resources" => "resource", _ => "prompt" };
        return (items, $"{_section.Name}, {Count(items.Count, noun)}", $"{_section.Name} of {_server.Name}", StateOf(_server));
    }

    /// <summary>The top level in sections (MP-1.1): Claude Code's servers, then Your servers; a header row over each that has any.</summary>
    private (IReadOnlyList<ItemRow> Rows, string Name, string Title, string Place) ServersLevel()
    {
        var rows = new List<ItemRow>();
        void Section(string name, IEnumerable<McpServer> servers)
        {
            var list = servers.ToList();
            if (list.Count == 0)
            {
                return;
            }

            rows.Add(new ItemRow(false, "section:" + name, name, $"{name}, section, {Count(list.Count, "server")}", null, IsSection: true));
            rows.AddRange(list.Select(server => new ItemRow(true, server.Key, server.Name, $"{server.Name}, {server.KindText}, {StateOf(server)}", server)));
        }

        Section("Claude Code", _servers.Where(server => !server.Own));
        Section(ServerSources.Own, _servers.Where(server => server.Own));
        if (rows.Count == 0)
        {
            rows.Add(new ItemRow(false, "none", "No servers", "No servers: Ctrl+N adds one of your own; claude mcp add in a terminal adds one to Claude Code"));
        }

        return (rows, $"Servers, {Count(_servers.Count, "server")}", "Servers", _folder ?? string.Empty);
    }

    protected override void EnterList(ItemRow row)
    {
        switch (row.Tag)
        {
            case McpServer server:
                _server = server;
                _section = null;
                EnsureStarted(server);
                break;
            case SectionTag section:
                _section = section;
                break;
            case var tag when ReferenceEquals(tag, StartTag):
                if (_server is not null)
                {
                    EnsureStarted(_server);
                }

                break;
        }
    }

    protected override string? LeaveList()
    {
        if (_section is { } section)
        {
            _section = null;
            return section.Key;
        }

        if (_server is { } server)
        {
            _server = null;
            return server.Key;
        }

        return null;
    }

    protected override ItemDetail DetailOf(ItemRow? row)
    {
        switch (row?.Tag)
        {
            case null when row is null:
                return new ItemDetail("Details, nothing selected", null, null, [new ItemDetailLine(Rows.Count == 0 ? "No servers: Ctrl+N adds one" : "Nothing selected")]);
            case McpServer server:
                var lines = new List<ItemDetailLine>
                {
                    new("Source: " + server.Source),
                    new(server.Kind == ServerKind.Stdio ? "Command: " + server.CommandLine : "Address: " + (server.Url ?? "none")),
                    new("Folder: " + (server.Folder ?? _folder ?? "your profile folder")),
                    new(server.Env.Count == 0 ? "Environment: nothing of its own" : $"Environment: {Count(server.Env.Count, "variable")}, {string.Join(", ", server.Env.Keys.Order(StringComparer.OrdinalIgnoreCase))}"),
                    new("State: " + StateOf(server)),
                };
                if (RunningOf(server) is { Info: { } info })
                {
                    lines.Add(new ItemDetailLine($"Server: {info.Name} {info.Version}".TrimEnd()));
                }

                return new ItemDetail("Details of " + server.Name, row.Key, null, lines);
            case SectionTag section:
                return new ItemDetail("Details of " + section.Name, row.Key, null, [new ItemDetailLine(row.Label), new ItemDetailLine(section.Key switch
                {
                    "tools" => "What the server can do: Tab on a tool for its arguments, Ctrl+Enter calls it",
                    "resources" => "What the server can show: Enter on a resource reads it",
                    _ => "Texts the server composes: Tab on a prompt for its arguments, Ctrl+Enter fetches it",
                })]);
            case ServerTool tool:
                return new ItemDetail("Arguments of " + tool.Title, row.Key, ArgumentsFor(row.Key, () => ArgumentFields.ForTool(tool)), [], tool);
            case ServerPrompt prompt:
                return new ItemDetail("Arguments of " + prompt.Title, row.Key, ArgumentsFor(row.Key, () => ArgumentFields.ForPrompt(prompt)), [], prompt);
            case ServerResource resource:
                var about = new List<ItemDetailLine>
                {
                    new("Name: " + resource.Name),
                    new((resource.IsTemplate ? "Address template: " : "Address: ") + resource.Uri),
                    new("Type: " + (resource.MimeType ?? "not given")),
                    new("Description: " + (resource.Description.Length == 0 ? "none" : resource.Description)),
                };
                if (resource.Size is { } size)
                {
                    about.Add(new ItemDetailLine("Size: " + FileSize.Text(size)));
                }

                about.Add(new ItemDetailLine(resource.IsTemplate ? "Enter asks for the address, then reads it" : "Enter reads it"));
                return new ItemDetail("Details of " + resource.Name, row.Key, null, about);
            default:
                return new ItemDetail("Details", row?.Key, null, [new ItemDetailLine(row?.Label ?? "Nothing selected")]);
        }
    }

    /// <summary>A change to a tool's or a prompt's arguments: kept with the server (MP-D6), the pane filled again, the change said.</summary>
    protected override void SaveFields(ItemDetail detail, ItemDocument changed, string said, int selectField)
    {
        if (detail.Key is null || _server is null || RunningOf(_server) is not { } running)
        {
            Announce("Nothing to change here", true);
            return;
        }

        running.Arguments[detail.Key] = changed;
        FillDetail();
        SelectField(selectField);
        Announce(said, true);
    }

    protected override void RenameItem(ItemRow row, string title) => Announce("Names here are the server's", true);

    protected override string? RenameRefused(ItemRow row) => row.Tag switch
    {
        McpServer { Own: true } => null,
        McpServer => "A server of Claude Code's is renamed with claude mcp remove and claude mcp add",
        _ => "Names here are the server's",
    };

    protected override string? RefusedName(ItemRow row, string name) => RefusedOwnName(name);

    protected override void RenameTo(ItemRow row, string name)
    {
        if (row.Tag is not McpServer { Own: true } server || OwnOf(server) is not { } own)
        {
            return;
        }

        var before = own.Name;
        own.Name = name.Trim();
        SaveSettings();
        ReadAndFill(ServerSources.Own + ": " + own.Name);
        Announce($"Renamed to {own.Name}", true);
        _undo = (() =>
        {
            own.Name = before;
            SaveSettings();
            ReadAndFill(ServerSources.Own + ": " + before);
        }, $"renamed {before} to {name.Trim()}");
    }

    protected override void DeleteRow(ItemRow row)
    {
        switch (row.Tag)
        {
            case McpServer { Own: true } server when OwnOf(server) is { } own:
                ShowNotice(Notice.Ask($"Remove {server.Name}?", "The server leaves axMCP's list; nothing is uninstalled.", "&Remove", () =>
                {
                    StopIfRunning(server);
                    var at = _settings.Servers.IndexOf(own);
                    _settings.Servers.Remove(own);
                    SaveSettings();
                    ReadAndFill(null);
                    Announce($"Removed {server.Name}", true);
                    _undo = (() =>
                    {
                        _settings.Servers.Insert(Math.Clamp(at, 0, _settings.Servers.Count), own);
                        SaveSettings();
                        ReadAndFill(server.Key);
                    }, $"removed {server.Name}");
                }));
                break;
            case McpServer:
                Announce("Only Your servers are removed here; a server of Claude Code's goes with claude mcp remove", true);
                break;
            default:
                Announce("Nothing to remove here", true);
                break;
        }
    }

    /// <summary>MP-1.3: a server of the user's own, asked in three notices: the name, the command line, the folder.</summary>
    protected override void NewItem()
    {
        if (_server is not null)
        {
            Announce("Servers are added at the top level", true);
            return;
        }

        AskText("Add server", "A name for the list.", "Name:", "Name", string.Empty, "&Next", name =>
            AskText("Add server", $"The command line that starts {name.Trim()}, as a terminal would run it.", "Command line:", "Command line", string.Empty, "&Next", command =>
                AskText("Add server", "The folder to start it in; empty for the folder axMCP was started on, or your profile folder.", "Folder:", "Folder", _folder ?? string.Empty, "&Add", folder =>
                {
                    var own = new OwnServer { Name = name.Trim(), CommandLine = command.Trim(), Folder = folder.Trim().Length == 0 ? null : folder.Trim() };
                    _settings.Servers.Add(own);
                    SaveSettings();
                    ReadAndFill(ServerSources.Own + ": " + own.Name);
                    Announce($"Added {own.Name}", true);
                    _undo = (() =>
                    {
                        _settings.Servers.Remove(own);
                        SaveSettings();
                        ReadAndFill(null);
                    }, $"added {own.Name}");
                }, typed => typed.Trim().Length > 0 && !Directory.Exists(typed.Trim()) ? "There is no folder " + typed.Trim() : null, required: false),
                typed => CommandLines.Split(typed).Count == 0 ? "Type the program and its arguments" : null),
            RefusedOwnName);
    }

    protected override void NewList() => Announce("Servers are added with Ctrl+N at the top level", true);

    protected override void Undo()
    {
        if (_undo is not { } step)
        {
            Announce("Nothing to undo", true);
            return;
        }

        _undo = null;
        step.Undo();
        Announce("Undone: " + step.Said, true);
    }

    /// <summary>F5 (MP-6): the files read again; a running server's lists too.</summary>
    protected override void BeforeRefresh()
    {
        _servers = ReadServers();
        if (_server is { } server && RunningOf(server) is { IsRunning: true } running)
        {
            Do(() => ListAsync(server, running, announce: false));
        }
    }

    // ---- starting and stopping (MP-2) ----

    private void EnsureStarted(McpServer server)
    {
        if (RunningOf(server) is { ExitCode: null })
        {
            return;
        }

        if (server.Kind != ServerKind.Stdio)
        {
            Announce($"{server.KindText} servers need a network connection of axKit's own, which this version does not make", true);
            return;
        }

        if (HasConsent(ConsentFor(server), () => StartServer(server), () => Announce($"Not connected to {server.Name}", true)))
        {
            StartServer(server);
        }
    }

    /// <summary>AX-11.4: the program the server is, in the consent notice's words.</summary>
    private static ConsentTarget ConsentFor(McpServer server) => new(
        $"{server.Name} ({Path.GetFileNameWithoutExtension(server.Command)})",
        $"The MCP server {server.Name} is a program on this computer, {server.CommandLine}, which axMCP starts and talks to for you: it does whatever its maker built it to do, on this computer and on any network it reaches, with your rights.",
        "the server's own licence and terms",
        "https://modelcontextprotocol.io/");

    private void StartServer(McpServer server)
    {
        Running running;
        try
        {
            running = new Running { Server = StdioServer.Start(server, server.Folder ?? _folder, Log.Info) };
        }
        catch (Exception ex) when (ex is Win32Exception or InvalidOperationException or IOException or UnauthorizedAccessException)
        {
            Failed($"Could not start {server.Name}", ex.Message);
            return;
        }

        _running[server.Key] = running;
        running.Server.Exited += (code, stopping) =>
        {
            if (!IsDisposed && IsHandleCreated)
            {
                BeginInvoke(() => OnExited(server, running, code, stopping));
            }
        };
        FillLevel();
        Do(() => ListAsync(server, running, announce: true));
    }

    /// <summary>The handshake and the three lists (MP-2.1), then the level filled and, at a start, said.</summary>
    private async Task ListAsync(McpServer server, Running running, bool announce)
    {
        using var limit = new CancellationTokenSource(Wait);
        try
        {
            var client = running.Server.Client;
            running.Info ??= await client.InitializeAsync(BundleInfo.Version, limit.Token);
            var tools = running.Info.HasTools ? await client.ListToolsAsync(limit.Token) : [];
            var resources = running.Info.HasResources ? await client.ListResourcesAsync(limit.Token) : [];
            var prompts = running.Info.HasPrompts ? await client.ListPromptsAsync(limit.Token) : [];
            running.Tools.Clear();
            running.Tools.AddRange(tools);
            running.Resources.Clear();
            running.Resources.AddRange(resources);
            running.Prompts.Clear();
            running.Prompts.AddRange(prompts);
        }
        catch (Exception ex) when (ex is McpError or OperationCanceledException)
        {
            running.Server.Stop();
            running.ExitCode ??= -1;
            running.Starting = false;
            FillLevel();
            Failed($"Could not start {server.Name}", ex is OperationCanceledException ? $"No answer from {server.Name} in 60 s" : ex.Message);
            return;
        }

        running.Starting = false;
        FillLevel();
        if (announce)
        {
            Announce($"{server.Name}, running, Tools {running.Tools.Count}, Resources {running.Resources.Count}, Prompts {running.Prompts.Count}", true);
        }
    }

    private void OnExited(McpServer server, Running running, int code, bool stopping)
    {
        running.ExitCode = code;
        running.Starting = false;
        FillLevel();
        if (!stopping)
        {
            Announce($"{server.Name} stopped (exit code {code})", true);
        }
    }

    /// <summary>Ctrl+Shift+S: the server of the level, or the one selected at the top (MP-2.3).</summary>
    private void StopSelected()
    {
        var server = _server ?? SelectedRow()?.Tag as McpServer;
        if (server is null)
        {
            Announce("Select a server first", true);
            return;
        }

        if (!StopIfRunning(server))
        {
            Announce($"{server.Name} is not running", true);
            return;
        }

        FillLevel();
        Announce($"Stopped {server.Name}", true);
    }

    private bool StopIfRunning(McpServer server)
    {
        if (RunningOf(server) is not { ExitCode: null } running)
        {
            return false;
        }

        running.Server.Stop();
        running.ExitCode ??= 0;
        running.Starting = false;
        return true;
    }

    // ---- calls (MP-3 to MP-5) ----

    /// <summary>Ctrl+Enter: the tool or prompt selected, with the arguments in the pane.</summary>
    private void CallSelected()
    {
        if (_server is null)
        {
            Announce("Select a tool first: Right into a server, then into Tools", true);
            return;
        }

        if (RunningOf(_server) is not { IsRunning: true } running)
        {
            Announce($"{_server.Name} is not running: Enter starts it", true);
            return;
        }

        if (_calling)
        {
            Announce("A call is running; it is given 60 seconds", true);
            return;
        }

        var row = SelectedRow();
        switch (row?.Tag)
        {
            case ServerTool tool:
                CallTool(running, row.Key, tool);
                break;
            case ServerPrompt prompt:
                GetPrompt(running, row.Key, prompt);
                break;
            default:
                Announce("Select a tool or a prompt first", true);
                break;
        }
    }

    private void CallTool(Running running, string key, ServerTool tool)
    {
        var document = ArgumentsFor(key, () => ArgumentFields.ForTool(tool));
        if (ArgumentFields.MissingRequired(document) is { } missing)
        {
            Announce($"{missing} is required", true);
            return;
        }

        JsonObject arguments;
        try
        {
            arguments = ArgumentFields.ToArguments(document);
        }
        catch (JsonException ex)
        {
            Announce("Not JSON: " + ex.Message, true);
            return;
        }

        Announce($"Calling {tool.Title}", true);
        Request(() => running.Server.Client.CallToolAsync(tool.Name, arguments, Token()), tool.Title, result =>
        {
            _lastResult = result;
            ShowNotice(Notice.Plain("Result of " + tool.Title, ResultText.OfCall(result)));
        });
    }

    private void GetPrompt(Running running, string key, ServerPrompt prompt)
    {
        var document = ArgumentsFor(key, () => ArgumentFields.ForPrompt(prompt));
        if (ArgumentFields.MissingRequired(document) is { } missing)
        {
            Announce($"{missing} is required", true);
            return;
        }

        Announce($"Fetching {prompt.Title}", true);
        Request(() => running.Server.Client.GetPromptAsync(prompt.Name, ArgumentFields.ToPromptArguments(document), Token()), prompt.Title, result =>
        {
            _lastResult = result;
            ShowNotice(Notice.Plain(prompt.Title, ResultText.OfPrompt(result)));
        });
    }

    /// <summary>Enter on a resource (MP-4.2): read, or the address asked for a template first.</summary>
    private void ReadSelectedResource()
    {
        if (SelectedRow()?.Tag is not ServerResource resource || _server is null || RunningOf(_server) is not { IsRunning: true } running)
        {
            Announce(_server is null || RunningOf(_server) is { IsRunning: true } ? "Select a resource first" : $"{_server.Name} is not running: Enter starts it", true);
            return;
        }

        if (resource.IsTemplate)
        {
            AskText("Read a resource", "The address, with the template's variables filled in.", "Address:", "Address", resource.Uri, "&Read", uri => ReadResource(running, resource.Name, uri.Trim()));
            return;
        }

        ReadResource(running, resource.Name, resource.Uri);
    }

    private void ReadResource(Running running, string name, string uri)
    {
        Announce($"Reading {name}", true);
        Request(() => running.Server.Client.ReadResourceAsync(uri, Token()), name, result =>
        {
            _lastResult = result;
            ShowNotice(Notice.Plain(name, ResultText.OfResource(result)));
        });
    }

    private CancellationTokenSource? _wait;

    private CancellationToken Token()
    {
        _wait?.Dispose();
        _wait = new CancellationTokenSource(Wait);
        return _wait.Token;
    }

    /// <summary>One request to the server on the window's thread (AX-7.5): the answer shown, an error said as a notice, the wait given up after 60 s (MP-2.4).</summary>
    private void Request(Func<Task<JsonObject>> request, string what, Action<JsonObject> shown)
    {
        _calling = true;
        Do(async () =>
        {
            try
            {
                shown(await request());
            }
            catch (McpError ex)
            {
                Failed($"{what} failed", ex.Message);
            }
            catch (OperationCanceledException)
            {
                Announce($"No answer from {_server?.Name ?? "the server"} in 60 s", true);
            }
            finally
            {
                _calling = false;
            }
        });
    }

    /// <summary>Ctrl+J (MP-3.6): the last answer, whole, as a file axJSON opens.</summary>
    private void OpenLastResult()
    {
        if (_lastResult is null)
        {
            Announce("No result yet", true);
            return;
        }

        var path = Path.Combine(Path.GetDirectoryName(AppPaths.LogsFolder)!, "axMCP", "last-result.json");
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(path, _lastResult.ToJsonString(new JsonSerializerOptions { WriteIndented = true, Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping }).ReplaceLineEndings("\r\n") + "\r\n");
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            Failed("Last result", "The result could not be written: " + ex.Message);
            return;
        }

        WindowMenu.Start(Dispatch.AxJSON, path);
    }

    // ---- helpers ----

    private Running? RunningOf(McpServer server) => _running.TryGetValue(server.Key, out var running) ? running : null;

    private OwnServer? OwnOf(McpServer server) => _settings.Servers.FirstOrDefault(own => string.Equals(own.Name, server.Name, StringComparison.OrdinalIgnoreCase));

    private List<McpServer> ReadServers() => [.. ServerSources.Read(_folder), .. _settings.OwnServers()];

    private void ReadAndFill(string? select)
    {
        _servers = ReadServers();
        Fill(select);
    }

    /// <summary>The level filled again with the selection kept, or the first row when the rows changed under it (a start that just finished).</summary>
    private void FillLevel()
    {
        var key = SelectedKey();
        Fill(key is "start" or "starting" ? null : key);
    }

    private ItemDocument ArgumentsFor(string key, Func<ItemDocument> make)
    {
        if (_server is null || RunningOf(_server) is not { } running)
        {
            return make();
        }

        if (!running.Arguments.TryGetValue(key, out var document))
        {
            document = make();
            running.Arguments[key] = document;
        }

        return document;
    }

    private string? RefusedOwnName(string typed)
    {
        var name = typed.Trim();
        if (name.Length == 0)
        {
            return "Type a name";
        }

        return _settings.Servers.Any(own => string.Equals(own.Name, name, StringComparison.OrdinalIgnoreCase)) ? $"There is a server named {name} already" : null;
    }

    private string StateOf(McpServer server) => RunningOf(server) switch
    {
        null => server.Kind == ServerKind.Stdio ? "stopped" : "not started by this version",
        { Starting: true, ExitCode: null } => "starting",
        { ExitCode: { } code } => $"stopped (exit code {code})",
        _ => "running",
    };

    /// <summary>The first sentence of a description, for a row (MP-3.1); the whole text when it has none.</summary>
    private static string FirstSentence(string description)
    {
        var text = description.ReplaceLineEndings(" ").Trim();
        if (text.Length == 0)
        {
            return "no description";
        }

        var end = text.IndexOf(". ", StringComparison.Ordinal);
        return end > 0 ? text[..(end + 1)] : text.Length > 160 ? text[..160].TrimEnd() + "…" : text;
    }
}
