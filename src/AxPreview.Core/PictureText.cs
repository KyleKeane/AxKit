using System.Diagnostics;
using System.Text;
using AxKit.Core;

namespace AxPreview.Core;

/// <summary>
/// The text in a picture through Windows' own reader (SPEC.md AP-7.2 item 4, AP-D11): <c>Windows.Media.Ocr</c>, a
/// WinRT engine every Windows has, which Windows PowerShell 5.1 loads as a type of its own. axPreview writes this one
/// small script beside its logs and runs it as a child process per picture, reading the text lines back, as axPass runs
/// KeePassXC's tool: no SDK in the program, no package, nothing leaves the computer. The script prints one line per
/// line of text and exits 0; exits 2 when Windows has no recognition language; exits 1 with the reason on standard
/// error for anything else. A picture larger than the reader takes is scaled down for it inside the script.
/// </summary>
public static class PictureText
{
    public const string Script = """
        param([string]$Path)
        [Console]::OutputEncoding = [Text.Encoding]::UTF8
        try {
          $null = [Windows.Media.Ocr.OcrEngine, Windows.Foundation, ContentType = WindowsRuntime]
          $null = [Windows.Storage.StorageFile, Windows.Storage, ContentType = WindowsRuntime]
          $null = [Windows.Graphics.Imaging.BitmapDecoder, Windows.Graphics, ContentType = WindowsRuntime]
          Add-Type -AssemblyName System.Runtime.WindowsRuntime
          $asTask = ([System.WindowsRuntimeSystemExtensions].GetMethods() | Where-Object { $_.Name -eq 'AsTask' -and $_.GetParameters().Count -eq 1 -and $_.GetParameters()[0].ParameterType.Name -eq 'IAsyncOperation`1' })[0]
          function Await($operation, $type) { $task = $asTask.MakeGenericMethod($type).Invoke($null, @($operation)); $task.Wait(); $task.Result }
          $engine = [Windows.Media.Ocr.OcrEngine]::TryCreateFromUserProfileLanguages()
          if ($null -eq $engine) { exit 2 }
          $file = Await ([Windows.Storage.StorageFile]::GetFileFromPathAsync($Path)) ([Windows.Storage.StorageFile])
          $stream = Await ($file.OpenAsync([Windows.Storage.FileAccessMode]::Read)) ([Windows.Storage.Streams.IRandomAccessStream])
          $decoder = Await ([Windows.Graphics.Imaging.BitmapDecoder]::CreateAsync($stream)) ([Windows.Graphics.Imaging.BitmapDecoder])
          $largest = [Math]::Max($decoder.PixelWidth, $decoder.PixelHeight)
          $limit = [Windows.Media.Ocr.OcrEngine]::MaxImageDimension
          if ($largest -gt $limit) {
            $transform = New-Object Windows.Graphics.Imaging.BitmapTransform
            $transform.ScaledWidth = [uint32][Math]::Max(1, [Math]::Floor($decoder.PixelWidth * $limit / $largest))
            $transform.ScaledHeight = [uint32][Math]::Max(1, [Math]::Floor($decoder.PixelHeight * $limit / $largest))
            $bitmap = Await ($decoder.GetSoftwareBitmapAsync([Windows.Graphics.Imaging.BitmapPixelFormat]::Bgra8, [Windows.Graphics.Imaging.BitmapAlphaMode]::Premultiplied, $transform, [Windows.Graphics.Imaging.ExifOrientationMode]::RespectExifOrientation, [Windows.Graphics.Imaging.ColorManagementMode]::DoNotColorManage)) ([Windows.Graphics.Imaging.SoftwareBitmap])
          } else {
            $bitmap = Await ($decoder.GetSoftwareBitmapAsync()) ([Windows.Graphics.Imaging.SoftwareBitmap])
          }
          $result = Await ($engine.RecognizeAsync($bitmap)) ([Windows.Media.Ocr.OcrResult])
          foreach ($line in $result.Lines) { Write-Output $line.Text }
          exit 0
        } catch {
          [Console]::Error.WriteLine($_.Exception.GetBaseException().Message)
          exit 1
        }
        """;

    /// <summary>How long the reader may take; a screenshot is read in a second or two.</summary>
    public static readonly TimeSpan Timeout = TimeSpan.FromSeconds(30);

    public const string NoLanguage = "Windows has no text recognition language installed. Settings, Time and language, Language and region: a language's options, then Optical character recognition (or Basic typing) adds one";

    /// <summary>The script beside axPreview's logs (AX-5.1), written by <see cref="EnsureScript"/>.</summary>
    public static string ScriptPath => Path.Combine(Path.GetDirectoryName(AppPaths.LogsFolder)!, "axpreview", "ocr.ps1");

    /// <summary>The script on disk at <paramref name="path"/>, written when missing or different (a new version of axKit brings a new script).</summary>
    public static void EnsureScript(string path)
    {
        if (File.Exists(path) && string.Equals(File.ReadAllText(path), Script, StringComparison.Ordinal))
        {
            return;
        }

        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, Script, new UTF8Encoding(false));
    }

    /// <summary>Windows PowerShell by its full path (security audit S-1), running the script on the picture, its output as UTF-8 (the script sets its console to it).</summary>
    public static ProcessStartInfo Start(string scriptPath, string picturePath)
    {
        var start = new ProcessStartInfo(SystemPrograms.PowerShell)
        {
            StandardOutputEncoding = new UTF8Encoding(false),
            StandardErrorEncoding = new UTF8Encoding(false),
        };
        foreach (var argument in new[] { "-NoProfile", "-NonInteractive", "-ExecutionPolicy", "Bypass", "-File", scriptPath, picturePath })
        {
            start.ArgumentList.Add(argument);
        }

        return start;
    }

    /// <summary>What the script's run means: the text lines (none for a picture without text), or why there are none.</summary>
    public static (IReadOnlyList<string>? Lines, string? Problem) Read(ProcessResult result)
    {
        if (result.TimedOut)
        {
            return (null, $"Windows' reader gave no answer in {Timeout.TotalSeconds:0} seconds");
        }

        return result.ExitCode switch
        {
            0 => (result.Output.Split('\n').Select(line => line.TrimEnd('\r')).Where(line => line.Length > 0).ToList(), null),
            2 => (null, NoLanguage),
            _ => (null, result.Error.Trim() is { Length: > 0 } reason ? reason.Split('\n')[0].Trim() : $"Windows' reader ended with code {result.ExitCode}"),
        };
    }

    /// <summary>The picture's text through the script, off the calling thread; a script that cannot start is a problem, never an exception.</summary>
    public static async Task<(IReadOnlyList<string>? Lines, string? Problem)> RecognizeAsync(string picturePath, CancellationToken cancellation, string? scriptPath = null)
    {
        var script = scriptPath ?? ScriptPath;
        try
        {
            EnsureScript(script);
            // Outside call, program, Windows: Windows PowerShell by its full path, running axPreview's own script beside its
            // logs, which asks Windows' text reader (Windows.Media.Ocr) for the text in one picture; on this computer, no network.
            return Read(await ProcessRun.RunAsync(Start(script, picturePath), [], Timeout, cancellation).ConfigureAwait(false));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or System.ComponentModel.Win32Exception or InvalidOperationException)
        {
            return (null, ex.Message);
        }
    }

    /// <summary>
    /// The page shown at once (AP-7.2, 4): what the picture is and that its text is being read, with the finished page
    /// to follow (<see cref="Rendered.Later"/>).
    /// </summary>
    public static Rendered FirstPage(string path)
    {
        var name = Path.GetFileName(path);
        return Rendered.Of(
        [
            $"{name} is a picture ({Extension(path)}). Reading its text with Windows' own reader...",
            "Its details are in the Details list below: Tab or F6 moves there. Ctrl+E opens the picture in the program Windows has for it.",
        ], "Picture", [], WayOut.Windows) with
        {
            Later = async cancellation =>
            {
                var (lines, problem) = await RecognizeAsync(path, cancellation).ConfigureAwait(false);
                return Finished(path, lines, problem);
            },
        };
    }

    /// <summary>The finished page: the text under a line naming the picture, or that none was found, or why it could not be read; with the one sentence said when it arrives.</summary>
    public static Rendered Finished(string path, IReadOnlyList<string>? lines, string? problem)
    {
        var name = Path.GetFileName(path);
        const string details = "Its details are in the Details list below: Tab or F6 moves there. Ctrl+E opens the picture in the program Windows has for it.";
        if (problem is not null)
        {
            return Rendered.Of([$"The text of {name} could not be read: {problem}", details], "Picture", [], WayOut.Windows) with { Said = "Text not read: " + problem };
        }

        if (lines is null || lines.Count == 0)
        {
            return Rendered.Of([$"No text was found in {name}.", details], "Picture", ["Text lines: 0"], WayOut.Windows) with { Said = "No text found in the picture" };
        }

        var text = new List<string> { $"{name}, picture ({Extension(path)}): the text Windows' reader found follows. {details}", string.Empty };
        text.AddRange(lines);
        var count = lines.Count == 1 ? "1 line" : $"{lines.Count} lines";
        return Rendered.Of(text, "Picture", [$"Text lines: {lines.Count}"], WayOut.Windows) with { Said = "Text read: " + count };
    }

    private static string Extension(string path) => Path.GetExtension(path).TrimStart('.').ToUpperInvariant();
}
