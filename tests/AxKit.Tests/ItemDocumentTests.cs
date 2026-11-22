using System.Text.Json.Nodes;
using AxKit.Core;

namespace AxKit.Tests;

/// <summary>
/// The item window's document: fields in memory, every change a new document. It has no file shape of its own since
/// 2026-11-21; the files are <c>FlatItem</c>'s and <c>ListConfig</c>'s (<c>FlatItemTests</c>, <c>ListConfigTests</c>).
/// </summary>
public class ItemDocumentTests
{
    private static readonly IReadOnlyList<ItemField> Template =
    [
        new("Done", FieldTypes.Check, JsonValue.Create(false), []),
        new("Start", FieldTypes.Date, JsonValue.Create(string.Empty), []),
        new("Due", FieldTypes.Date, JsonValue.Create(string.Empty), []),
    ];

    private static ItemDocument Sample() => ItemDocument.Of(
    [
        new("Title", FieldTypes.Text, JsonValue.Create("Fix the tap"), []),
        new("Done", FieldTypes.Check, JsonValue.Create(false), []),
        new("Due", FieldTypes.Date, JsonValue.Create("2026-10-30"), []),
        new("Priority", FieldTypes.Choice, JsonValue.Create("High"), ["High", "Normal", "Low"]),
    ]);

    [Fact]
    public void HoldsTheFieldsInOrder()
    {
        var document = Sample();
        Assert.Equal(["Title", "Done", "Due", "Priority"], document.Fields.Select(field => field.Title));
        Assert.Equal("Fix the tap", document.Title);
        Assert.Equal(["High", "Normal", "Low"], document.Fields[3].Options);
        Assert.Equal(2, document.IndexOf("due"));
        Assert.Equal(-1, document.IndexOf("Notes"));
    }

    [Fact]
    public void AChangeLeavesTheDocumentAsItWas()
    {
        var document = Sample();
        var changed = document.WithValue(1, JsonValue.Create(true));
        Assert.True(FieldTypes.IsTrue(changed.Fields[1].Value));
        Assert.False(FieldTypes.IsTrue(document.Fields[1].Value));
    }

    [Fact]
    public void AValueHandedInIsTheDocumentsOwn()
    {
        // A value may belong to a JSON document that changes later, or be handed to another one: the document keeps
        // a copy of its own.
        var value = JsonValue.Create("first");
        var holder = new JsonObject { ["value"] = value };
        var document = ItemDocument.Of([new ItemField("Title", FieldTypes.Text, holder["value"], [])]);
        holder["value"] = "second";
        Assert.Equal("first", document.Title);
        Assert.Null(document.Fields[0].Value!.Parent);
    }

    [Fact]
    public void ANewItemHasItsTitleThenTheTemplate()
    {
        var item = ItemDocument.NewItem("Call the plumber", Template);
        Assert.Equal(["Title", "Done", "Start", "Due"], item.Fields.Select(field => field.Title));
        Assert.Equal("Call the plumber", item.Title);
        // A template that names the Title does not give a second one.
        Assert.Equal(["Title", "Done"], ItemDocument.NewItem("x", [new("title", FieldTypes.Text, null, []), Template[0]]).Fields.Select(field => field.Title));
    }

    [Fact]
    public void ANewItemStartsWithTheTemplatesValues()
    {
        var item = ItemDocument.NewItem("Issue 7", [new("Kind", FieldTypes.Choice, JsonValue.Create("unlabelled"), ["Bug", "unlabelled"])]);
        Assert.Equal("unlabelled", FieldTypes.TextOf(item.Fields[item.IndexOf("Kind")].Value));
        Assert.Equal(["Bug", "unlabelled"], item.Fields[1].Options);
    }

    [Fact]
    public void FieldsAreAddedRemovedRenamedAndMoved()
    {
        var document = Sample();
        var added = document.WithField(0, "Notes", FieldTypes.LongText, []);
        Assert.Equal("Notes", added.Fields[1].Title);
        Assert.Equal(string.Empty, FieldTypes.TextOf(added.Fields[1].Value));
        Assert.Equal("Status", added.WithFieldTitle(1, "Status").Fields[1].Title);
        Assert.Equal(["Title", "Done", "Due", "Priority"], added.WithoutField(1).Fields.Select(field => field.Title));
        Assert.Equal("Due", document.WithFieldMoved(2, -1)!.Fields[1].Title);
        Assert.Null(document.WithFieldMoved(3, 1));
        Assert.Null(document.WithFieldMoved(0, -1));
        var choice = document.WithField(-1, "Size", FieldTypes.Choice, ["S", "M"]);
        Assert.Equal(["S", "M"], choice.Fields[^1].Options);
    }

    [Fact]
    public void ATitleIsSetOrPutFirst()
    {
        Assert.Equal("Mend the tap", Sample().WithTitle("Mend the tap").Title);
        var untitled = ItemDocument.Of([Template[0]]).WithTitle("New");
        Assert.Equal(["Title", "Done"], untitled.Fields.Select(field => field.Title));
    }

    [Fact]
    public void HoldsTellsAFieldThatMoved()
    {
        var document = Sample();
        var shown = document.Fields[2];
        Assert.True(document.Holds(2, shown));
        Assert.False(document.WithFieldMoved(2, -1)!.Holds(2, shown));
        Assert.False(document.Holds(9, shown));
    }
}
