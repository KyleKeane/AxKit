using System.IO.Pipes;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using AxKit.Core;
using AxKit.Core.Mcp;

namespace AxKit.Forms;

/// <summary>
/// A window's end of the AI tools' access (docs/axKit/mcp.md MC-6.2 to MC-6.5): one pipe per window,
/// <c>AxKit-mcp-&lt;user SID&gt;-&lt;random&gt;</c>, for the current user alone and never over the network. A connection is
/// one request: the window checks that the client runs this very program file (the broker) and that the bundle's
/// setting is on, sends a fresh challenge, takes the HMAC answer under the key, then reads one line and answers it on
/// its own thread: <c>hello</c> (the app and the title), <c>state</c>, <c>list</c>, <c>text</c>, <c>commands</c>,
/// <c>said</c>, <c>pair</c>, which asks the user, or <c>run</c>, a command of the window's list run as its menu item would while the user allows it (MC-7), or <c>app</c>, a typed data tool of the window's app answered by its own code (MC-8); nothing here types a key or answers a notice. The
/// window of an app that is never read (axPass, axClip, <see cref="McpTools.NeverRead"/>) serves no pipe.
/// </summary>
internal sealed class McpChannel
{
    /// <summary>The start of every window's pipe name for this user: the broker lists the pipes by it.</summary>
    public static string Prefix => $"AxKit-mcp-{UserPipe.User().Value}-";

    /// <summary>The longest text a read gives (MC-3.5): 1 MB of characters.</summary>
    public const int MaxText = 1_000_000;

    /// <summary>How long a pairing notice waits for the user before the call is refused (MC-6.4).</summary>
    public static readonly TimeSpan PairingWait = TimeSpan.FromMinutes(2);

    private static readonly JsonSerializerOptions Plain = new() { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping };

    /// <summary>A run's answer (MC-7.1) without the parts that do not apply.</summary>
    private static readonly JsonSerializerOptions RunAnswer = new() { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping, DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull };

    private readonly AxKitForm _form;
    private readonly string _name = Prefix + Convert.ToHexStringLower(RandomNumberGenerator.GetBytes(8));

    /// <summary>What the window announced lately, the last 50, taken by <c>said</c>; recorded only from the first call a client proved the key for, so that nothing said before any access is handed over.</summary>
    private readonly Queue<(DateTime At, string Text)> _said = new();
    private readonly object _gate = new();
    private volatile bool _recording;

    /// <summary>What the window says while a run's answer waits for it (MC-7.1), null between runs; and when it last said something.</summary>
    private List<string>? _heard;
    private long _lastHeardTick;

    /// <summary>How long a request may wait for the window's thread before the call is refused: a busy or hung window never holds the pipe for good.</summary>
    private static readonly TimeSpan WindowWait = TimeSpan.FromSeconds(10);

    private McpChannel(AxKitForm form)
    {
        _form = form;
        Announcer.Raised += (text, _) =>
        {
            if (!_recording)
            {
                return;
            }

            lock (_gate)
            {
                if (_heard is { } heard)
                {
                    heard.Add(text);
                    _lastHeardTick = Environment.TickCount64;
                }

                _said.Enqueue((DateTime.UtcNow, text));
                while (_said.Count > 50)
                {
                    _said.Dequeue();
                }
            }
        };
    }

    /// <summary>Starts the window's pipe, unless its app is never read; the setting is asked at each connection, so the pipe is there either way and answers nothing while it is off.</summary>
    public static void Start(AxKitForm form, string appVerb)
    {
        if (McpTools.NeverRead.Contains(appVerb))
        {
            return;
        }

        var channel = new McpChannel(form);
        var thread = new Thread(channel.Serve) { IsBackground = true, Name = "AxKit MCP" };
        thread.Start();
    }

    private void Serve()
    {
        var failures = 0;
        while (!_form.IsDisposed)
        {
            try
            {
                // Outside call, pipe, AI tools: the window's own named pipe, which only this Windows account may open,
                // where the MCP server asks the window what it shows while the AI tools are on; each connection must
                // answer the challenge with the paired key.
                using var pipe = NamedPipeServerStreamAcl.Create(_name, PipeDirection.InOut, 1, PipeTransmissionMode.Byte, PipeOptions.None, 0, 0, UserPipe.Security());
                pipe.WaitForConnection();
                failures = 0;
                Answer(pipe);
            }
            catch (UnauthorizedAccessException ex)
            {
                // The name taken by another program first: this window serves no pipe; the window goes on.
                Log.Error("The AI tools' pipe was not opened: its name is held by another program", ex);
                return;
            }
            catch (Exception ex)
            {
                // Never out of this thread: an exception escaping a background thread ends the process, and the
                // window with it (an editor's unsaved text). The pipe is made again after a pause.
                if (ex is not (IOException or ObjectDisposedException))
                {
                    Log.Error("The AI tools' pipe failed", ex);
                }

                if (_form.IsDisposed || ++failures > 20)
                {
                    return;
                }

                Thread.Sleep(Math.Min(2000, 50 * failures));
            }
        }
    }

    /// <summary>One connection: who it is, the setting, the challenge, one request, one answer.</summary>
    private void Answer(NamedPipeServerStream pipe)
    {
        // MC-6.3: the broker is this very program file; anything else is let go without a word.
        if (UserPipe.ClientOf(pipe) is not { } client || !ThisProgram.Runs(client))
        {
            Log.Info("AI tools' pipe: a client that is not axKit was refused");
            return;
        }

        using var reader = new StreamReader(pipe, new UTF8Encoding(false));
        using var writer = new StreamWriter(pipe, new UTF8Encoding(false)) { AutoFlush = true };
        if (!McpAccess.Enabled)
        {
            // S-49: nothing heard while access is off is kept for a later client.
            _recording = false;
            lock (_gate)
            {
                _said.Clear();
            }

            writer.WriteLine(JsonSerializer.Serialize(new { error = "axKit's AI tools access is off: axKit menu, AI tools, Let AI tools read and open axKit." }, Plain));
            return;
        }

        if (McpAccess.ReadKey() is not { } key)
        {
            writer.WriteLine(JsonSerializer.Serialize(new { error = "axKit has no key for AI tools: turn the access off and on again." }, Plain));
            return;
        }

        // Both ends prove the key: the broker first, to this challenge; then the window, to the broker's, so that a
        // pipe of another user's copy of AxKit.exe cannot pass itself off as this user's window.
        var challenge = McpAccess.NewChallenge();
        writer.WriteLine(JsonSerializer.Serialize(new { challenge }, Plain));
        if (!McpAccess.Proves(key, challenge, UserPipe.ReadLine(reader)))
        {
            Log.Info("AI tools' pipe: a client without the key was refused");
            return;
        }

        if (UserPipe.ReadLine(reader) is not { Length: > 0 } theirs)
        {
            return;
        }

        writer.WriteLine(McpAccess.Proof(key, theirs));
        if (UserPipe.ReadLine(reader) is not { } line)
        {
            return;
        }

        _recording = true;

        var space = line.IndexOf(' ');
        var verb = space < 0 ? line : line[..space];
        var argument = space < 0 ? string.Empty : line[(space + 1)..];
        string answer;
        try
        {
            answer = verb switch
            {
                "pair" => Pair(argument),
                "run" => Run(argument),
                "app" => App(argument),
                _ => OnWindow(() => Execute(verb)),
            };
        }
        catch (Exception ex)
        {
            // Whatever went wrong is the client's answer, never the end of the window's process.
            Log.Error("The AI tools' request " + verb + " failed", ex);
            answer = JsonSerializer.Serialize(new { error = "The window could not answer." }, Plain);
        }

        writer.WriteLine(answer);
    }


    /// <summary>Runs on the window's thread and waits at most <see cref="WindowWait"/>; a window that does not answer in time refuses the call.</summary>
    private T OnWindow<T>(Func<T> read)
    {
        var call = _form.BeginInvoke(read);
        if (!call.AsyncWaitHandle.WaitOne(WindowWait))
        {
            throw new TimeoutException("The window did not answer in time");
        }

        return (T)_form.EndInvoke(call)!;
    }

    private string Execute(string verb)
    {
        object result = verb switch
        {
            "hello" => new { app = _form.AppNameForTools, title = _form.Text, handle = (long)_form.Handle },
            "state" => _form.McpState(),
            "list" => List(),
            "text" => Text(),
            "commands" => _form.CommandList.Select(command => new
            {
                label = command.Label.Replace("&", string.Empty, StringComparison.Ordinal),
                menu = _form.MenuOf(command),
                key = command.Key == Keys.None ? null : BundleKeys.Text(command.Key).TrimEnd('.'),
                mark = command.Mark.ToString().ToLowerInvariant(),
            }).ToArray(),
            "said" => Said(),
            _ => new { error = $"unknown request {verb}" },
        };
        return JsonSerializer.Serialize(result, Plain);
    }

    private object List()
    {
        var focused = Control.FromChildHandle(User32.GetFocus());
        if ((focused is ListBox own && own.FindForm() == _form ? own : WindowReading.FirstVisibleList(_form)) is not { } list)
        {
            return new { error = "The window shows no list." };
        }

        return new { name = list.AccessibleName, items = list.Items.Cast<object>().Take(5000).Select(item => item.ToString()).ToArray(), selected = list.SelectedIndex, count = list.Items.Count };
    }

    private object Text()
    {
        var focused = Control.FromChildHandle(User32.GetFocus());
        if ((focused is TextBoxBase own && own.FindForm() == _form ? own : WindowReading.FirstVisibleText(_form)) is not { } box)
        {
            return new { error = "The window shows no text." };
        }

        // MC-4.3: a hidden field never leaves the window.
        if (WindowReading.IsSecret(box))
        {
            return new { name = box.AccessibleName, text = "protected", caretLine = 0 };
        }

        var text = box.Text;
        return new
        {
            name = box.AccessibleName,
            text = text.Length > MaxText ? text[..MaxText] : text,
            truncated = text.Length > MaxText,
            caretLine = WindowReading.LogicalLine(box),
        };
    }

    private object Said()
    {
        // S-49 (security review, 2026-10-25): only what was said since the setting last changed, so that what a window
        // heard while access was off, or before this client was paired, is never handed over, in any axKit process.
        var since = McpAccess.ChangedAtUtc;
        lock (_gate)
        {
            var said = _said.Where(entry => entry.At > since).Select(entry => entry.Text).ToArray();
            _said.Clear();
            return said;
        }
    }

    /// <summary>How long a run's answer waits for what the window says (MC-7.1), and the quiet after the last words that ends the wait sooner.</summary>
    private const int RunListenMs = 2500;

    private const int RunQuietMs = 700;

    /// <summary>
    /// MC-7.1: <c>axkit_window_run</c>. The window checks and posts the command on its thread (<see cref="AxKitForm.McpRunCommand"/>);
    /// this thread then listens to what the window says until it has been quiet for a moment, at most
    /// <see cref="RunListenMs"/>, and reads whether a notice or a Windows dialog now waits for the user. The answer
    /// names what ran, or that the user is asked (an act, MC-7.3), what was said, and the question the window asks.
    /// </summary>
    private string Run(string argument)
    {
        var request = System.Text.Json.Nodes.JsonNode.Parse(argument);
        var name = McpProtocol.Text(request?["command"]);
        var menu = McpProtocol.Text(request?["menu"]);
        var client = McpProtocol.Text(request?["client"]) ?? "An AI tool";
        lock (_gate)
        {
            _heard = [];
            _lastHeardTick = Environment.TickCount64;
        }

        try
        {
            // MC-7.6: the sealed switch read here, on the pipe's thread, so that the window's thread never waits on the file
            // (bundle rule 3); the window refuses when it is off.
            var allowed = McpAccess.RunsCommands;
            var (error, label, asked, ticket) = OnWindow(() => _form.McpRunCommand(name, menu, client, allowed));
            if (error is not null)
            {
                return JsonSerializer.Serialize(new { error }, RunAnswer);
            }

            var start = Environment.TickCount64;
            while (Environment.TickCount64 - start < RunListenMs && !_form.IsDisposed)
            {
                Thread.Sleep(100);
                lock (_gate)
                {
                    if (_heard!.Count > 0 && Environment.TickCount64 - _lastHeardTick >= RunQuietMs)
                    {
                        break;
                    }
                }
            }

            string[] said;
            lock (_gate)
            {
                said = [.. _heard!];
            }

            if (_form.IsDisposed)
            {
                return JsonSerializer.Serialize(new { said, closed = "The window closed; whether the command ran is not known." }, RunAnswer);
            }

            var (notice, noticeText, dialog, ran, notRun) = OnWindow(() => _form.McpAfterRun(ticket));
            return JsonSerializer.Serialize(new
            {
                // "ran" only for a run the window took up and ran (MC-7.1): never for one that a notice or the switch stopped.
                ran = !asked && ran == true ? label : null,
                notRun = !asked && ran == false ? $"{label} did not run: {notRun}." : null,
                unknown = !asked && ran is null ? $"Whether {label} ran is not known yet: axkit_window_said tells." : null,
                asked = asked ? $"The window asks the user whether to run {label}; the user answers there, and axkit_window_said tells what came of it." : null,
                said,
                notice,
                noticeText,
                dialog = dialog ? "The window shows the user a Windows dialog." : null,
            }, RunAnswer);
        }
        catch (InvalidOperationException) when (_form.IsDisposed)
        {
            return JsonSerializer.Serialize(new { closed = "The window closed; whether the command ran is not known." }, RunAnswer);
        }
        finally
        {
            lock (_gate)
            {
                _heard = null;
            }
        }
    }

    /// <summary>
    /// MC-8: a typed data tool of the window's app, answered by the window's own code on its thread
    /// (<see cref="AxKitForm.McpApp"/>). The switch is read here, off the window's thread, as for a run (MC-7.6); the
    /// window refuses a change while it is off.
    /// </summary>
    private string App(string argument)
    {
        var request = System.Text.Json.Nodes.JsonNode.Parse(argument);
        var tool = McpProtocol.Text(request?["tool"]) ?? string.Empty;
        var arguments = request?["arguments"] as System.Text.Json.Nodes.JsonObject ?? [];
        var client = McpProtocol.Text(request?["client"]) ?? "An AI tool";
        var allowed = McpAccess.RunsCommands;
        var answer = OnWindow(() => _form.McpApp(tool, arguments, client, allowed));
        return JsonSerializer.Serialize(answer, RunAnswer);
    }

    /// <summary>
    /// MC-6.4: the pairing notice, shown on the window's thread; this pipe thread waits up to two minutes for Allow or
    /// Deny. The notice is unprompted (the chime, and a moment in which keys do nothing), Deny is its default, and the
    /// window flashes on the taskbar when it is not in front.
    /// </summary>
    private string Pair(string argument)
    {
        var request = System.Text.Json.Nodes.JsonNode.Parse(argument);
        var name = McpProtocol.Text(request?["name"]) ?? "An AI tool";
        var program = McpProtocol.Text(request?["program"]) ?? string.Empty;
        var answer = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        // A window with a notice open shows no other: a new notice would replace the one the user is answering.
        if (!OnWindow(() => _form.AskToPairMcp(name, program, answer)))
        {
            return JsonSerializer.Serialize(new { allowed = false, busy = true }, Plain);
        }

        if (!answer.Task.Wait(PairingWait))
        {
            // Unanswered: refused, and the notice taken away, so that a later Allow cannot pair a call that is gone.
            answer.TrySetResult(false);
            _form.BeginInvoke(() => _form.EndMcpPairing(name));
        }

        return JsonSerializer.Serialize(new { allowed = answer.Task.Result }, Plain);
    }
}
