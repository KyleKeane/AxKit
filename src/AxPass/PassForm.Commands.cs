using AxKit.Core;
using AxKit.Forms;
using AxPass.Core;

namespace AxPass;

/// <summary>
/// axPass's commands (docs/axPass/SPEC.md PW-4 to PW-7): each one or two runs of the tool, the list read again after,
/// and for Ctrl+Z the runs that take it back. The field commands are the shared window's; they reach the database
/// through <see cref="SaveFields"/>.
/// </summary>
public sealed partial class PassForm
{
    /// <summary>
    /// One run of the tool that takes a change back: the command, its options, the paths after the database.
    /// <see cref="Sole"/>: the first path is an entry in the Recycle Bin, reached by its name, and the run is made
    /// only while a listing read just before shows one entry of that path and no more (PW-6.4: the tool takes the
    /// first of two, the older one). <see cref="Purge"/>: the run removes what the step made, and where that lands
    /// in the Recycle Bin as the one of its name it is deleted there too, so that undoing a new entry leaves none.
    /// </summary>
    private sealed record Call(string Command, string[] Options, string[] Rest)
    {
        public bool Sole { get; init; }

        public bool Purge { get; init; }
    }

    /// <summary>
    /// A change Ctrl+Z can take back: its words, the runs that undo it, and the row to select after; or, with
    /// <paramref name="CannotUndo"/>, a change the tool cannot take back (a value written into an empty field), which
    /// Ctrl+Z says and passes over.
    /// </summary>
    private sealed record Step(string Said, IReadOnlyList<Call> Undo, string? Select, string? CannotUndo = null);

    private const int MaxUndo = 20;

    private readonly List<Step> _undo = [];

    /// <summary>A change to an entry's fields (PW-6.1): <c>edit</c> with the options that make it, the way back kept.</summary>
    protected override void SaveFields(ItemDetail detail, ItemDocument changed, string said, int selectField)
    {
        if (detail.Source is not string path || detail.Document is not { } before)
        {
            return;
        }

        var options = EntryFields.EditOptions(before, changed);
        if (options.Count == 0)
        {
            Announce("Only KeePass's own fields change here", true);
            return;
        }

        // The tool cannot empty a field (EntryFields.Emptied): said in axPass's words, and the tool is not started.
        if (EntryFields.Emptied(before, changed) is { Count: > 0 } emptied)
        {
            Announce(EntryFields.CannotEmpty(emptied), true);
            return;
        }

        Do(async () =>
        {
            if (await Cli(KeePassCliTexts.Edit, options, [path]) is null)
            {
                return;
            }

            _entries[path] = changed;
            // A field that was empty cannot be emptied again by the tool: the step says so when Ctrl+Z comes to it.
            var back = EntryFields.Emptied(changed, before);
            Remember(back.Count > 0
                ? new Step(said, [], path, EntryFields.CannotEmpty(back))
                : new Step(said, [new Call(KeePassCliTexts.Edit, [.. EntryFields.EditOptions(changed, before)], [path])], path));
            Fill(path);
            SelectField(selectField);
            Announce(said, true);
        });
    }

    /// <summary>An entry's Title, from F2 or the Title field: <c>edit -t</c>, its path changing with it.</summary>
    protected override void RenameItem(ItemRow row, string title)
    {
        if (row.Tag is not PassNode entry || entry.IsGroup)
        {
            return;
        }

        if (PassTree.RefusedName(title) is { } why)
        {
            Announce(why, true);
            return;
        }

        var to = PassTree.Join(entry.Group, title);
        Do(async () =>
        {
            if (await Cli(KeePassCliTexts.Edit, [KeePassCliTexts.TitleOption, title], [entry.Path]) is null)
            {
                return;
            }

            if (_entries.Remove(entry.Path, out var fields))
            {
                _entries[to] = fields.WithTitle(title);
            }

            Remember(new Step("Renamed " + title, [new Call(KeePassCliTexts.Edit, [KeePassCliTexts.TitleOption, entry.Name], [to])], entry.Path));
            await ReloadAsync(to);
            Announce("Renamed " + title, true);
        });
    }

    /// <summary>F2 in the navigator (AX-8.7): an entry's title in place; a group is renamed in KeePassXC's own window.</summary>
    protected override string? RenameRefused(ItemRow row) =>
        row.Tag is PassNode { IsGroup: true } ? "A group is renamed in KeePassXC's own window: its tool has no command for it" : null;

    protected override string? RefusedName(ItemRow row, string name) => PassTree.RefusedName(name);

    // ---- the selection (bundle AX-8.16; SPEC.md PW-6.4a, framework step 10, 2026-11-22) ----

    /// <summary>Space selects entries: Delete then moves all the selected to the Recycle Bin, and one Ctrl+Z brings them back.</summary>
    protected override bool HasSelection => true;

    /// <summary>
    /// An entry outside the Recycle Bin can be selected, since that is what Delete moves there and Ctrl+Z brings
    /// back. A group cannot (only KeePassXC brings a group back, so it goes one at a time, after its question), and
    /// nothing in the Recycle Bin can (there Delete is for good, one at a time, after its question).
    /// </summary>
    protected override bool CanSelectRow(ItemRow row) => row.Tag is PassNode { IsGroup: false } node && !PassBin.Holds(Bin, node.Path);

    protected override string RowNoun => "entry";

    /// <summary>
    /// The level by the database, the group and whether it is locked: the status bar's second part is the database
    /// at every level. So the selection is given up when the group changes and when the window locks.
    /// </summary>
    protected override string LevelIdentity() => $"{_database}\n{_group}\n{(_password is null ? "locked" : "unlocked")}";

    /// <summary>
    /// The removal itself, of one entry or group or of several entries (PW-6.4, PW-6.4a; the review of 2026-11-27):
    /// one run of the tool for each, in the list's order, a run the tool refuses stopping the rest; then the
    /// database is listed again, and what became of each is read from the two listings (<see cref="PassBin.Outcome"/>),
    /// never taken from what was expected: moved, and to which group, or deleted for good. What is said is that
    /// (<see cref="PassBin.Said"/>), and Ctrl+Z gets the way back of the entries that are the one of their name in
    /// the Recycle Bin, and of no other. The group an entry arrived in is the Recycle Bin from then on.
    /// <paramref name="inBin"/>: what is removed was in the Recycle Bin, so that for good is what was asked for.
    /// </summary>
    private async Task RemoveAsync(IReadOnlyList<PassNode> nodes, bool inBin)
    {
        if (_tree is not { } before)
        {
            return;
        }

        var select = KeyAfter([.. nodes.Select(node => node.IsGroup ? GroupKey(node.Path) : node.Path)]);
        var removed = new List<PassNode>();
        foreach (var node in nodes)
        {
            if (await Cli(node.IsGroup ? KeePassCliTexts.RemoveGroup : KeePassCliTexts.Remove, null, [node.Path]) is null)
            {
                break;
            }

            _entries.Remove(node.Path);
            removed.Add(node);
        }

        if (removed.Count == 0 || await Cli(KeePassCliTexts.List, KeePassCliTexts.ListOptions) is not { } listed)
        {
            return;
        }

        var after = PassTree.Parse(listed.Output);
        var done = PassBin.Outcome(before, after, removed);
        _binSeen = done.FirstOrDefault(one => one.To is not null)?.To ?? _binSeen;
        var entries = done.Where(one => one.To is not null && !one.Node.IsGroup).ToList();
        var back = entries.Where(one => one.Sole).ToList();
        if (back.Count > 0)
        {
            Remember(new Step(PassBin.StepSaid(back),
                [.. back.Select(one => new Call(KeePassCliTexts.Move, [], [PassTree.Join(one.To!, one.Node.Name), one.Node.Group.Length == 0 ? KeePassCliTexts.Root : one.Node.Group]) { Sole = true })],
                back[0].Node.Path));
        }
        else if (entries.Count > 0)
        {
            // Moved, and not to be moved back by its name: Ctrl+Z says why, once, and passes over it.
            Remember(new Step(PassBin.StepSaid(entries), [], null, PassBin.NotSole(entries[0].Node.Name)));
        }

        Selection.Clear();
        _tree = after;
        Fill(select);
        Announce(PassBin.Said(done, nodes.Count, inBin, BundleKeys.Text(ItemKeys.Undo)), true);
    }

    /// <summary>
    /// Delete (PW-6.4, PW-6.4a). What it acts on is the item window's answer (<see cref="ItemForm.RowsInHand"/>,
    /// bundle AX-8.16): the selected entries whenever any is selected, one alone too and wherever the row you are on
    /// is; the row you are on when none is. (Until 2026-11-27 one selected entry was passed over for the row you are
    /// on.) What it does is as far as axPass knows (<see cref="PassBin.Fate"/>): an entry to the Recycle Bin at once,
    /// back with Ctrl+Z; a group after a question, since only KeePassXC brings a group back; in the Recycle Bin,
    /// for good after a question; and where the tool's output does not say which of these it will be, after a
    /// question that says so, Cancel its default. Two selected entries of one name are refused before anything is
    /// moved, since in the Recycle Bin their names are all the way back has to tell them apart.
    /// </summary>
    protected override void DeleteRow(ItemRow row)
    {
        if (_tree is not { } tree)
        {
            return;
        }

        var nodes = RowsInHand().Select(one => one.Tag).OfType<PassNode>().ToList();
        if (nodes.Count == 0)
        {
            return;
        }

        var bin = Bin;
        if (nodes.Count > 1)
        {
            if (nodes.GroupBy(entry => entry.Name, StringComparer.OrdinalIgnoreCase).FirstOrDefault(same => same.Count() > 1) is { } twice)
            {
                Announce($"Two of the selected entries are called {twice.Key}: move those to the Recycle Bin one at a time, so that {BundleKeys.Text(ItemKeys.Undo)} can tell them apart", true);
                return;
            }

            if (nodes.Any(entry => PassBin.Fate(_binOn, bin, entry) == DeleteFate.NotKnown))
            {
                var (title, text) = PassBin.Question(_binOn, nodes);
                ShowNotice(Notice.Ask(title, text, "&Delete", () => Do(() => RemoveAsync(nodes, inBin: false)), cancelIsDefault: true));
                return;
            }

            Do(() => RemoveAsync(nodes, inBin: false));
            return;
        }

        var node = nodes[0];
        if (node.Path == bin)
        {
            Announce("The Recycle Bin stays; KeePassXC empties it", true);
            return;
        }

        switch (PassBin.Fate(_binOn, bin, node))
        {
            case DeleteFate.Recycled when !node.IsGroup:
                Do(() => RemoveAsync(nodes, inBin: false));
                return;
            case DeleteFate.Recycled:
                ShowNotice(Notice.Ask("Move the group",
                    $"Move the group {node.Name} and its {Count(tree.CountIn(node.Path), "item")} to the Recycle Bin? {BundleKeys.Text(ItemKeys.Undo)} cannot bring a group back: KeePassXC's own window can.",
                    "&Move", () => Do(() => RemoveAsync(nodes, inBin: false)), cancelIsDefault: true));
                return;
            case DeleteFate.ForGood:
                ShowNotice(Notice.Ask("Delete for good",
                    $"Delete {node.Name} for good? It is in the Recycle Bin, and nothing brings it back.",
                    "&Delete", () => Do(() => RemoveAsync(nodes, inBin: true)), cancelIsDefault: true));
                return;
            default:
                var (unknownTitle, unknownText) = PassBin.Question(_binOn, nodes, node.IsGroup && tree.CountIn(node.Path) > 0 ? Count(tree.CountIn(node.Path), "item") : string.Empty);
                ShowNotice(Notice.Ask(unknownTitle, unknownText, "&Delete", () => Do(() => RemoveAsync(nodes, inBin: false)), cancelIsDefault: true));
                return;
        }
    }

    /// <summary>
    /// Ctrl+M (PW-6.5): the entry moved to another group chosen from a list of them, the top of the database first;
    /// <c>mv</c>, with the way back kept for Ctrl+Z.
    /// </summary>
    private void MoveEntry()
    {
        if (SelectedEntry() is not { } entry || _tree is null)
        {
            return;
        }

        const string TopName = "The top of the database";
        var bin = Bin;
        var groups = _tree.Nodes.Where(node => node.IsGroup && !PassBin.Holds(bin, node.Path) && node.Path != entry.Group).Select(node => node.Path).ToList();
        List<string> choices = entry.Group.Length == 0 ? groups : [TopName, .. groups];
        if (choices.Count == 0)
        {
            Announce($"There is no other group: {BundleKeys.Text(ItemKeys.NewList)} makes one", true);
            return;
        }

        ChooseFromList("Move " + entry.Name, $"Now in {(entry.Group.Length == 0 ? "the top of the database" : entry.Group)}.", "Group", choices.Select(choice => choice.Replace("/", ", ")).ToList(), 0, chosen =>
        {
            var group = choices[chosen] == TopName ? string.Empty : choices[chosen];
            if (_tree.HasEntry(PassTree.Join(group, entry.Name)))
            {
                Announce($"There is an entry called {entry.Name} there already", true);
                return;
            }

            Do(async () =>
            {
                var select = NeighbourKey();
                if (await Cli(KeePassCliTexts.Move, null, [entry.Path, group.Length == 0 ? KeePassCliTexts.Root : group]) is null)
                {
                    return;
                }

                var moved = PassTree.Join(group, entry.Name);
                if (_entries.Remove(entry.Path, out var fields))
                {
                    _entries[moved] = fields;
                }

                var where = group.Length == 0 ? "the top of the database" : group.Replace("/", ", ");
                Remember(new Step($"Moved {entry.Name} to {where}", [new Call(KeePassCliTexts.Move, [], [moved, entry.Group.Length == 0 ? KeePassCliTexts.Root : entry.Group])], entry.Path));
                await ReloadAsync(select);
                Announce($"Moved {entry.Name} to {where}", true);
            });
        });
    }

    /// <summary>Ctrl+N in the navigator: a new entry in the group shown, its title asked; no password until one is set.</summary>
    protected override void NewItem()
    {
        if (_password is null)
        {
            Announce("Locked: Enter unlocks", true);
            return;
        }

        var group = _group;
        AskText("New entry", group.Length == 0 ? "At the top of the database." : $"In {group}.", "Title:", "Title", string.Empty, "&Add", title => Do(async () =>
        {
            var path = PassTree.Join(group, title.Trim());
            if (await Cli(KeePassCliTexts.Add, [], [path]) is null)
            {
                return;
            }

            // Undone, the entry is removed, and where that leaves it in the Recycle Bin as the one of its name, deleted
            // there too (Call.Purge). Until 2026-11-27 the second run named "Recycle Bin/<title>", which, with an older
            // entry of that title in the bin, deleted the older one for good.
            Remember(new Step("Added " + title.Trim(), [new Call(KeePassCliTexts.Remove, [], [path]) { Purge = true }], null));
            await ReloadAsync(path);
            Announce($"Added {title.Trim()}; Tab, then Enter on Password sets its password", true);
        }), typed => PassTree.RefusedName(typed) ?? (_tree?.HasEntry(PassTree.Join(group, typed.Trim())) == true ? $"There is an entry called {typed.Trim()} here already" : null));
    }

    /// <summary>Ctrl+Shift+N: a new group in the group shown.</summary>
    protected override void NewList()
    {
        if (_password is null)
        {
            Announce("Locked: Enter unlocks", true);
            return;
        }

        var group = _group;
        AskText("New group", group.Length == 0 ? "At the top of the database." : $"In {group}.", "Name:", "Name", string.Empty, "&Make", name => Do(async () =>
        {
            var path = PassTree.Join(group, name.Trim());
            if (await Cli(KeePassCliTexts.MakeGroup, [], [path]) is null)
            {
                return;
            }

            Remember(new Step("Made " + name.Trim(), [new Call(KeePassCliTexts.RemoveGroup, [], [path]) { Purge = true }], null));
            await ReloadAsync(GroupKey(path));
            Announce("Made " + name.Trim(), true);
        }), typed => PassTree.RefusedName(typed) ?? (_tree?.HasGroup(PassTree.Join(group, typed.Trim())) == true ? $"There is a group called {typed.Trim()} here already" : null));
    }

    /// <summary>Ctrl+Z: the last step's runs, in order.</summary>
    protected override void Undo()
    {
        if (_undo.Count == 0)
        {
            Refuse("Nothing to undo");
            return;
        }

        // The step stays until its first run has succeeded (docs/axKit/repairs.json R-39): taken off first, a run the
        // tool refused at once ("Still working on the last command") lost the step, and the change could no longer be
        // undone. Once a run has gone through, the step is used up, whatever the later runs do.
        var step = _undo[^1];
        if (step.CannotUndo is { } why)
        {
            // Said once and passed over, so that the next Ctrl+Z reaches the change before it.
            _undo.Remove(step);
            Refuse($"Not undone: {step.Said}. {why}");
            return;
        }

        Do(async () =>
        {
            // PW-6.4: a run that reaches into the Recycle Bin by a name is made only for the one entry of that name
            // there, by a listing read now, since another may have joined it since the step was kept.
            if (step.Undo.Any(call => call.Sole))
            {
                if (await Cli(KeePassCliTexts.List, KeePassCliTexts.ListOptions) is not { } listed)
                {
                    return;
                }

                _tree = PassTree.Parse(listed.Output);
            }

            var begun = false;
            var left = new List<string>();
            foreach (var call in step.Undo)
            {
                if (call.Sole && _tree?.Count(call.Rest[0], isGroup: false) != 1)
                {
                    left.Add(PassTree.NodeOf(call.Rest[0], isGroup: false).Name);
                    continue;
                }

                var before = _tree;
                if (await Cli(call.Command, call.Options, call.Rest) is null)
                {
                    await ReloadAsync(SelectedKey());
                    return;
                }

                if (!begun)
                {
                    begun = true;
                    _undo.Remove(step);
                }

                if (call.Purge && before is not null)
                {
                    await PurgeAsync(before, call);
                }
            }

            _undo.Remove(step);
            if (!begun)
            {
                // Nothing could be told apart: said once and passed over, as a change the tool cannot take back.
                Fill(SelectedKey());
                Refuse($"Not undone: {step.Said}. {PassBin.NotSole(left[0])}");
                return;
            }

            _entries.Clear();
            if (step.Select is { } path && !path.EndsWith(KeePassCliTexts.PathSeparator) && await Cli(KeePassCliTexts.Show, null, [path]) is { } shown)
            {
                // The entry read again before the words, so that its fields are there for the next key.
                _entries[path] = EntryFields.Parse(shown.Output);
            }

            await ReloadAsync(step.Select ?? SelectedKey());
            Announce(left.Count == 0 ? "Undone: " + step.Said : $"Undone: {step.Said}, but for {string.Join(", ", left)}. {PassBin.NotSole(left[0])}", true);
        });
    }

    /// <summary>
    /// After a run that removed what a step made (<see cref="Call.Purge"/>): the database listed again, and where
    /// the thing now lies in the Recycle Bin as the one of its name, it is deleted there too. With the Recycle Bin
    /// off the removal was for good already; where another of its name lies in the bin it stays there, since the
    /// tool could not be told which of the two to delete.
    /// </summary>
    private async Task PurgeAsync(PassTree before, Call call)
    {
        if (await Cli(KeePassCliTexts.List, KeePassCliTexts.ListOptions) is not { } listed)
        {
            return;
        }

        var after = PassTree.Parse(listed.Output);
        _tree = after;
        var node = PassTree.NodeOf(call.Rest[0], call.Command == KeePassCliTexts.RemoveGroup);
        if (PassBin.Outcome(before, after, [node]) is [{ To: { } to, Sole: true }])
        {
            _binSeen = to;
            await Cli(call.Command, null, [PassTree.Join(to, node.Name)]);
        }
    }

    private void Remember(Step step)
    {
        _undo.Add(step);
        if (_undo.Count > MaxUndo)
        {
            _undo.RemoveAt(0);
        }
    }

    // ---- the password field (PW-6.2) ----

    /// <summary>Enter on the Password field: a new one typed or generated, asked twice when typed, set with <c>edit -p</c>; the field for anything else not KeePass's own is read only.</summary>
    protected override bool ChangeSpecial(ItemRow row, int index, ItemField field)
    {
        if (row.Tag is not PassNode entry)
        {
            return false;
        }

        if (field.Type != FieldTypes.Password)
        {
            if (EntryFields.IsStandard(field.Title))
            {
                return false;
            }

            Announce($"{field.Title} is changed in KeePassXC's own window", true);
            return true;
        }

        var generated = false;
        Notice? asked = null;
        asked = new Notice("New password for " + entry.Name, $"Type it, or Generate makes one of 20 characters. {BundleKeys.Text(ItemKeys.Undo)} does not bring the old one back.",
        [
            new OverlayChoice("&Set", () =>
            {
                var typed = Overlay.InputText;
                Overlay.SetInputText(string.Empty);
                if (generated)
                {
                    SetPassword(entry, typed);
                    return;
                }

                ShowNotice(new Notice("Type it again", "The same password once more.",
                [
                    new OverlayChoice("&Set", () =>
                    {
                        var again = Overlay.InputText;
                        Overlay.SetInputText(string.Empty);
                        if (again == typed)
                        {
                            SetPassword(entry, typed);
                        }
                        else
                        {
                            Announce("The two passwords differ: nothing changed", true);
                        }
                    }, IsDefault: true),
                    new OverlayChoice("&Cancel", IsCancel: true),
                ])
                {
                    Input = new OverlayInput("Password again:", $"Re-enter the password for {entry.Name}", string.Empty, "Type the password again", Hidden: true),
                });
            }, IsDefault: true),
            new OverlayChoice("&Generate", () => Do(async () =>
            {
                if (ToolPath() is not { } cli)
                {
                    return;
                }

                // The password goes into the field it was asked for and no other (PW-4.3; the review of
                // 2026-11-27): after a lock the field shown is the master password's (Tool drops the answer), and
                // after Escape and another command it may be one that shows what it holds.
                if (await Tool(() => KeePassCli.Run(cli, KeePassCliTexts.GenerateArguments, [])) is not { } result || !NoticeOpen || !ReferenceEquals(Overlay.Shown, asked))
                {
                    return;
                }

                if (result.Succeeded)
                {
                    Overlay.SetInputText(result.Output.Trim());
                    generated = true;
                    Announce("Generated a password of 20 characters; Enter sets it", true);
                }
                else
                {
                    Announce(result.Why, true);
                }
            }), StaysOpen: true),
            new OverlayChoice("&Cancel", IsCancel: true),
        ])
        {
            Input = new OverlayInput("New password:", $"Type a new password for {entry.Name}", string.Empty, "Type a password, or Generate", Hidden: true),
        };
        ShowNotice(asked);
        return true;
    }

    private void SetPassword(PassNode entry, string password) => Do(async () =>
    {
        if (await Cli(KeePassCliTexts.Edit, [KeePassCliTexts.PasswordPromptOption], [entry.Path], [password, password]) is not null)
        {
            Announce("Password set for " + entry.Name, true);
        }
    });

    // ---- the copies and the URL (PW-5.4, PW-7) ----

    /// <summary>Ctrl+B: the selected entry's user name on the clipboard (no secret, so axPass puts it there itself).</summary>
    private void CopyUserName()
    {
        if (SelectedEntry() is not { } entry)
        {
            return;
        }

        if (!_entries.TryGetValue(entry.Path, out var fields))
        {
            Announce("Reading the entry: press it again in a moment", true);
            return;
        }

        var user = EntryFields.ValueOf(fields, EntryFields.UserName);
        if (user.Length == 0)
        {
            Announce(entry.Name + " has no user name", true);
            return;
        }

        try
        {
            // Marked private as the password is: no copy in Windows clipboard history or axClip (S-15).
            PrivateClipboard.Copy(user);
            Announce("Copied the user name of " + entry.Name, true);
        }
        catch (System.Runtime.InteropServices.ExternalException ex)
        {
            Fail("The clipboard could not be written: " + ex.Message);
        }
    }

    /// <summary>The seconds a copied password stays on the clipboard (PW-5.4).</summary>
    private const int ClipboardSeconds = 30;

    /// <summary>
    /// Ctrl+C (PW-5.4, PW-D4; security audit S-15): the one password read from the tool at this moment (<c>show -s -a
    /// Password</c>), put on the clipboard marked private, so that Windows clipboard history, the cloud clipboard and
    /// axClip keep no copy, and cleared after 30 seconds, on lock and on close, if nothing else was copied meanwhile.
    /// The tool's own <c>clip</c> set no such marks on Windows.
    /// </summary>
    private void CopyPassword()
    {
        if (SelectedEntry() is not { } entry || _password is null)
        {
            return;
        }

        Do(async () =>
        {
            if (await Cli(KeePassCliTexts.Show, KeePassCliTexts.ShowValueOptions(EntryFields.Password), [entry.Path]) is not { } result)
            {
                return;
            }

            try
            {
                // Only the line end the tool adds: a password may end in spaces.
                _clipboardSequence = PrivateClipboard.Copy(result.Output.TrimEnd('\r', '\n'));
            }
            catch (System.Runtime.InteropServices.ExternalException ex)
            {
                Fail("The clipboard could not be written: " + ex.Message);
                return;
            }

            _clipboardTimer.Stop();
            _clipboardTimer.Start();
            Announce($"Copied the password of {entry.Name}; the clipboard clears in {ClipboardSeconds} seconds", true);
        });
    }

    /// <summary>The password copied by axPass taken off the clipboard, if it is still there.</summary>
    private void ClearCopiedPassword()
    {
        _clipboardTimer.Stop();
        if (_clipboardSequence is { } sequence)
        {
            _clipboardSequence = null;
            PrivateClipboard.ClearIfUnchanged(sequence);
        }
    }

    /// <summary>Ctrl+U: the entry's URL in the browser.</summary>
    private void OpenEntryUrl()
    {
        if (SelectedEntry() is not { } entry)
        {
            return;
        }

        if (_entries.TryGetValue(entry.Path, out var fields) && EntryFields.ValueOf(fields, EntryFields.Url) is { Length: > 0 } url)
        {
            // S-4: a web address only; a database from elsewhere could hold \\server\x.exe, file: or another scheme.
            if (EntryFields.WebAddress(url) is { } web)
            {
                OpenUrl(web);
            }
            else
            {
                Refuse($"Not opened: {entry.Name}'s URL is not a web address");
            }
        }
        else
        {
            Announce(_entries.ContainsKey(entry.Path) ? entry.Name + " has no URL" : "Reading the entry: press it again in a moment", true);
        }
    }

    /// <summary>The selected entry; "Select an entry" said otherwise, or "Locked".</summary>
    private PassNode? SelectedEntry()
    {
        if (_password is null)
        {
            Announce("Locked: Enter unlocks", true);
            return null;
        }

        if (SelectedRow() is { Tag: PassNode { IsGroup: false } entry })
        {
            return entry;
        }

        Refuse("Select an entry first");
        return null;
    }

    // ---- the database ----

    /// <summary>Ctrl+O: another database from the Windows file picker; this one locks first.</summary>
    private void ChooseDatabase()
    {
        using var dialog = new OpenFileDialog
        {
            Title = "Open a KeePass database",
            Filter = "KeePass databases (*.kdbx)|*.kdbx|All files (*.*)|*.*",
            InitialDirectory = _database is not null ? Path.GetDirectoryName(_database) : Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
        };
        if (ShowSystemDialog(dialog) != DialogResult.OK)
        {
            return;
        }

        OpenDatabase(dialog.FileName);
    }

    private void OpenDatabase(string path)
    {
        _unlock++;
        _password = null;
        _tree = null;
        _binOn = false;
        _binSeen = null;
        ClearCopiedPassword();
        _entries.Clear();
        _undo.Clear();
        _backedUp = false;
        _database = path;
        _group = string.Empty;
        Fill(select: null);
        ShowUnlock(null);
    }

    /// <summary>Session, New database: a file name from the Windows picker, then the master password twice; <c>db-create -p</c>.</summary>
    private void NewDatabase()
    {
        if (ToolPath() is not { } cli)
        {
            Announce("KeePassXC is not installed", true);
            return;
        }

        // Bundle AX-11.5: a new database is made by KeePassXC's tool too.
        if (!HasConsent(ConsentTarget.KeePassXC, NewDatabase, () => Refuse("Not connected to KeePassXC")))
        {
            return;
        }

        using var dialog = new SaveFileDialog
        {
            Title = "Make a KeePass database",
            Filter = "KeePass databases (*.kdbx)|*.kdbx",
            DefaultExt = "kdbx",
            OverwritePrompt = true,
            InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
        };
        if (ShowSystemDialog(dialog) != DialogResult.OK)
        {
            return;
        }

        var path = dialog.FileName;
        ShowNotice(new Notice("Master password for " + PassTree.DatabaseName(path), "The password that opens the database. Nothing brings it back if it is forgotten.",
        [
            new OverlayChoice("&Next", () =>
            {
                var first = Overlay.InputText;
                Overlay.SetInputText(string.Empty);
                ShowNotice(new Notice("Type it again", "The same master password once more.",
                [
                    new OverlayChoice("&Make", () =>
                    {
                        var again = Overlay.InputText;
                        Overlay.SetInputText(string.Empty);
                        if (again != first)
                        {
                            Announce("The two passwords differ: no database made", true);
                            return;
                        }

                        Do(async () =>
                        {
                            if (File.Exists(path))
                            {
                                File.Delete(path);
                            }

                            // The database is made either way; it becomes the window's only while nothing else was
                            // asked for meanwhile (another database opened, a lock): Tool gives no answer then.
                            if (await Tool(() => KeePassCli.Run(cli, KeePassCli.On(KeePassCliTexts.CreateDatabase, path, [KeePassCliTexts.PasswordPromptOption]), [first, first])) is not { } result)
                            {
                                return;
                            }

                            if (!result.Succeeded)
                            {
                                Fail(PassTree.DatabaseName(path) + " could not be made: " + result.Why);
                                return;
                            }

                            await UnlockAsync(path, first);
                        });
                    }, IsDefault: true),
                    new OverlayChoice("&Cancel", IsCancel: true),
                ])
                {
                    Input = new OverlayInput("Master password again:", $"Re-enter the master password for {PassTree.DatabaseName(path)}", string.Empty, "Type the master password again", Hidden: true),
                });
            }, IsDefault: true),
            new OverlayChoice("&Cancel", IsCancel: true),
        ])
        {
            Input = new OverlayInput("Master password:", $"Type a master password for the new database {PassTree.DatabaseName(path)}", string.Empty, "Type a master password", Hidden: true),
        });
    }

    /// <summary>Options, KeePassXC's tool: where <c>keepassxc-cli.exe</c> is, when KeePassXC is not where it installs.</summary>
    private void ChooseCli()
    {
        using var dialog = new OpenFileDialog
        {
            Title = "Where is keepassxc-cli.exe?",
            Filter = "keepassxc-cli.exe|keepassxc-cli.exe",
        };
        if (ShowSystemDialog(dialog) != DialogResult.OK)
        {
            return;
        }

        _settings.CliPath = dialog.FileName;
        SaveSettings();
        Announce("axPass uses " + dialog.FileName, true);
    }
}
