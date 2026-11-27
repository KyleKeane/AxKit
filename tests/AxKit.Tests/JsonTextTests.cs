using System.Text.Json;
using System.Text.Json.Nodes;
using AxKit.Core;
using AxKit.TestShared;

namespace AxKit.Tests;

/// <summary>
/// <see cref="JsonText"/>, the one reader of JSON a person may have written by hand, and the rule that holds the
/// sources to it. .NET's <c>JsonNode.Parse</c> lets a file through that names a member twice, and the object then
/// throws an <c>ArgumentException</c> at whoever asks it for a member first: no reader of a file catches that kind,
/// so one such item file made every fill of its list fail (found 2026-11-27).
/// </summary>
public sealed class JsonTextTests
{
    [Fact]
    public void What_dotnet_does_with_a_member_named_twice_is_why_this_reader_is_there()
    {
        // Parsed without a word, and the failure comes later, as another kind, from an innocent line.
        var node = JsonNode.Parse("""{ "Title": "One", "Title": "Two" }""")!;
        Assert.Throws<ArgumentException>(() => node["Title"]);
    }

    [Theory]
    [InlineData("""{ "Title": "One", "Title": "Two" }""")]
    [InlineData("""{ "a": { "b": 1, "b": 2 } }""")]
    [InlineData("""[ { "b": 1, "b": 2 } ]""")]
    public void A_member_named_twice_is_refused_at_once_wherever_it_is(string text)
    {
        var refused = Assert.ThrowsAny<JsonException>(() => JsonText.Parse(text));
        Assert.Equal(JsonText.Twice, JsonText.Why(text, refused));
        Assert.ThrowsAny<JsonException>(() => JsonText.Parse(text, lenient: true));
    }

    [Fact]
    public void Text_that_is_not_JSON_says_its_line_and_the_lenient_reading_takes_a_comma_and_a_comment()
    {
        const string broken = "{\r\n  \"a\": 1,\r\n  nope\r\n}";
        var refused = Assert.ThrowsAny<JsonException>(() => JsonText.Parse(broken));
        Assert.Equal("it is not JSON at line 3", JsonText.Why(broken, refused));

        const string byHand = """{ "a": 1, /* a note */ "b": [1, 2,], }""";
        Assert.ThrowsAny<JsonException>(() => JsonText.Parse(byHand));
        Assert.Equal(2, ((JsonObject)JsonText.Parse(byHand, lenient: true)!).Count);
        Assert.Equal("1", JsonText.Parse("""{ "a": 1 }""")!["a"]!.ToJsonString());
    }

    /// <summary>
    /// The files that still call <c>JsonNode.Parse</c> themselves, each with why. A ratchet: a file not named here
    /// reads its JSON through <see cref="JsonText"/>, and a file named here that no longer calls it comes off the
    /// list in the same commit. The ones marked "not looked at" read what a program wrote or a settings file, and
    /// whether a member named twice can reach them unhandled is open work, not a decision.
    /// </summary>
    private static readonly Dictionary<string, string> StillOwnParse = new()
    {
        ["src/AxKit.Core/JsonText.cs"] = "the reader itself",
        ["src/AxKit.Core/ListOptions.cs"] = "parses values written in its own source, never a file",
        ["src/AxClaude.Core/ClaudeCodeSettings.cs"] = "refuses a member named twice with its own options (AllowDuplicateProperties = false)",
        ["src/AxKit.Core/SettingsFile.cs"] = "not looked at (the settings' batch): names are read without case there, which JsonText does not do",
        ["src/AxKit.Core/Theme.cs"] = "not looked at (the settings' batch)",
        ["src/AxKit.Core/Mcp/McpAccess.cs"] = "not looked at (the AI tools' batch): a program's answer",
        ["src/AxKit.Core/Mcp/McpAudit.cs"] = "not looked at (the AI tools' batch): a program's answer",
        ["src/AxKit.Core/Mcp/McpProtocol.cs"] = "not looked at (the AI tools' batch): a program's message",
        ["src/AxKit.Forms/McpBroker.cs"] = "not looked at (the AI tools' batch): a program's message",
        ["src/AxKit.Forms/McpChannel.cs"] = "not looked at (the AI tools' batch): a program's message",
        ["src/AxMCP.Core/ArgumentFields.cs"] = "not looked at (axMCP's batch)",
        ["src/AxMCP.Core/McpClient.cs"] = "not looked at (axMCP's batch): a program's message",
        ["src/AxMCP.Core/ServerSources.cs"] = "not looked at (axMCP's batch)",
        ["src/AxAutomate.Core/Broker.cs"] = "not looked at (axAutomate's batch): a program's message",
        ["src/AxAutomate.Core/WorkflowFile.cs"] = "not looked at (axAutomate's batch)",
        ["src/AxAutomate.Core/WorkflowRecord.cs"] = "not looked at (axAutomate's batch)",
    };

    [Fact]
    public void A_file_reads_JSON_through_JsonText_or_is_on_the_list_of_those_that_do_not_yet()
    {
        var root = SourceTexts.RepositoryRoot();
        var calling = new List<string>();
        foreach (var file in Directory.GetFiles(Path.Combine(root, "src"), "*.cs", SearchOption.AllDirectories))
        {
            var relative = Path.GetRelativePath(root, file).Replace(Path.DirectorySeparatorChar, '/');
            if (relative.Contains("/obj/", StringComparison.Ordinal) || relative.Contains("/bin/", StringComparison.Ordinal))
            {
                continue;
            }

            if (File.ReadAllText(file).Contains("JsonNode.Parse(", StringComparison.Ordinal))
            {
                calling.Add(relative);
            }
        }

        var unlisted = calling.Where(file => !StillOwnParse.ContainsKey(file)).ToList();
        Assert.True(unlisted.Count == 0, "These files call JsonNode.Parse themselves; read the text with AxKit.Core.JsonText.Parse, which refuses a member named twice as a JsonException:\n" + string.Join("\n", unlisted));
        var gone = StillOwnParse.Keys.Where(file => !calling.Contains(file)).ToList();
        Assert.True(gone.Count == 0, "These files no longer call JsonNode.Parse: take them off StillOwnParse:\n" + string.Join("\n", gone));
    }
}
