using System.Text.Json.Nodes;
using AxKit.Core;
using AxTask.Core;

namespace AxTask.Tests;

/// <summary>A command on several selected items (docs/axTask/SPEC.md TK-4.7; bundle AX-8.16): what Done makes of them, and the words.</summary>
public sealed class ManyItemsTests
{
    private static ItemDocument Task(bool done) =>
        ItemDocument.NewItem("A task", [new ItemField("Done", FieldTypes.Check, JsonValue.Create(done), [])]);

    [Fact]
    public void Done_on_several_tasks_checks_them_while_any_is_not_done_and_unchecks_only_when_all_are()
    {
        Assert.True(ManyItems.DoneNow([Task(false), Task(false)]));
        Assert.True(ManyItems.DoneNow([Task(true), Task(false)]));
        Assert.False(ManyItems.DoneNow([Task(true), Task(true)]));
    }

    [Fact]
    public void The_words_count_what_changed_and_what_was_passed_over()
    {
        Assert.Equal("Done: 3 tasks", ManyItems.DoneSaid(now: true, changed: 3, already: 0, notTasks: 0));
        Assert.Equal("Not done: 1 task", ManyItems.DoneSaid(now: false, changed: 1, already: 0, notTasks: 0));
        Assert.Equal("Done: 2 tasks. 1 was done already.", ManyItems.DoneSaid(now: true, changed: 2, already: 1, notTasks: 0));
        Assert.Equal("Done: 1 task. 2 were done already. 1 selected item is not a task.", ManyItems.DoneSaid(now: true, changed: 1, already: 2, notTasks: 1));
        Assert.Equal("Not done: 2 tasks. 3 selected items are not tasks.", ManyItems.DoneSaid(now: false, changed: 2, already: 0, notTasks: 3));
        Assert.Equal("1 item", ManyItems.Items(1));
        Assert.Equal("4 items", ManyItems.Items(4));
    }
}
