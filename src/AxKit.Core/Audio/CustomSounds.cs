namespace AxKit.Core.Audio;

/// <summary>
/// The user's own sounds (docs/axKit/SPEC.md AX-7.12; the maintainer's ask of 2026-11-15, "I want the folder", built at
/// his word of 2026-11-24): a WAV file in <c>%APPDATA%\AxKit\sounds</c> named as one of axKit's sounds
/// (<see cref="Names"/>, <c>edge.wav</c>) is played in place of the built-in one, from the next start of a window; taking
/// the file away brings the built-in back. A file of any common shape is taken (PCM of 8, 16, 24 or 32 bits, 32-bit
/// float, mono or stereo, any rate) and made into the one shape the sound devices play, 16-bit mono at
/// <see cref="WaveTone.SampleRate"/>; a file that is not one, or longer than <see cref="MaxSeconds"/>, is passed over
/// and the log says why. Pure but for the folder's read in <see cref="Or"/>; tested in <c>CustomSoundsTests</c>.
/// </summary>
public static class CustomSounds
{
    /// <summary>The names a file may have, without ".wav", each a sound of the bundle; the sound names file lists them with what each is.</summary>
    public static readonly IReadOnlyList<(string Name, string What)> Names =
    [
        ("click", "a line arrived in axClaude's or axTerminal's conversation"),
        ("tool", "a tool call arrived in axClaude"),
        ("ready", "Claude is ready"),
        ("sent", "your message went out"),
        ("responding", "a reply starts"),
        ("done", "a turn is done"),
        ("question", "a question waits for you"),
        ("notice", "a notice opened by itself"),
        ("offer", "an AI offers a command for you to run"),
        ("edge", "the edge of a list or a text: nothing more that way"),
        ("nav-key", "a key of navigation mode was taken"),
        ("nothing", "nothing of that kind to go to"),
        ("unassigned", "the key has no command here"),
        ("recall", "a command recalled into a field"),
        ("heading", "the caret reached a heading"),
        ("form-open", "the focus entered a form"),
        ("list-open", "the focus entered a list"),
        ("field-open", "the focus entered a field to type in"),
        ("text-open", "the focus entered a text to read"),
        ("alert", "Claude finished or asks while its window is behind"),
        ("background-start", "work started in the background"),
        ("background-finish", "work in the background ended"),
        ("working-music", "the music while Claude works, played as a loop"),
    ];

    /// <summary>The longest file taken: a sound, not a recording; the working music loops a file this long at most.</summary>
    public const int MaxSeconds = 60;

    /// <summary>The folder's own file that lists the names, written when the folder is made.</summary>
    public const string NamesFile = "sound names.txt";

    /// <summary>The text of <see cref="NamesFile"/>.</summary>
    public static string NamesText() =>
        "axKit's sounds. Put a WAV file here named as one of these, edge.wav for instance, and it plays in place of axKit's own from the next start of a window. Take the file away and axKit's own comes back.\r\n\r\n"
        + string.Join("\r\n", Names.Select(name => $"{name.Name}.wav: {name.What}")) + "\r\n";

    /// <summary>
    /// The sound <paramref name="name"/> as the user's file in <paramref name="folder"/> gives it, made into the bundle's
    /// shape, or <paramref name="made"/> when there is no such file or it is not taken (said through
    /// <paramref name="problem"/>, for the log).
    /// </summary>
    public static byte[] Or(string folder, string name, byte[] made, Action<string> problem)
    {
        var path = Path.Combine(folder, name + ".wav");
        try
        {
            if (!File.Exists(path))
            {
                return made;
            }

            if (Convert(File.ReadAllBytes(path), out var why) is { } converted)
            {
                return converted;
            }

            problem($"The sound file {name}.wav is not used: {why}");
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            problem($"The sound file {name}.wav could not be read: {ex.Message}");
        }

        return made;
    }

    /// <summary>
    /// A WAV file's bytes as 16-bit mono PCM at <see cref="WaveTone.SampleRate"/>, or null with <paramref name="problem"/>
    /// saying why: not a WAV file, a shape not taken, no samples, or longer than <see cref="MaxSeconds"/>.
    /// </summary>
    public static byte[]? Convert(byte[] file, out string? problem)
    {
        problem = null;
        if (file.Length < 12 || !file.AsSpan(0, 4).SequenceEqual("RIFF"u8) || !file.AsSpan(8, 4).SequenceEqual("WAVE"u8))
        {
            problem = "it is not a WAV file";
            return null;
        }

        int format = 0, channels = 0, rate = 0, bits = 0;
        ReadOnlySpan<byte> data = default;
        var found = false;
        for (var at = 12; at + 8 <= file.Length;)
        {
            var id = file.AsSpan(at, 4);
            var length = BitConverter.ToInt32(file, at + 4);
            var body = at + 8;
            if (length < 0 || body + length > file.Length)
            {
                length = file.Length - body;
            }

            if (id.SequenceEqual("fmt "u8) && length >= 16)
            {
                format = BitConverter.ToUInt16(file, body);
                channels = BitConverter.ToUInt16(file, body + 2);
                rate = BitConverter.ToInt32(file, body + 4);
                bits = BitConverter.ToUInt16(file, body + 14);
                if (format == 0xFFFE && length >= 26)
                {
                    // WAVE_FORMAT_EXTENSIBLE: the sub-format's first two bytes are the format tag.
                    format = BitConverter.ToUInt16(file, body + 24);
                }
            }
            else if (id.SequenceEqual("data"u8))
            {
                data = file.AsSpan(body, length);
                found = true;
            }

            at = body + length + (length & 1);
        }

        var pcm = format == 1 && bits is 8 or 16 or 24 or 32;
        var floats = format == 3 && bits == 32;
        if (!pcm && !floats || channels < 1 || rate < 1000 || !found)
        {
            problem = "its shape is not one axKit plays: PCM of 8, 16, 24 or 32 bits, or 32-bit float";
            return null;
        }

        var frameBytes = bits / 8 * channels;
        var frames = data.Length / frameBytes;
        if (frames == 0)
        {
            problem = "it holds no sound";
            return null;
        }

        if (frames / (double)rate > MaxSeconds)
        {
            problem = $"it is longer than {MaxSeconds} seconds";
            return null;
        }

        // The channels mixed to one, as numbers from -1 to 1.
        var mono = new double[frames];
        for (var frame = 0; frame < frames; frame++)
        {
            var sum = 0.0;
            for (var channel = 0; channel < channels; channel++)
            {
                sum += Sample(data, (frame * channels + channel) * (bits / 8), bits, floats);
            }

            mono[frame] = sum / channels;
        }

        // To the bundle's rate, each sample read between the two nearest of the file's.
        var count = (int)Math.Max(1, Math.Round(frames * (double)WaveTone.SampleRate / rate));
        var samples = new short[count];
        for (var i = 0; i < count; i++)
        {
            var at = i * (double)rate / WaveTone.SampleRate;
            var left = Math.Min((int)at, frames - 1);
            var right = Math.Min(left + 1, frames - 1);
            var value = mono[left] + (mono[right] - mono[left]) * (at - left);
            samples[i] = (short)Math.Round(Math.Clamp(value, -1.0, 1.0) * short.MaxValue);
        }

        return WaveTone.Wav(samples);
    }

    private static double Sample(ReadOnlySpan<byte> data, int at, int bits, bool floats) => bits switch
    {
        8 => (data[at] - 128) / 128.0,
        16 => BitConverter.ToInt16(data.Slice(at, 2)) / 32768.0,
        24 => ((data[at] | (data[at + 1] << 8) | (data[at + 2] << 16)) << 8 >> 8) / 8388608.0,
        _ => floats ? BitConverter.ToSingle(data.Slice(at, 4)) : BitConverter.ToInt32(data.Slice(at, 4)) / 2147483648.0,
    };
}
