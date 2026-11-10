using System.Text.Json.Nodes;
using AxKit.Core;
using AxKit.Core.Reports;
using AxKit.Forms;
using AxTask.Core;

namespace AxTask;

/// <summary>
/// The GitHub lists and the action keys (docs/axKit/reports.md RP-7, RP-8): a list whose <c>list.json</c> names a
/// repository fills itself from the repository's issues when it opens and on F5; Ctrl+1 to Ctrl+9 run the actions a
/// list's <c>list.json</c> assigns (set fields, move, open), and in a GitHub list a Kind or State set goes to GitHub as
/// the user's own account first; the Item menu's GitHub submenu has the actions by name; the user's token is kept
/// protected on this computer.
/// </summary>
public sealed partial class TaskForm
{
    private readonly ToolStripMenuItem _actionKeysMenu = new("Action &keys");
    private readonly ToolStripMenuItem[] _actionKeyItems = new ToolStripMenuItem[ActionKeys.Last + 1];
    private readonly ToolStripMenuItem _gitHubMenu = new("Git&Hub");

    private static readonly TimeSpan GitHubLimit = TimeSpan.FromSeconds(15);

    // ---- the list in hand ----

    private ItemDocument? CurrentListDetails => _smart is null && !AtTop && Directory.Exists(_folder) ? ItemFolder.ReadList(_folder) : null;

    private GitHubListSettings? CurrentGitHub => GitHubList.Of(CurrentListDetails);

    /// <summary>The menus of this file, placed by the constructor: the action keys in the Item menu, the GitHub actions, the List menu's items.</summary>
    private void PlaceGitHubMenus()
    {
        for (var digit = ActionKeys.First; digit <= ActionKeys.Last; digit++)
        {
            var d = digit;
            var item = new ToolStripMenuItem("Not set", null, (_, _) => RunActionKey(d)) { ShortcutKeyDisplayString = BundleKeys.Text(AxTaskKeys.ActionKey(d)) };
            _actionKeyItems[d] = item;
            _actionKeysMenu.DropDownItems.Add(item);
            AddKey(new Command($"Action key {d}", AxTaskKeys.ActionKey(d), () => RunActionKey(d)) { When = NotRenaming });
        }

        CommandPalette.RefreshedOnOpening(_actionKeysMenu, RefreshActionKeyItems);

        _gitHubMenu.DropDownItems.Add(new ToolStripMenuItem("Make it a &bug", null, (_, _) => SetKind(ReportKind.Bug)));
        _gitHubMenu.DropDownItems.Add(new ToolStripMenuItem("Make it a &feature request", null, (_, _) => SetKind(ReportKind.Feature)));
        _gitHubMenu.DropDownItems.Add(new ToolStripMenuItem("Make it feedbac&k", null, (_, _) => SetKind(ReportKind.Feedback)));
        _gitHubMenu.DropDownItems.Add(new ToolStripMenuItem("&Close", null, (_, _) => SetState(GitHubList.ClosedState)));
        _gitHubMenu.DropDownItems.Add(new ToolStripMenuItem("Reo&pen", null, (_, _) => SetState(GitHubList.OpenState)));
        _gitHubMenu.DropDownItems.Add(new ToolStripMenuItem("Co&mment...", null, (_, _) => CommentOnIssue()));
        _gitHubMenu.DropDownItems.Add(new ToolStripSeparator());
        _gitHubMenu.DropDownItems.Add(new ToolStripMenuItem("Block this &reporter", null, (_, _) => Block(reporter: true)));
        _gitHubMenu.DropDownItems.Add(new ToolStripMenuItem("Block this &version", null, (_, _) => Block(reporter: false)));
        _gitHubMenu.DropDownItems.Add(new ToolStripSeparator());
        _gitHubMenu.DropDownItems.Add(new ToolStripMenuItem("Open on &GitHub", null, (_, _) => WithGitHubItem((_, item) => OpenTarget(item, GitHubList.IssueField))));
        _gitHubMenu.DropDownItems.Add(new ToolStripMenuItem("Open in ax&Claude", null, (_, _) => WithGitHubItem((_, item) => OpenInAxClaude(item))));
    }

    /// <summary>The nine items say the list's actions as the menu opens (RP-8.3), so that the palette and F1 list the user's names.</summary>
    private void RefreshActionKeyItems()
    {
        var keys = ActionKeys.Read(CurrentListDetails);
        for (var digit = ActionKeys.First; digit <= ActionKeys.Last; digit++)
        {
            _actionKeyItems[digit].Text = keys.TryGetValue(digit, out var action) ? action.Name.Replace("&", "&&", StringComparison.Ordinal) : "Not set";
        }
    }

    // ---- Ctrl+1 to Ctrl+9 (RP-8.1) ----

    private void RunActionKey(int digit)
    {
        if (_smart is not null || AtTop)
        {
            Sounds.Edge();
            Announce("Action keys act in a list: open one first", true);
            return;
        }

        if (!ActionKeys.Read(CurrentListDetails).TryGetValue(digit, out var action))
        {
            Sounds.Edge();
            Announce($"No action on Ctrl+{digit} in this list; List menu, Action keys sets one", true);
            return;
        }

        if (SelectedRow() is not { Tag: ItemFile item })
        {
            Sounds.Edge();
            Announce("Select an item first", true);
            return;
        }

        if (action.Set is { } set)
        {
            SetFields(item, set, action.Name);
        }
        else if (action.Move is { } move)
        {
            MoveItem(item, move, action.Name);
        }
        else if (action.Open is { } open)
        {
            OpenTarget(item, open);
        }
    }

    /// <summary>A Set action (RP-8.1): the fields written as one step; in a GitHub list a Kind or State goes to GitHub first (RP-7.2).</summary>
    private void SetFields(ItemFile item, IReadOnlyDictionary<string, JsonNode?> set, string name)
    {
        if (!CheckFresh(item.Path, item.Text))
        {
            return;
        }

        var changed = ActionKeys.ApplySet(item.Document, set);
        var said = $"{name}: {ActionKeys.SpokenSet(set)}";
        if (CurrentGitHub is { } github && GitHubChange(set) is { } change)
        {
            var number = GitHubList.NumberOf(item.Document);
            if (number <= 0)
            {
                Announce("This item has no issue number", true);
                return;
            }

            WithGitHubToken(token => Do(async () =>
            {
                if (!await TryGitHubAsync(ct => GitHubIssues.PatchAsync(AxKitHttp.Client, github.Repository, token, number, change.Labels, change.State, ct), "changed"))
                {
                    return;
                }

                Commit(new Step([FileChange.Write(item.Path, item.Text, changed.ToText())], said), item.Path);
            }));
            return;
        }

        Commit(new Step([FileChange.Write(item.Path, item.Text, changed.ToText())], said), item.Path);
    }

    /// <summary>What of a Set reaches GitHub: the labels for a Kind, the state for a State; null for a set of local fields alone.</summary>
    private static (IReadOnlyList<string>? Labels, string? State)? GitHubChange(IReadOnlyDictionary<string, JsonNode?> set)
    {
        IReadOnlyList<string>? labels = null;
        string? state = null;
        if (set.TryGetValue(GitHubList.KindField, out var kind))
        {
            labels = GitHubList.LabelOf(FieldTypes.TextOf(kind)) is { } label ? [label] : [];
        }

        if (set.TryGetValue(GitHubList.StateField, out var stateValue) && FieldTypes.TextOf(stateValue).Trim().ToLowerInvariant() is GitHubList.OpenState or GitHubList.ClosedState)
        {
            state = FieldTypes.TextOf(stateValue).Trim().ToLowerInvariant();
        }

        return labels is null && state is null ? null : (labels, state);
    }

    /// <summary>A Move action (RP-8.1): to the list at the relative path, the links following (IA-D3).</summary>
    private void MoveItem(ItemFile item, string move, string name)
    {
        var target = Path.GetFullPath(Path.Combine(_folder, move));
        var top = TopFolder.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
        if (!Directory.Exists(target) || !target.StartsWith(top, StringComparison.OrdinalIgnoreCase) || ItemFolder.IsArchive(TopFolder, target) || target.Equals(_folder, StringComparison.OrdinalIgnoreCase))
        {
            Announce($"No list to move to at {move}", true);
            return;
        }

        if (!CheckFresh(item.Path, item.Text))
        {
            return;
        }

        var to = ItemFiles.FreePath(target, Path.GetFileNameWithoutExtension(item.Path), ".json");
        var parts = new List<FileChange> { FileChange.Move(item.Path, to) };
        parts.AddRange(Links.AfterMove(ItemFolder.AllItems(TopFolder), item.Path, to));
        Commit(new Step(parts, $"{name}: {ItemFolder.NameOf(item)} moved to {Path.GetFileName(target)}"), NeighbourKey());
    }

    /// <summary>An Open action (RP-8.1): axClaude on the issue, an app on the item's file, or a field's web address in the browser.</summary>
    private void OpenTarget(ItemFile item, string open)
    {
        if (open.Equals(GitHubList.AxClaudeTarget, StringComparison.OrdinalIgnoreCase))
        {
            OpenInAxClaude(item);
            return;
        }

        if (Dispatch.Apps.FirstOrDefault(known => known.Verb.Equals(open, StringComparison.OrdinalIgnoreCase)) is { } app)
        {
            // A shell or Claude Code never on a folder off this computer (RP-8.1, S-5's rule; 2026-11-09).
            if (ActionKeys.OpenRefusal(app.Verb, item.Path) is { } refused)
            {
                Announce(refused, true);
                return;
            }

            AxKitMenu.Start(app.Verb, item.Path);
            return;
        }

        var index = item.Document.IndexOf(open);
        var value = index >= 0 ? FieldTypes.TextOf(item.Document.Fields[index].Value).Trim() : string.Empty;
        if (value.StartsWith("https://", StringComparison.OrdinalIgnoreCase) || value.StartsWith("http://", StringComparison.OrdinalIgnoreCase))
        {
            OpenUrl(value);
            return;
        }

        Announce(index < 0 ? $"No field called {open}, and no app called that" : value.Length == 0 ? $"{open} is empty" : $"{open} holds no web address", true);
    }

    /// <summary>Open in axClaude (RP-7.2): axClaude on the code repository's folder named in list.json, the issue in its message field, not sent.</summary>
    private void OpenInAxClaude(ItemFile item)
    {
        if (CurrentGitHub is not { Folder: { } folder })
        {
            Announce("Set the code repository's folder first: \"folder\" under \"github\" in this list's list.json (Ctrl+J)", true);
            return;
        }

        // Before the folder is touched: a share is never asked whether it exists (S-5; 2026-11-09).
        if (ActionKeys.OpenRefusal(Dispatch.AxClaude, folder) is { } refused)
        {
            Announce(refused, true);
            return;
        }

        if (!Directory.Exists(folder))
        {
            Announce($"The folder in list.json does not exist: {folder}", true);
            return;
        }

        var body = FieldText(item.Document, GitHubList.BodyField);
        var text = $"Issue {GitHubList.NumberOf(item.Document)}: {item.Document.Title}\r\n\r\n{body}";
        if (text.Length > 8000)
        {
            text = text[..8000] + "\r\n(cut; the rest is in the item's Body field)";
        }

        AxKitMenu.StartWith(Dispatch.AxClaude, [folder, "--message", text]);
    }

    // ---- the GitHub list itself (RP-7.1) ----

    /// <summary>Called when a list opens: a GitHub list is read from GitHub on each visit.</summary>
    private void RefreshGitHubOnEnter()
    {
        if (CurrentGitHub is { } github)
        {
            RefreshGitHub(github, _folder);
        }
    }

    /// <summary>F5 in a GitHub list reads GitHub again.</summary>
    private void RefreshGitHubAgain()
    {
        if (CurrentGitHub is { } github)
        {
            RefreshGitHub(github, _folder);
        }
    }

    private void RefreshGitHub(GitHubListSettings github, string folder) => Do(async () =>
    {
        IReadOnlyList<IssueInfo> issues;
        try
        {
            using var limit = new CancellationTokenSource(GitHubLimit);
            issues = await GitHubIssues.ListAsync(AxKitHttp.Client, github.Repository, github.State, UserToken(), limit.Token);
        }
        catch (Exception ex) when (ex is GitHubException or HttpRequestException or OperationCanceledException)
        {
            Announce($"GitHub not read: {GitHubIssues.ReasonOf(ex)}; the list shows what was read last", true);
            return;
        }

        int added;
        IReadOnlyList<FileChange> changes;
        try
        {
            changes = GitHubList.Refresh(folder, issues, ItemFolder.ArchiveFolder(TopFolder), out added);
            if (changes.Count > 0)
            {
                // A mirror of GitHub, not a step of the user's: nothing to undo (RP-7.1).
                Changes.Apply(changes);
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidOperationException or InvalidDataException)
        {
            Fail($"The issues could not be written: {ex.Message}");
            return;
        }

        if (_folder.Equals(folder, StringComparison.OrdinalIgnoreCase) && _smart is null)
        {
            Fill(SelectedKey());
            Announce(GitHubList.SpokenRefresh(ListTitle(), Rows.Count, added), false);
        }
    });

    /// <summary>List, New GitHub list (RP-7.1): a list in the top folder named after the repository, filled when it opens.</summary>
    private void NewGitHubList()
    {
        var folder = TopFolder;
        AskText("New GitHub list", "The repository, as owner/name, for example KyleKeane/AxKit-reports. Its open issues become the list's items; your Notes on each stay yours.",
            "Repository:", "Repository", string.Empty, "&Make", typed =>
            {
                var repository = typed.Trim();
                var name = repository[(repository.IndexOf('/') + 1)..] + " issues";
                var listFile = Path.Combine(folder, name, ItemDocument.ListFile);
                var details = WithSectionField(GitHubList.NewList(repository, GitHubList.OpenState, null));
                _smart = null;
                _folder = TopFolder;
                Commit(new Step([FileChange.WriteInNewFolder(listFile, details.ToText())], "Made " + name), Path.Combine(folder, name));
            },
            typed => !GitHubList.IsRepository(typed) ? "Type the repository as owner/name" : ItemFolder.RefusedListName(folder, typed.Trim()[(typed.Trim().IndexOf('/') + 1)..] + " issues"));
    }

    // ---- the GitHub actions of the Item menu (RP-7.2) ----

    private void SetKind(ReportKind kind) =>
        WithGitHubItem((_, item) => SetFields(item, new Dictionary<string, JsonNode?> { [GitHubList.KindField] = JsonValue.Create(ReportKinds.Name(kind)) }, "Make it " + (kind == ReportKind.Feedback ? "feedback" : "a " + ReportKinds.Name(kind).ToLowerInvariant())));

    private void SetState(string state) =>
        WithGitHubItem((_, item) => SetFields(item, new Dictionary<string, JsonNode?> { [GitHubList.StateField] = JsonValue.Create(state) }, state == GitHubList.ClosedState ? "Close" : "Reopen"));

    private void CommentOnIssue() => WithGitHubItem((github, item) =>
    {
        var number = GitHubList.NumberOf(item.Document);
        AskText($"Comment on issue {number}", "Your comment goes to the issue on GitHub as your account.", "Comment:", "Comment", string.Empty, "&Send", comment =>
            WithGitHubToken(token => Do(async () =>
            {
                if (await TryGitHubAsync(ct => GitHubIssues.CommentAsync(AxKitHttp.Client, github.Repository, token, number, comment.Trim(), ct), "commented on"))
                {
                    Announce($"Comment sent to issue {number}", true);
                }
            })),
            typed => typed.Trim().Length == 0 ? "Type the comment" : null);
    });

    /// <summary>Block this reporter or this version (RP-6.4): the blocklist of the reports repository read, added to and written as the user.</summary>
    private void Block(bool reporter) => WithGitHubItem((github, item) =>
    {
        var value = FieldText(item.Document, reporter ? GitHubList.ReporterField : GitHubList.VersionField).Trim();
        if (value.Length == 0 || (reporter && !Guid.TryParse(value, out _)))
        {
            Announce(reporter ? "This issue was not sent from axKit: no reporter id to block" : "This issue names no axKit version", true);
            return;
        }

        WithGitHubToken(token => Do(async () =>
        {
            var done = await TryGitHubAsync(async ct =>
            {
                var list = await GitHubIssues.ReadBlocklistAsync(AxKitHttp.Client, github.Repository, ct);
                list = reporter ? list.WithReporter(Guid.Parse(value)) : list.WithVersion(value);
                await GitHubIssues.WriteBlocklistAsync(AxKitHttp.Client, github.Repository, token, list, $"Block {(reporter ? "reporter " + value : "version " + value)}", ct);
            }, "written");
            if (done)
            {
                Announce(reporter ? $"Blocked the reporter {value[..8]}; the workflow closes what still arrives" : $"Blocked axKit {value}; the workflow closes what still arrives", true);
            }
        }));
    });

    /// <summary>A GitHub action needs a GitHub list and an item selected in it.</summary>
    private void WithGitHubItem(Action<GitHubListSettings, ItemFile> act)
    {
        if (CurrentGitHub is not { } github)
        {
            Announce("Not a GitHub list: List menu, New GitHub list makes one", true);
            return;
        }

        if (SelectedRow() is not { Tag: ItemFile item })
        {
            Announce("Select an issue first", true);
            return;
        }

        act(github, item);
    }

    /// <summary>One GitHub call with its limit; a refusal said in a few words, false then.</summary>
    private async Task<bool> TryGitHubAsync(Func<CancellationToken, Task> call, string what)
    {
        try
        {
            using var limit = new CancellationTokenSource(GitHubLimit);
            await call(limit.Token);
            return true;
        }
        catch (Exception ex) when (ex is GitHubException or HttpRequestException or OperationCanceledException)
        {
            Announce($"Not {what}: {GitHubIssues.ReasonOf(ex)}", true);
            return false;
        }
    }

    // ---- the user's token (RP-7.2) ----

    private void WithGitHubToken(Action<string> act)
    {
        if (UserToken() is { } token)
        {
            act(token);
        }
        else
        {
            Announce("Set your GitHub token in the List menu first: GitHub account", true);
        }
    }

    /// <summary>The user's own token, kept protected for this Windows account; null for none.</summary>
    private static string? UserToken()
    {
        try
        {
            return File.Exists(AppPaths.GitHubTokenFile) ? UserSecret.Unprotect(File.ReadAllText(AppPaths.GitHubTokenFile)) is { Length: > 0 } token ? token : null : null;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or System.Security.Cryptography.CryptographicException or FormatException)
        {
            return null;
        }
    }

    /// <summary>List, GitHub account: a fine-grained token of the user's, pasted once into a hidden field (RP-7.2).</summary>
    private void SetGitHubAccount()
    {
        AskText("GitHub account",
            "A fine-grained token of your own GitHub account, for the GitHub actions: Issues read and write, and Contents read and write for the blocklist, on the reports repository alone. It is kept protected for your Windows account in " + Path.GetDirectoryName(AppPaths.GitHubTokenFile) + ", never sent anywhere but to GitHub.",
            "Token:", "Paste your GitHub token", string.Empty, "&Keep", typed =>
            {
                var token = typed.Trim();
                try
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(AppPaths.GitHubTokenFile)!);
                    File.WriteAllText(AppPaths.GitHubTokenFile, UserSecret.Protect(token));
                    Announce("GitHub token kept", false);
                }
                catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or System.Security.Cryptography.CryptographicException)
                {
                    Fail("The token could not be kept: " + ex.Message);
                }
            }, emptyMessage: "Paste the token first", hidden: true, cancel: "Cancel");
    }

    private void ForgetGitHubAccount()
    {
        try
        {
            if (File.Exists(AppPaths.GitHubTokenFile))
            {
                File.Delete(AppPaths.GitHubTokenFile);
                Announce("GitHub token forgotten", true);
            }
            else
            {
                Announce("No GitHub token was kept", true);
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            Fail("The token could not be removed: " + ex.Message);
        }
    }

    // ---- List, Action keys (RP-8.2) ----

    private void EditActionKeys()
    {
        if (_smart is not null || AtTop)
        {
            Announce("Action keys belong to a list: open one first", true);
            return;
        }

        var folder = _folder;
        var keys = ActionKeys.Read(ItemFolder.ReadList(folder));
        var lines = Enumerable.Range(ActionKeys.First, ActionKeys.Last).Select(digit => ActionKeys.Spoken(digit, keys.GetValueOrDefault(digit))).ToList();
        ChooseFromList("Action keys", $"The Ctrl+number keys of {ListTitle()}: Enter on one sets or changes it.", "Keys", lines, 0, chosen => EditActionKey(folder, chosen + ActionKeys.First));
    }

    private void EditActionKey(string folder, int digit)
    {
        var current = ActionKeys.Read(ItemFolder.ReadList(folder)).GetValueOrDefault(digit);
        AskText($"Ctrl+{digit}", "The action's name, as the Item menu will show it. Empty removes the key.", "Name:", "Name", current?.Name ?? string.Empty, "&Next", typed =>
        {
            var name = typed.Trim();
            if (name.Length == 0)
            {
                WriteActionKey(folder, digit, null);
                return;
            }

            var kinds = new[] { "Set fields to values", "Move the item to a list", "Open a field's web address, or an app" };
            var start = current?.Kind switch { ListAction.MoveKind => 1, ListAction.OpenKind => 2, _ => 0 };
            ChooseFromList($"Ctrl+{digit}: {name}", "What the key does.", "What", kinds, start, kind =>
            {
                switch (kind)
                {
                    case 0:
                        AskText($"Ctrl+{digit}: {name} sets", "One field per line, as Field: value. Done: done or not done for a check; Kind: Bug.", "Fields:", "Fields",
                            current?.Set is { } set ? string.Join("\r\n", set.Select(pair => $"{pair.Key}: {FieldTypes.TextOf(pair.Value)}")) : string.Empty, "&Keep",
                            text => WriteActionKey(folder, digit, new ListAction(name, Set: ActionKeys.ParseSetLines(text))),
                            text => ActionKeys.ParseSetLines(text) is null ? "Type at least one line as Field: value" : null);
                        break;
                    case 1:
                        AskText($"Ctrl+{digit}: {name} moves to", "The list's name, a list in the top folder.", "List:", "List", current?.Move?.Replace("../", string.Empty, StringComparison.Ordinal) ?? string.Empty, "&Keep",
                            text => WriteActionKey(folder, digit, new ListAction(name, Move: "../" + text.Trim())),
                            text => Directory.Exists(Path.Combine(TopFolder, text.Trim())) ? null : $"No list called {text.Trim()} in the top folder");
                        break;
                    default:
                        AskText($"Ctrl+{digit}: {name} opens", "A field whose value is a web address (Issue), or an app for the item's file: axjson, axfile, or axclaude for an issue.", "Open:", "Open", current?.Open ?? string.Empty, "&Keep",
                            text => WriteActionKey(folder, digit, new ListAction(name, Open: text.Trim())),
                            text => text.Trim().Length == 0 ? "Type a field's title or an app" : null);
                        break;
                }
            });
        }, _ => null, required: false);
    }

    /// <summary>The list's details written with the key set or removed, one undo step.</summary>
    private void WriteActionKey(string folder, int digit, ListAction? action)
    {
        var listFile = Path.Combine(folder, ItemDocument.ListFile);
        var before = Changes.ReadOrNull(listFile);
        var details = ItemFolder.ReadList(folder) ?? ItemDocument.NewList(TaskRules.TemplateOf(null));
        var changed = ActionKeys.With(details, digit, action);
        Commit(new Step([FileChange.Write(listFile, before, changed.ToText())], action is null ? $"Ctrl+{digit} cleared" : $"Ctrl+{digit}: {action.Name}, {action.Described}"), SelectedKey());
    }

    private static string FieldText(ItemDocument item, string field) =>
        item.IndexOf(field) is var index && index >= 0 ? FieldTypes.TextOf(item.Fields[index].Value) : string.Empty;
}
