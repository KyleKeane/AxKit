using System.Collections;
using System.Text;
using AxKit.Core;
using AxKit.Core.Pty;

namespace AxKit.Tests;

/// <summary>
/// The pseudo console host (ConPTY): a command runs, its output arrives, its exit code comes back. The one check that
/// the handle dance still works after a .NET or Windows update; the fixtures were recorded through it.
/// </summary>
public sealed class PtyHostTests
{
    [Fact]
    public async Task A_command_runs_in_the_pseudo_console_and_its_exit_is_reported()
    {
        var output = new StringBuilder();
        var exited = new TaskCompletionSource<int>();
        var environment = new Dictionary<string, string>();
        foreach (DictionaryEntry entry in Environment.GetEnvironmentVariables())
        {
            environment[(string)entry.Key] = (string?)entry.Value ?? string.Empty;
        }

        using var host = PtyHost.Start(
            new PtyOptions($"\"{SystemPrograms.Cmd}\" /d /c echo pseudo-console-ok& exit 7", Path.GetTempPath(), 80, 25, environment),
            data =>
            {
                lock (output)
                {
                    output.Append(Encoding.UTF8.GetString(data));
                }
            },
            code => exited.TrySetResult(code));
        var code = await exited.Task.WaitAsync(TimeSpan.FromSeconds(30));
        Assert.Equal(7, code);

        // The last bytes may still be on their way when the exit is reported.
        for (var i = 0; i < 50; i++)
        {
            lock (output)
            {
                if (output.ToString().Contains("pseudo-console-ok", StringComparison.Ordinal))
                {
                    return;
                }
            }

            await Task.Delay(100);
        }

        lock (output)
        {
            Assert.Contains("pseudo-console-ok", output.ToString());
        }
    }
}