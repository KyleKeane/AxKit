using AxKit.Core;
using AxKit.TestShared;
using AxMCP.Core;

namespace AxMCP.Tests;

/// <summary>The consent before a server starts is for the whole of what would run (SPEC.md MP-2.1a; repairs.md R-61).</summary>
public sealed class ServerConsentTests
{
    private const string Program = @"C:\Program Files\nodejs\node.exe";
    private const string Folder = @"C:\work";

    private static McpServer Server(string name = "github", string[]? args = null, Dictionary<string, string>? env = null) =>
        new(name, ServerKind.Stdio, "node", args ?? ["server.js", "--port", "1"], env ?? new Dictionary<string, string> { ["API_TOKEN"] = "secret-value-1", ["MODE"] = "fast" }, null, "Claude Code, user scope", null, Own: false);

    [Fact]
    public void An_agreement_counts_for_the_same_definition_and_for_no_other()
    {
        using var temp = new TempFolder("axmcp-consent");
        var file = Path.Combine(temp.Path, "consent.json");
        Consent.Record(file, "axMCP", ServerConsent.For(Server(), Program, Folder).Program, "1", new DateTime(2026, 11, 16));

        Assert.True(Consent.Given(file, "axMCP", ServerConsent.For(Server(), Program, Folder).Program));
        // Another program of the same name, another argument, another value of a variable, another variable, another folder, another name.
        Assert.False(Consent.Given(file, "axMCP", ServerConsent.For(Server(), @"C:\elsewhere\node.exe", Folder).Program));
        Assert.False(Consent.Given(file, "axMCP", ServerConsent.For(Server(args: ["server.js", "--port", "2"]), Program, Folder).Program));
        Assert.False(Consent.Given(file, "axMCP", ServerConsent.For(Server(args: ["SERVER.js", "--port", "1"]), Program, Folder).Program));
        Assert.False(Consent.Given(file, "axMCP", ServerConsent.For(Server(env: new() { ["API_TOKEN"] = "secret-value-2", ["MODE"] = "fast" }), Program, Folder).Program));
        Assert.False(Consent.Given(file, "axMCP", ServerConsent.For(Server(env: new() { ["API_TOKEN"] = "secret-value-1" }), Program, Folder).Program));
        Assert.False(Consent.Given(file, "axMCP", ServerConsent.For(Server(), Program, @"C:\other").Program));
        Assert.False(Consent.Given(file, "axMCP", ServerConsent.For(Server(name: "other"), Program, Folder).Program));
    }

    [Fact]
    public void Arguments_cannot_be_moved_between_each_other_under_one_mark()
    {
        Assert.NotEqual(
            ServerConsent.Mark(Server(args: ["a b", "c"]), Program, Folder),
            ServerConsent.Mark(Server(args: ["a", "b c"]), Program, Folder));
        // The order the definition lists its variables in is not a difference.
        Assert.Equal(
            ServerConsent.Mark(Server(env: new() { ["A"] = "1", ["B"] = "2" }), Program, Folder),
            ServerConsent.Mark(Server(env: new() { ["B"] = "2", ["A"] = "1" }), Program, Folder));
    }

    [Fact]
    public void The_notice_reads_the_whole_command_line_the_folder_and_the_variables_names_and_never_a_value()
    {
        var target = ServerConsent.For(Server(), Program, Folder);
        Assert.Contains("\"C:\\Program Files\\nodejs\\node.exe\" server.js --port 1", target.Work);
        Assert.Contains(Folder, target.Work);
        Assert.Contains("API_TOKEN, MODE", target.Work);
        Assert.Contains("API_TOKEN, MODE", target.Program);
        Assert.Equal("the MCP server github", target.Name);
        foreach (var text in new[] { target.Program, target.Work, target.Name })
        {
            Assert.DoesNotContain("secret-value-1", text);
            Assert.DoesNotContain("fast", text);
        }

        Assert.Contains("sets no environment variables", ServerConsent.For(Server(env: []), Program, Folder).Work);
        Assert.Contains("no environment variables", ServerConsent.For(Server(env: []), Program, Folder).Program);
    }
}
