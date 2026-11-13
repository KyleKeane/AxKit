using AxKit.Core;
using AxKit.Core.Mcp;

namespace AxKit.Tests;

/// <summary>A test run's AI tools' pipes carry the run's mark, and a broker finds only the pipes of its own kind (McpPipeName, 2026-11-12).</summary>
public class McpPipeNameTests
{
    private const string Random = "0123456789abcdef";

    [Fact]
    public void The_users_broker_finds_the_users_windows_and_no_run_s()
    {
        var mark = AppPaths.MarkOf(@"C:\Temp\axkit-automate\1234\home");
        Assert.Equal(Random, McpPipeName.Suffix(null, Random));
        Assert.True(McpPipeName.SameKind(McpPipeName.Suffix(null, Random), null));
        Assert.False(McpPipeName.SameKind(McpPipeName.Suffix(mark, Random), null));
        Assert.False(McpPipeName.SameKind("not-a-pipe-of-ours", null));
    }

    [Fact]
    public void A_runs_broker_finds_its_own_run_s_windows_alone()
    {
        var mark = AppPaths.MarkOf(@"C:\Temp\axkit-automate\1234\home");
        var other = AppPaths.MarkOf(@"C:\Temp\axkit-automate\5678\home");
        Assert.NotEqual(mark, other);
        Assert.Equal(mark, AppPaths.MarkOf(@"c:\temp\AXKIT-automate\1234\HOME"));
        Assert.Equal(12, mark.Length);
        var suffix = McpPipeName.Suffix(mark, Random);
        Assert.EndsWith(Random, suffix, StringComparison.Ordinal);
        Assert.True(McpPipeName.SameKind(suffix, mark));
        Assert.False(McpPipeName.SameKind(suffix, other));
        Assert.False(McpPipeName.SameKind(Random, mark));
    }
}
