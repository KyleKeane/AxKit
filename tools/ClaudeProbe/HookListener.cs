using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json.Nodes;

namespace Probe;

/// <summary>
/// The hook receiver: a small HTTP/1.1 server on 127.0.0.1 over a plain socket, so that every time of a request is
/// the probe's own (accepted, request line read, body complete, reply written) and nothing of http.sys is between.
/// An http hook posts here itself (?via=http); a command hook runs the relay, which posts the hook's standard input
/// (?via=command). Every request is one line of hooks.jsonl with its body unchanged. The reply is an empty 200 unless
/// the plan gives the event a reply, after the event's delay.
/// </summary>
internal sealed class HookListener
{
    private readonly HookPlan _plan;
    private readonly Jsonl _log;
    private readonly TranscriptWatcher _watcher;
    private readonly object _gate = new();
    private readonly List<(double T, string Event, string Via)> _arrivals = [];
    private TcpListener _tcp = null!;
    private volatile bool _stopped;
    private int _connections;

    public HookListener(HookPlan plan, Jsonl log, TranscriptWatcher watcher)
    {
        _plan = plan;
        _log = log;
        _watcher = watcher;
    }

    public int Port { get; private set; }

    /// <summary>The session id of the first hook that carried one.</summary>
    public string? SessionId { get; private set; }

    public void Start(int port)
    {
        _tcp = new TcpListener(IPAddress.Loopback, port);
        _tcp.Start();
        Port = ((IPEndPoint)_tcp.LocalEndpoint).Port;
        new Thread(AcceptLoop) { IsBackground = true, Name = "hook-accept" }.Start();
    }

    public void Stop()
    {
        _stopped = true;
        try
        {
            _tcp.Stop();
        }
        catch (SocketException)
        {
        }
    }

    /// <summary>A port on 127.0.0.1 that nothing listens on: taken and let go again.</summary>
    public static int ClosedPort()
    {
        var probe = new TcpListener(IPAddress.Loopback, 0);
        probe.Start();
        var port = ((IPEndPoint)probe.LocalEndpoint).Port;
        probe.Stop();
        return port;
    }

    /// <summary>How many requests for an event matching the pattern arrived at or after <paramref name="since"/>.</summary>
    public int CountSince(System.Text.RegularExpressions.Regex pattern, double since)
    {
        lock (_gate)
        {
            return _arrivals.Count(arrival => arrival.T >= since && pattern.IsMatch(arrival.Event));
        }
    }

    public Dictionary<string, int> Counts()
    {
        lock (_gate)
        {
            return _arrivals.GroupBy(arrival => $"{arrival.Event} via {arrival.Via}").OrderBy(group => group.Key, StringComparer.Ordinal).ToDictionary(group => group.Key, group => group.Count());
        }
    }

    private void AcceptLoop()
    {
        while (!_stopped)
        {
            TcpClient client;
            try
            {
                client = _tcp.AcceptTcpClient();
            }
            catch (Exception ex) when (ex is SocketException or ObjectDisposedException or InvalidOperationException)
            {
                break;
            }

            var accepted = Clock.Ms;
            var id = Interlocked.Increment(ref _connections);
            client.NoDelay = true;
            new Thread(() => Serve(client, id, accepted)) { IsBackground = true, Name = "hook-conn-" + id }.Start();
        }
    }

    private void Serve(TcpClient client, int connection, double accepted)
    {
        try
        {
            using (client)
            using (var stream = client.GetStream())
            {
                var reader = new HttpReader(stream);
                var request = 0;
                while (true)
                {
                    var requestLine = reader.ReadLine();
                    if (requestLine is null)
                    {
                        break;
                    }

                    if (requestLine.Length == 0)
                    {
                        continue;
                    }

                    var firstAt = Clock.Ms;
                    request++;
                    var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                    while (reader.ReadLine() is { Length: > 0 } header)
                    {
                        var colon = header.IndexOf(':');
                        if (colon > 0)
                        {
                            headers[header[..colon].Trim().ToLowerInvariant()] = header[(colon + 1)..].Trim();
                        }
                    }

                    if (headers.TryGetValue("expect", out var expect) && expect.Contains("100-continue", StringComparison.OrdinalIgnoreCase))
                    {
                        stream.Write("HTTP/1.1 100 Continue\r\n\r\n"u8);
                    }

                    byte[] body;
                    if (headers.TryGetValue("transfer-encoding", out var encoding) && encoding.Contains("chunked", StringComparison.OrdinalIgnoreCase))
                    {
                        using var memory = new MemoryStream();
                        while (true)
                        {
                            var sizeLine = reader.ReadLine() ?? "0";
                            var semicolon = sizeLine.IndexOf(';');
                            var size = Convert.ToInt32((semicolon < 0 ? sizeLine : sizeLine[..semicolon]).Trim(), 16);
                            if (size == 0)
                            {
                                while (reader.ReadLine() is { Length: > 0 })
                                {
                                }

                                break;
                            }

                            memory.Write(reader.ReadExactly(size));
                            reader.ReadLine();
                        }

                        body = memory.ToArray();
                    }
                    else
                    {
                        body = headers.TryGetValue("content-length", out var length) && int.TryParse(length, out var count) && count > 0 ? reader.ReadExactly(count) : [];
                    }

                    var bodyAt = Clock.Ms;
                    var bodyWall = Clock.Wall;
                    var parts = requestLine.Split(' ');
                    var method = parts[0];
                    var path = parts.Length > 1 ? parts[1] : string.Empty;
                    var reply = Handle(method, path, headers, body, connection, request, accepted, firstAt, bodyAt, bodyWall, stream);
                    if (!reply || (headers.TryGetValue("connection", out var keep) && keep.Contains("close", StringComparison.OrdinalIgnoreCase)))
                    {
                        break;
                    }
                }
            }
        }
        catch (Exception ex) when (ex is IOException or SocketException or ObjectDisposedException or FormatException or EndOfStreamException)
        {
            // The other end closed in the middle of something: nothing more to log than what was.
        }
    }

    /// <summary>Logs the request and writes the reply; false when the connection is to be closed.</summary>
    private bool Handle(string method, string path, Dictionary<string, string> headers, byte[] body, int connection, int request,
        double accepted, double firstAt, double bodyAt, string bodyWall, NetworkStream stream)
    {
        var query = path.IndexOf('?');
        var route = query < 0 ? path : path[..query];
        if (!route.StartsWith("/hook/", StringComparison.Ordinal))
        {
            stream.Write("HTTP/1.1 404 Not Found\r\nContent-Length: 0\r\n\r\n"u8);
            return true;
        }

        var routeEvent = route["/hook/".Length..];
        var via = query >= 0 && path[(query + 1)..].Contains("via=command", StringComparison.Ordinal) ? "command" : "http";
        string text;
        var utf8 = true;
        try
        {
            text = new UTF8Encoding(false, true).GetString(body);
        }
        catch (ArgumentException)
        {
            utf8 = false;
            text = Encoding.UTF8.GetString(body);
        }

        var parsed = Json.TryObject(text);
        var name = Json.Str(parsed, "hook_event_name") ?? routeEvent;
        lock (_gate)
        {
            _arrivals.Add((bodyAt, name, via));
            SessionId ??= Json.Str(parsed, "session_id");
        }

        // The session's own file, as the hook names it: the watcher follows it from here on.
        if (Json.Str(parsed, "transcript_path") is { Length: > 0 } transcript)
        {
            _watcher.Track(transcript, "hook " + name);
        }

        if (Json.Str(parsed, "agent_transcript_path") is { Length: > 0 } agentTranscript)
        {
            _watcher.Track(agentTranscript, "hook " + name + " (agent)");
        }

        var delay = _plan.DelayFor(name);
        if (delay > 0)
        {
            Thread.Sleep(delay);
        }

        _plan.Replies.TryGetValue(name, out var reply);
        var status = reply is null ? 200 : Json.Int(reply, "status", 200);
        byte[] replyBody = [];
        if (reply?["body"] is { } replyNode)
        {
            replyBody = Encoding.UTF8.GetBytes(replyNode is JsonValue value && value.TryGetValue<string>(out var plain) ? plain : replyNode.ToJsonString(Json.Line));
        }

        var head = new StringBuilder();
        head.Append("HTTP/1.1 ").Append(status).Append(status is >= 200 and < 300 ? " OK" : " Error").Append("\r\n");
        head.Append("Content-Length: ").Append(replyBody.Length).Append("\r\n");
        if (replyBody.Length > 0)
        {
            head.Append("Content-Type: application/json\r\n");
        }

        if (via == "command" && reply is not null)
        {
            if (reply["exit"] is not null)
            {
                head.Append("X-Probe-Exit: ").Append(Json.Int(reply, "exit", 0)).Append("\r\n");
            }

            if (Json.Str(reply, "stderr") is { } stderr)
            {
                head.Append("X-Probe-Stderr: ").Append(Uri.EscapeDataString(stderr)).Append("\r\n");
            }
        }

        var close = headers.TryGetValue("connection", out var connectionHeader) && connectionHeader.Contains("close", StringComparison.OrdinalIgnoreCase);
        if (close)
        {
            head.Append("Connection: close\r\n");
        }

        head.Append("\r\n");
        var wrote = true;
        try
        {
            stream.Write(Encoding.ASCII.GetBytes(head.ToString()));
            stream.Write(replyBody);
            stream.Flush();
        }
        catch (Exception ex) when (ex is IOException or SocketException or ObjectDisposedException)
        {
            // The hook was given up on (a timeout shorter than the delay): the request is logged all the same.
            wrote = false;
        }

        var replyAt = Clock.Ms;
        var record = new JsonObject
        {
            ["t"] = bodyAt,
            ["wall"] = bodyWall,
            ["event"] = name,
            ["via"] = via,
            ["path"] = path,
            ["method"] = method,
            ["conn"] = connection,
            ["req"] = request,
            ["tAccept"] = accepted,
            ["tRequestLine"] = firstAt,
            ["tReply"] = replyAt,
            ["delayMs"] = delay,
            ["status"] = status,
            ["replyWritten"] = wrote,
            ["replyBytes"] = replyBody.Length,
            ["bodyBytes"] = body.Length,
            ["utf8"] = utf8,
        };
        if (name != routeEvent)
        {
            record["routeEvent"] = routeEvent;
        }

        var shown = new JsonObject();
        foreach (var pair in headers)
        {
            if (!pair.Key.StartsWith("x-probe-", StringComparison.Ordinal))
            {
                shown[pair.Key] = pair.Value;
            }
        }

        record["headers"] = shown;
        if (via == "command")
        {
            // The relay's own times, on the probe's clock (through the wall clock both processes share).
            var relay = new JsonObject();
            if (headers.TryGetValue("x-probe-pid", out var pid))
            {
                relay["pid"] = int.TryParse(pid, out var number) ? number : 0;
            }

            foreach (var (header, field) in new[] { ("x-probe-created", "tCreated"), ("x-probe-main", "tMain"), ("x-probe-stdin-end", "tStdinEnd") })
            {
                if (headers.TryGetValue(header, out var ticks) && long.TryParse(ticks, out var value))
                {
                    relay[field] = Clock.FromTicks(value);
                }
            }

            if (headers.TryGetValue("x-probe-stdin-redirected", out var redirected))
            {
                relay["stdinRedirected"] = redirected == "1";
            }

            if (headers.TryGetValue("x-probe-cwd", out var cwd))
            {
                relay["cwd"] = Uri.UnescapeDataString(cwd);
            }

            if (headers.TryGetValue("x-probe-env", out var env))
            {
                var variables = new JsonObject();
                foreach (var item in env.Split('&', StringSplitOptions.RemoveEmptyEntries))
                {
                    var equals = item.IndexOf('=');
                    variables[Uri.UnescapeDataString(equals < 0 ? item : item[..equals])] = equals < 0 ? "(set; value not recorded)" : Uri.UnescapeDataString(item[(equals + 1)..]);
                }

                relay["env"] = variables;
            }

            record["relay"] = relay;
        }

        record["body"] = text;
        _log.Write(record);
        return wrote && !close;
    }
}

/// <summary>Lines and counted bytes off a stream, as HTTP/1.1 needs them.</summary>
internal sealed class HttpReader(Stream stream)
{
    private readonly byte[] _buffer = new byte[65536];
    private int _start;
    private int _end;

    /// <summary>The next line without its CR LF; null at the end of the stream with nothing read.</summary>
    public string? ReadLine()
    {
        using var line = new MemoryStream();
        while (true)
        {
            for (var i = _start; i < _end; i++)
            {
                if (_buffer[i] == (byte)'\n')
                {
                    line.Write(_buffer, _start, i - _start);
                    _start = i + 1;
                    var bytes = line.ToArray();
                    var length = bytes.Length;
                    if (length > 0 && bytes[length - 1] == (byte)'\r')
                    {
                        length--;
                    }

                    return Encoding.Latin1.GetString(bytes, 0, length);
                }
            }

            line.Write(_buffer, _start, _end - _start);
            _start = _end = 0;
            if (!Fill())
            {
                return line.Length == 0 ? null : Encoding.Latin1.GetString(line.ToArray());
            }
        }
    }

    public byte[] ReadExactly(int count)
    {
        var result = new byte[count];
        var done = 0;
        while (done < count)
        {
            if (_start == _end)
            {
                _start = _end = 0;
                if (!Fill())
                {
                    throw new EndOfStreamException();
                }
            }

            var take = Math.Min(count - done, _end - _start);
            Buffer.BlockCopy(_buffer, _start, result, done, take);
            _start += take;
            done += take;
        }

        return result;
    }

    private bool Fill()
    {
        var read = stream.Read(_buffer, _end, _buffer.Length - _end);
        if (read <= 0)
        {
            return false;
        }

        _end += read;
        return true;
    }
}
