// EdgeDrive: one command per run against a Microsoft Edge started with --remote-debugging-port (docs/axkit/store-setup.md
// section 6). The current page target is kept in a small file, so that a sequence of runs works on one tab.
//
//   EdgeDrive targets                       the open pages: id, title, address
//   EdgeDrive open <url>                    a new tab on the address, made the current target
//   EdgeDrive use <id>                      another tab as the current target
//   EdgeDrive nav <url>                     the current tab to the address, waiting for the load
//   EdgeDrive eval <javascript>             the expression's value as JSON (a promise is awaited)
//   EdgeDrive text                          the page's visible text
//   EdgeDrive find <text>                   the clickable elements whose text holds the words, with an index each
//   EdgeDrive click <text> [<n>]            clicks the n-th (1 by default) such element, as find lists them
//   EdgeDrive focus <css selector>          focuses the element
//   EdgeDrive type <text>                   types the text into the focused element, as keys
//   EdgeDrive press <key>                   Enter, Tab, Escape, Space, Down, Up
//   EdgeDrive upload <css selector> <file>  sets the file on a file input, as a user's pick would
//   EdgeDrive shot <file.png>               a screenshot of the page
//   EdgeDrive wait <text> [<seconds>]       waits until the page's text holds the text (30 s by default)
//
// The port is 9222 unless EDGEDRIVE_PORT says otherwise.
using System.Net.Http;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

internal static class Program
{
    private static readonly string Port = Environment.GetEnvironmentVariable("EDGEDRIVE_PORT") ?? "9222";
    private static readonly string TargetFile = Path.Combine(Path.GetTempPath(), "edgedrive-target.txt");
    private static readonly HttpClient Http = new();

    private static async Task<int> Main(string[] args)
    {
        if (args.Length == 0)
        {
            Console.Error.WriteLine("EdgeDrive <command> ...; the commands are listed at the top of Program.cs.");
            return 2;
        }

        try
        {
            switch (args[0])
            {
                case "targets":
                    foreach (var target in await TargetsAsync())
                    {
                        if (target is not null)
                        {
                            Console.WriteLine($"{target["id"]}  {target["type"]}  {target["title"]}  {target["url"]}");
                        }
                    }

                    return 0;
                case "open":
                {
                    var created = JsonNode.Parse(await Http.GetStringAsync($"http://localhost:{Port}/json/new?{Uri.EscapeDataString(Arg(args, 1))}").ContinueWith(t => t.Result))!;
                    File.WriteAllText(TargetFile, created["id"]!.ToString());
                    Console.WriteLine($"Opened {created["id"]} on {created["url"]}");
                    return 0;
                }
                case "use":
                    File.WriteAllText(TargetFile, Arg(args, 1));
                    Console.WriteLine($"Using {Arg(args, 1)}");
                    return 0;
            }

            using var page = await Page.ConnectAsync(await CurrentTargetUrlAsync());
            switch (args[0])
            {
                case "nav":
                    await page.CallAsync("Page.enable");
                    await page.CallAsync("Page.navigate", new JsonObject { ["url"] = Arg(args, 1) });
                    await page.WaitForEventAsync("Page.loadEventFired", TimeSpan.FromSeconds(60));
                    await Task.Delay(1500);
                    Console.WriteLine(await page.EvalAsync("document.title + ' | ' + location.href"));
                    return 0;
                case "eval":
                    Console.WriteLine(await page.EvalAsync(Arg(args, 1)));
                    return 0;
                case "evalfile":
                    // A script from a file: a shell's quoting mangles any JavaScript of size.
                    Console.WriteLine(await page.EvalAsync(File.ReadAllText(Arg(args, 1))));
                    return 0;
                case "text":
                    Console.WriteLine(await page.EvalAsync("document.body.innerText"));
                    return 0;
                case "find":
                    Console.WriteLine(await page.EvalAsync(FindScript(Arg(args, 1), click: false, index: 0)));
                    return 0;
                case "click":
                {
                    var index = args.Length > 2 ? int.Parse(args[2]) : 1;
                    Console.WriteLine(await page.EvalAsync(FindScript(Arg(args, 1), click: true, index)));
                    await Task.Delay(1000);
                    return 0;
                }
                case "focus":
                    Console.WriteLine(await page.EvalAsync($"(() => {{ const e = document.querySelector({JsonSerializer.Serialize(Arg(args, 1))}); if (!e) return 'not found'; e.focus(); return 'focused ' + (e.tagName + ' ' + (e.getAttribute('aria-label') || e.id || e.name || '')).trim(); }})()"));
                    return 0;
                case "type":
                    await page.CallAsync("Input.insertText", new JsonObject { ["text"] = Arg(args, 1) });
                    Console.WriteLine($"Typed {Arg(args, 1).Length} characters");
                    return 0;
                case "press":
                    await page.PressAsync(Arg(args, 1));
                    Console.WriteLine($"Pressed {Arg(args, 1)}");
                    return 0;
                case "dropxy":
                {
                    // A file dropped on the page at viewport coordinates, as a user drags one from File Explorer: for an
                    // upload area whose hidden file input ignores a file set on it directly.
                    var x = double.Parse(Arg(args, 1), System.Globalization.CultureInfo.InvariantCulture);
                    var y = double.Parse(Arg(args, 2), System.Globalization.CultureInfo.InvariantCulture);
                    var files = new JsonArray();
                    foreach (var file in args.Skip(3))
                    {
                        files.Add(Path.GetFullPath(file));
                    }

                    JsonObject Data() => new() { ["items"] = new JsonArray(), ["files"] = files.DeepClone(), ["dragOperationsMask"] = 1 };
                    await page.CallAsync("Input.dispatchDragEvent", new JsonObject { ["type"] = "dragEnter", ["x"] = x, ["y"] = y, ["data"] = Data() });
                    await page.CallAsync("Input.dispatchDragEvent", new JsonObject { ["type"] = "dragOver", ["x"] = x, ["y"] = y, ["data"] = Data() });
                    await page.CallAsync("Input.dispatchDragEvent", new JsonObject { ["type"] = "drop", ["x"] = x, ["y"] = y, ["data"] = Data() });
                    Console.WriteLine($"Dropped {files.Count} file(s) at {x}, {y}");
                    return 0;
                }
                case "clickxy":
                {
                    // A real mouse click at viewport coordinates, for a page that acts only on trusted events.
                    var x = double.Parse(Arg(args, 1), System.Globalization.CultureInfo.InvariantCulture);
                    var y = double.Parse(Arg(args, 2), System.Globalization.CultureInfo.InvariantCulture);
                    await page.CallAsync("Input.dispatchMouseEvent", new JsonObject { ["type"] = "mouseMoved", ["x"] = x, ["y"] = y });
                    await page.CallAsync("Input.dispatchMouseEvent", new JsonObject { ["type"] = "mousePressed", ["x"] = x, ["y"] = y, ["button"] = "left", ["clickCount"] = 1 });
                    await page.CallAsync("Input.dispatchMouseEvent", new JsonObject { ["type"] = "mouseReleased", ["x"] = x, ["y"] = y, ["button"] = "left", ["clickCount"] = 1 });
                    Console.WriteLine($"Clicked at {x}, {y}");
                    return 0;
                }
                case "upload":
                {
                    var document = await page.CallAsync("DOM.getDocument", new JsonObject { ["depth"] = 0 });
                    var root = document["root"]!["nodeId"]!.GetValue<int>();
                    var node = await page.CallAsync("DOM.querySelector", new JsonObject { ["nodeId"] = root, ["selector"] = Arg(args, 1) });
                    var nodeId = node["nodeId"]!.GetValue<int>();
                    if (nodeId == 0)
                    {
                        Console.WriteLine("No element matches the selector.");
                        return 1;
                    }

                    var files = new JsonArray();
                    foreach (var file in args.Skip(2))
                    {
                        files.Add(Path.GetFullPath(file));
                    }

                    await page.CallAsync("DOM.setFileInputFiles", new JsonObject { ["files"] = files, ["nodeId"] = nodeId });
                    Console.WriteLine($"Set {files.Count} file(s) on the input.");
                    return 0;
                }
                case "shot":
                {
                    var shot = await page.CallAsync("Page.captureScreenshot", new JsonObject { ["format"] = "png" });
                    File.WriteAllBytes(Arg(args, 1), Convert.FromBase64String(shot["data"]!.ToString()));
                    Console.WriteLine($"Saved {Arg(args, 1)}");
                    return 0;
                }
                case "wait":
                {
                    var seconds = args.Length > 2 ? int.Parse(args[2]) : 30;
                    var wanted = Arg(args, 1);
                    var deadline = DateTime.UtcNow.AddSeconds(seconds);
                    while (DateTime.UtcNow < deadline)
                    {
                        var text = await page.EvalAsync("document.body.innerText");
                        if (text.Contains(wanted, StringComparison.OrdinalIgnoreCase))
                        {
                            Console.WriteLine($"Found: {wanted}");
                            return 0;
                        }

                        await Task.Delay(1000);
                    }

                    Console.WriteLine($"Not found within {seconds} s: {wanted}");
                    return 1;
                }
                default:
                    Console.Error.WriteLine($"Unknown command {args[0]}.");
                    return 2;
            }
        }
        catch (Exception e)
        {
            Console.Error.WriteLine($"{e.GetType().Name}: {e.Message}");
            return 1;
        }
    }

    private static string Arg(string[] args, int index) =>
        index < args.Length ? args[index] : throw new ArgumentException($"The command {args[0]} needs an argument.");

    private static async Task<JsonArray> TargetsAsync() =>
        JsonNode.Parse(await Http.GetStringAsync($"http://localhost:{Port}/json"))!.AsArray();

    private static async Task<string> CurrentTargetUrlAsync()
    {
        var targets = await TargetsAsync();
        var wanted = File.Exists(TargetFile) ? File.ReadAllText(TargetFile).Trim() : null;
        var target = targets.FirstOrDefault(t => t!["id"]!.ToString() == wanted)
            ?? targets.FirstOrDefault(t => t!["type"]!.ToString() == "page")
            ?? throw new InvalidOperationException("Edge has no page; is it running with --remote-debugging-port?");
        return target!["webSocketDebuggerUrl"]!.ToString();
    }

    /// <summary>Finds buttons, links, menu items and inputs whose visible text or label holds every word of the text; lists them, or clicks the n-th.</summary>
    private static string FindScript(string text, bool click, int index) => $$"""
        (() => {
          const words = {{JsonSerializer.Serialize(text)}}.toLowerCase().split(/\s+/).filter(w => w);
          // Partner Center is built of custom elements (he-button, he-link, he-tab ...) inside shadow roots, so the
          // walk goes into every shadow root and takes any element with a clickable tag, role or tag prefix.
          const selector = 'a, button, [role=button], [role=link], [role=tab], [role=menuitem], [role=radio], [role=checkbox], [role=option], [role=switch], input, select, textarea, label, summary, he-button, he-link, he-tab, he-radio, he-checkbox, he-menu-item, he-option, he-dropdown, he-text-field, he-text-area, he-select, he-switch, he-accordion-item, fluent-button, fluent-anchor, fluent-tab, fluent-radio, fluent-checkbox';
          const all = [];
          const walk = root => { for (const e of root.querySelectorAll('*')) { if (e.matches(selector)) all.push(e); if (e.shadowRoot) walk(e.shadowRoot); } };
          walk(document);
          const visible = e => { const r = e.getBoundingClientRect(); return r.width > 0 && r.height > 0; };
          const name = e => ((e.innerText || e.textContent || e.value || '') + ' ' + (e.getAttribute('aria-label') || '') + ' ' + (e.getAttribute('title') || '') + ' ' + (e.getAttribute('placeholder') || '') + ' ' + (e.getAttribute('label') || '')).replace(/\s+/g, ' ').trim();
          const hits = all.filter(e => visible(e) && words.every(w => name(e).toLowerCase().includes(w)));
          if (!{{(click ? "true" : "false")}}) return hits.map((e, i) => (i + 1) + '. ' + e.tagName.toLowerCase() + (e.getAttribute('role') ? '[' + e.getAttribute('role') + ']' : '') + ': ' + name(e).slice(0, 120)).join('\n') || 'nothing matches';
          const target = hits[{{index}} - 1];
          if (!target) return 'no element ' + {{index}} + ' for the text; ' + hits.length + ' match';
          target.scrollIntoView({ block: 'center' });
          target.focus();
          target.click();
          return 'clicked ' + target.tagName.toLowerCase() + ': ' + name(target).slice(0, 120);
        })()
        """;
}

/// <summary>One DevTools connection to a page target: calls by id, events watched by name.</summary>
internal sealed class Page : IDisposable
{
    private readonly ClientWebSocket _socket = new();
    private readonly List<JsonNode> _events = [];
    private int _nextId;

    public static async Task<Page> ConnectAsync(string url)
    {
        var page = new Page();
        page._socket.Options.SetBuffer(1 << 20, 1 << 20);
        await page._socket.ConnectAsync(new Uri(url), CancellationToken.None);
        return page;
    }

    public async Task<JsonNode> CallAsync(string method, JsonObject? parameters = null)
    {
        var id = ++_nextId;
        var message = new JsonObject { ["id"] = id, ["method"] = method, ["params"] = parameters ?? [] };
        await _socket.SendAsync(Encoding.UTF8.GetBytes(message.ToJsonString()), WebSocketMessageType.Text, true, CancellationToken.None);
        var deadline = DateTime.UtcNow.AddSeconds(60);
        while (DateTime.UtcNow < deadline)
        {
            var node = await ReceiveAsync(deadline - DateTime.UtcNow);
            if (node["id"]?.GetValue<int>() == id)
            {
                if (node["error"] is { } error)
                {
                    throw new InvalidOperationException($"{method}: {error["message"]}");
                }

                return node["result"] ?? new JsonObject();
            }

            if (node["method"] is not null)
            {
                _events.Add(node);
            }
        }

        throw new TimeoutException($"{method} did not answer.");
    }

    public async Task<string> EvalAsync(string expression)
    {
        var result = await CallAsync("Runtime.evaluate", new JsonObject { ["expression"] = expression, ["returnByValue"] = true, ["awaitPromise"] = true });
        if (result["exceptionDetails"] is { } exception)
        {
            return "JavaScript error: " + (exception["exception"]?["description"]?.ToString() ?? exception["text"]?.ToString());
        }

        var value = result["result"]?["value"];
        return value is null ? (result["result"]?["type"]?.ToString() ?? "undefined") : value is JsonValue v && v.TryGetValue<string>(out var s) ? s : value.ToJsonString();
    }

    public async Task WaitForEventAsync(string method, TimeSpan timeout)
    {
        if (_events.Any(e => e["method"]!.ToString() == method))
        {
            return;
        }

        var deadline = DateTime.UtcNow.Add(timeout);
        while (DateTime.UtcNow < deadline)
        {
            var node = await ReceiveAsync(deadline - DateTime.UtcNow);
            if (node["method"]?.ToString() == method)
            {
                return;
            }
        }
    }

    public async Task PressAsync(string key)
    {
        var (code, keyName, text, windowsCode) = key switch
        {
            "Enter" => ("Enter", "Enter", "\r", 13),
            "Tab" => ("Tab", "Tab", "", 9),
            "Escape" => ("Escape", "Escape", "", 27),
            "Space" => ("Space", " ", " ", 32),
            "Down" => ("ArrowDown", "ArrowDown", "", 40),
            "Up" => ("ArrowUp", "ArrowUp", "", 38),
            _ => throw new ArgumentException($"Unknown key {key}."),
        };
        var down = new JsonObject { ["type"] = text.Length > 0 ? "keyDown" : "rawKeyDown", ["key"] = keyName, ["code"] = code, ["windowsVirtualKeyCode"] = windowsCode };
        if (text.Length > 0)
        {
            down["text"] = text;
        }

        await CallAsync("Input.dispatchKeyEvent", down);
        await CallAsync("Input.dispatchKeyEvent", new JsonObject { ["type"] = "keyUp", ["key"] = keyName, ["code"] = code, ["windowsVirtualKeyCode"] = windowsCode });
    }

    private async Task<JsonNode> ReceiveAsync(TimeSpan timeout)
    {
        using var cancellation = new CancellationTokenSource(timeout > TimeSpan.Zero ? timeout : TimeSpan.FromMilliseconds(1));
        var buffer = new byte[1 << 20];
        using var stream = new MemoryStream();
        WebSocketReceiveResult result;
        do
        {
            result = await _socket.ReceiveAsync(buffer, cancellation.Token);
            stream.Write(buffer, 0, result.Count);
        } while (!result.EndOfMessage);

        return JsonNode.Parse(stream.ToArray())!;
    }

    public void Dispose() => _socket.Dispose();
}
