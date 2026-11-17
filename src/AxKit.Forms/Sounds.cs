using System.Runtime.InteropServices;
using AxKit.Core;
using AxKit.Core.Audio;

namespace AxKit.Forms;

/// <summary>
/// The app's sounds (axClaude FR-7.3, FR-7.8, FR-7.9), played through winmm wave-out devices that are opened at the
/// first sound and stay open for the life of the process (D31). Every sound is generated once, pinned and prepared
/// for its device; playing it is a single <c>waveOutWrite</c>, which queues the buffer behind whatever is playing. A
/// chime clears the queue first, so it is never delayed and a tick never cuts it short. A tick is dropped while its
/// own buffer is still queued, so ticks never pile up, and a tick that arrives during a chime follows the chime.
/// Until 1.2.1 the sounds went through <c>PlaySound</c>, which opens and closes the device for every call: ticks in
/// quick succession stuttered and backed up. The working music is the one sound that loops: one buffer written once
/// with the loop flags, on a second device of its own, where no chime's reset can cut it off and no tick waits
/// behind it, and stopped with a reset of that device. The recurring chords of background work (FR-7.10), 4.6 s
/// each, have a third device for the same reasons; they are made off the UI thread at their first use. UI thread only.
///
/// Internal since 2026-11-17 (docs/axKit/events.md EV-5.1): no window and no control plays a sound from here. They
/// raise a kind of event at the door, <see cref="EventDoor"/>, which is this class's one caller; an app cannot name
/// this class at all, and inside these shared pieces <c>EventDoorTests</c> holds it. The automation channel listens
/// to <see cref="Played"/>.
/// </summary>
internal static class Sounds
{
    private const uint WAVE_MAPPER = 0xFFFFFFFF;
    private const int WHDR_BEGINLOOP = 0x0004;
    private const int WHDR_ENDLOOP = 0x0008;
    private const int WHDR_INQUEUE = 0x0010;
    private static readonly int HeaderSize = Marshal.SizeOf<WAVEHDR>();
    private static readonly int FlagsOffset = (int)Marshal.OffsetOf<WAVEHDR>(nameof(WAVEHDR.dwFlags));
    private static readonly int LoopsOffset = (int)Marshal.OffsetOf<WAVEHDR>(nameof(WAVEHDR.dwLoops));

    /// <summary>A sound: its samples, pinned, and the wave header the device reads and writes, in unmanaged memory.</summary>
    private sealed class Sound
    {
        public Sound(byte[] wav, bool loops = false)
        {
            var data = GCHandle.Alloc(wav, GCHandleType.Pinned).AddrOfPinnedObject();
            Header = Marshal.AllocHGlobal(HeaderSize);
            var header = new WAVEHDR { lpData = data + WaveTone.HeaderLength, dwBufferLength = (uint)(wav.Length - WaveTone.HeaderLength) };
            Marshal.StructureToPtr(header, Header, false);
            Loops = loops;
        }

        public nint Header { get; }

        /// <summary>Once written, plays again without end until its device is reset.</summary>
        public bool Loops { get; }

        /// <summary>The device has not finished with the buffer: written and not yet played to the end.</summary>
        public bool Queued => (Marshal.ReadInt32(Header, FlagsOffset) & WHDR_INQUEUE) != 0;

        /// <summary>Marks the buffer as a loop of itself without end: set before each write, after the header was prepared (which wants the flags clear).</summary>
        public void MarkLoop()
        {
            Marshal.WriteInt32(Header, FlagsOffset, Marshal.ReadInt32(Header, FlagsOffset) | WHDR_BEGINLOOP | WHDR_ENDLOOP);
            Marshal.WriteInt32(Header, LoopsOffset, -1);
        }
    }

    /// <summary>One wave-out device and the sounds prepared for it: opened at the first sound, closed when a write fails, silent for the run when it cannot open.</summary>
    private sealed class Device
    {
        private readonly string _name;
        private readonly List<Sound> _sounds;
        private nint _handle;
        private bool _failed;

        public Device(string name, params Sound[] sounds)
        {
            _name = name;
            _sounds = [.. sounds];
        }

        /// <summary>
        /// One more sound for this device, made after the device (a chord of background work, made at its first use):
        /// prepared at once when the device is open, with the others when it opens.
        /// </summary>
        public void Add(Sound sound)
        {
            _sounds.Add(sound);
            if (_handle == 0 || _failed)
            {
                return;
            }

            var result = waveOutPrepareHeader(_handle, sound.Header, HeaderSize);
            if (result != 0)
            {
                // As after a failed write: closed, and the next sound opens the device and prepares every sound again.
                Log.Error($"waveOutPrepareHeader failed with {result}; the {_name} device is reopened at the next sound");
                Close();
            }
        }

        /// <summary>Queues the sound; with <paramref name="replace"/> it first stops and drops whatever is queued, otherwise it is skipped while it is queued itself.</summary>
        public void Play(Sound sound, bool replace)
        {
            if (_failed)
            {
                return;
            }

            try
            {
                if (_handle == 0 && !Open())
                {
                    return;
                }

                if (replace)
                {
                    waveOutReset(_handle);
                }
                else if (sound.Queued)
                {
                    return;
                }

                if (sound.Loops)
                {
                    sound.MarkLoop();
                }

                var result = waveOutWrite(_handle, sound.Header, HeaderSize);
                if (result != 0)
                {
                    // The device went away (unplugged, switched): the next sound opens the current one.
                    Log.Error($"waveOutWrite failed with {result}; the {_name} device is reopened at the next sound");
                    Close();
                }
            }
            catch (Exception ex) when (ex is DllNotFoundException or EntryPointNotFoundException)
            {
                // No winmm on this Windows: log once and stay quiet; the announcements still work.
                _failed = true;
                Log.Error($"No {_name} on this Windows", ex);
            }
        }

        /// <summary>Stops and drops whatever is queued; nothing while the device is not open.</summary>
        public void Reset()
        {
            if (_handle != 0)
            {
                waveOutReset(_handle);
            }
        }

        /// <summary>Opens the default output device for the sounds' format (16-bit mono PCM) and prepares every sound's buffer for it.</summary>
        private bool Open()
        {
            var format = new WAVEFORMATEX
            {
                wFormatTag = 1, // PCM
                nChannels = 1,
                nSamplesPerSec = WaveTone.SampleRate,
                nAvgBytesPerSec = WaveTone.SampleRate * 2,
                nBlockAlign = 2,
                wBitsPerSample = 16,
            };
            var result = waveOutOpen(out _handle, WAVE_MAPPER, ref format, 0, 0, 0);
            if (result != 0)
            {
                _handle = 0;
                _failed = true;
                Log.Error($"waveOutOpen failed with {result}; no {_name} this run");
                return false;
            }

            foreach (var sound in _sounds)
            {
                result = waveOutPrepareHeader(_handle, sound.Header, HeaderSize);
                if (result != 0)
                {
                    Log.Error($"waveOutPrepareHeader failed with {result}; no {_name} this run");
                    Close();
                    _failed = true;
                    return false;
                }
            }

            return true;
        }

        private void Close()
        {
            waveOutReset(_handle);
            foreach (var sound in _sounds)
            {
                waveOutUnprepareHeader(_handle, sound.Header, HeaderSize);
            }

            waveOutClose(_handle);
            _handle = 0;
        }
    }

    // A typewriter-like strike, a burst of noise over a 1.5 kHz tone that dies away within 10 ms: a line from Claude
    // arrived (the 1.3.0 sound, which the user likes; softer versions were tried in 2.4.0 and went unheard under
    // speech). A tiny mouth pop, 30 ms, its pitch falling from 700 towards 300 Hz: a tool call arrived. Chosen by the
    // user by ear on 2026-11-15, the first of three pops ("something more like a tiny mouth pop so it is not
    // elongated"); until then two lower knocks of noise and tone, 650 and 850 Hz, at 0.5, which he heard as jarring.
    private static readonly Sound LineTick = new(WaveTone.Strikes(0.4, 10, 0.5, 1500));
    private static readonly Sound ToolPop = new(WaveTone.Pop(0.3, 700, 300, 0.006, 0.030));

    // C6 alone, a high ping: Claude is ready. A4 alone, low: the message went out. C5, E5, G5 up to a long note:
    // Claude starts replying. The same notes down, G5, E5, C5: the turn is done. Two E5s: Claude needs an answer.
    // A quick C6-E6 trill: the answer notice opened in place of the conversation.
    private static readonly Sound ReadyChime = new(WaveTone.Notes(0.35, (1046.5, 120)));
    private static readonly Sound SentChime = new(WaveTone.Notes(0.35, (440, 80)));
    private static readonly Sound RespondingChime = new(WaveTone.Notes(0.35, (523.25, 90), (659.25, 90), (783.99, 220)));
    private static readonly Sound DoneChime = new(WaveTone.Notes(0.35, (783.99, 90), (659.25, 90), (523.25, 220)));
    private static readonly Sound QuestionChime = new(WaveTone.Notes(0.35, (659.25, 70), (0, 40), (659.25, 110)));
    private static readonly Sound NoticeChime = new(WaveTone.Notes(0.35, (1046.5, 45), (1318.5, 45), (1046.5, 45), (1318.5, 110)));

    // Five short swoops falling from 2400 to 900 Hz, 80 ms each, 400 ms in all: a command offered for the user to run
    // themselves (axClaude FR-2.12, asked 2026-11-13). An alarm, not a chime, louder than the chimes. Chosen by the
    // user by ear the same day, played to him one by one: two low knocks and a note an octave up were too mild; seven
    // fast struck taps, rising swoops, slower falling swoops and three of these were heard and passed over.
    private static readonly Sound OfferChime = new(WaveTone.Swoops(0.5, 2400, 900, 80, 5));

    // A3 alone, short and low: the edge, nothing there (a list's first or last entry under Up or Down, a typed name no
    // entry starts with; axFile AF-3.6). An earcon where a sentence would be noise, asked 2026-10-22. A4 falling to
    // A3, louder than the chimes: A3 alone at 0.35 was lost under the speech that follows it (2026-10-23), and A4
    // carries on small speakers where A3 hardly does.
    private static readonly Sound EdgeTick = new(WaveTone.Notes(0.6, (440, 45), (220, 90)));

    // The three sounds of a plain key in a text's navigation mode (AX-8.13), all chosen by the maintainer by ear on
    // 2026-11-16 from a script that played candidates. A high glassy tick, a pure 3800 Hz of 6 ms: the key was taken
    // as a command; over before NVDA speaks, and unlike axClaude's line tick, which the first click (2200 Hz with
    // noise) resembled. A buzzy 150 Hz tone of 110 ms: the command ran and there was nothing of its kind to go to. A
    // wooden bonk of 70 ms, falling from 420 Hz: the key has no command at all ("more deadpan and bonk like" than the
    // blips heard first). The candidates heard and kept for later are in docs/axKit/earcons.md, "Candidates kept".
    private static readonly Sound NavKeyClick = new(WaveTone.Tick(0.3, 3800, 3800, 0.006, 10, 700, 0.0));
    private static readonly Sound NothingTone = new(WaveTone.Flat(0.3, 150, 110, 6, buzzy: true));
    private static readonly Sound UnassignedKnock = new(WaveTone.Knock(0.4, 420, 260, 0.008, 0.022, 2.76, 0.5, 70));

    // E5 up to A5, short: a command recalled into a field (axTerminal AT-3.9, asked 2026-10-23), rising where the edge falls.
    private static readonly Sound RecallTick = new(WaveTone.Notes(0.45, (659.25, 35), (880, 50)));

    // G4 leaping an octave to G5, 85 ms in all: a move onto a heading (AX-7.9, asked 2026-11-11), before NVDA reads it.
    // An octave where the recall rises a fourth and the edge falls, and over before NVDA starts speaking.
    private static readonly Sound HeadingTick = new(WaveTone.Notes(0.45, (392, 30), (783.99, 55)));

    // G5, C6 up to a held E6, and once more: longer and louder than the chimes, so that Claude finishing or asking is
    // heard from another window, where the done chime was missed (axClaude FR-7.3).
    private static readonly Sound AlertChime = new(WaveTone.Notes(0.6, (783.99, 100), (1046.5, 100), (1318.5, 300), (0, 200), (783.99, 100), (1046.5, 100), (1318.5, 500)));

    // A pad of four slow chords with a soft high ping in each, 14 s that loop without a seam while Claude works
    // (axClaude FR-7.9; AmbientSounds.WorkingMusic, the one place it is made; asked 2026-11-15: gentler, like the lift
    // of a building of the future). On a device of its own. Before it: sixteen soft plucked notes, an arpeggio at
    // 500 ms a note (WaveTone.Plucks), and before that a version at half the chimes' peak with a music-box ring,
    // which was too much over NVDA's speech.
    private static readonly Sound WorkingMusic = new(AmbientSounds.WorkingMusic(), loops: true);

    // The sounds of work in the background (axClaude FR-7.10, asked 2026-11-15): a start and an end are events,
    // punctuation, three quick bell notes with a sparkle; what arrives rises, what leaves falls (his rule). The
    // recurring sound is a slow soft chord with a long tail, one of the working music's four at random, with a tone
    // more for each thing running. All were chosen by the user by ear (2026-11-15); each is one method of AxKit.Core's
    // AmbientSounds, held to what he heard by its tests.
    private static readonly Sound BackgroundStartTone = new(AmbientSounds.BackgroundStart());
    private static readonly Sound BackgroundFinishTone = new(AmbientSounds.BackgroundFinish());

    // The chords, by chord and number of things: made at their first use, off the UI thread (some tens of
    // milliseconds each, rule 3), and kept for the life of the process like every other sound; 200 KB each.
    private static readonly Sound?[,] BackgroundChords = new Sound?[AmbientSounds.ChordNames.Count, AmbientSounds.MostThings];
    private static readonly bool[,] BackgroundChordsBeingMade = new bool[AmbientSounds.ChordNames.Count, AmbientSounds.MostThings];

    private static readonly Device Chimes = new("sounds", [LineTick, ToolPop, ReadyChime, SentChime, RespondingChime, DoneChime, QuestionChime, NoticeChime, OfferChime, EdgeTick, RecallTick, HeadingTick, AlertChime, BackgroundStartTone, BackgroundFinishTone, NavKeyClick, NothingTone, UnassignedKnock]);
    private static readonly Device Music = new("working music", WorkingMusic);

    // The recurring chords of background work, 4.6 s each, on a third device of their own: on the chimes' device
    // every tick would wait behind a chord until it ended, and a chime's reset would cut it off.
    private static readonly Device ChordDevice = new("background work sound");

    /// <summary>
    /// Every sound asked for, by its name ("Notice", "Edge", "Ready"): the automation channel's record
    /// (docs/axKit/automation.md). In a test run the sounds are recorded and not played, so that a run on the test
    /// desktop is silent on the user's speakers.
    /// </summary>
    public static event Action<string>? Played;

    private static readonly bool Silent = AutomationEnvironment.Requested;

    private static bool _muted;
    private static bool _musicWanted;

    /// <summary>
    /// No sound at all while set: the process's one window is put away, hidden and running on (axClaude's window that
    /// hides on closing, docs/axClaude/SPEC.md FR-9.6), and a hidden window makes no sound. A sound asked for meanwhile
    /// is neither played nor recorded; the working music stops, and starts again when the mute ends while it is
    /// still wanted.
    /// </summary>
    public static bool Muted
    {
        get => _muted;
        set
        {
            if (_muted == value)
            {
                return;
            }

            _muted = value;
            if (Silent)
            {
                return;
            }

            if (value)
            {
                Music.Reset();
                ChordDevice.Reset();
            }
            else if (_musicWanted)
            {
                Music.Play(WorkingMusic, replace: false);
            }
        }
    }

    private static void Play(string name, Action play)
    {
        if (_muted)
        {
            return;
        }

        Played?.Invoke(name);
        if (!Silent)
        {
            play();
        }
    }

    /// <summary>A tiny typewriter-like tick: a line from Claude arrived.</summary>
    public static void Click() => Play("Click", () => Chimes.Play(LineTick, replace: false));

    /// <summary>A tiny mouth pop: a tool call arrived.</summary>
    public static void Tool() => Play("Tool", () => Chimes.Play(ToolPop, replace: false));

    /// <summary>A single high ping: Claude is ready.</summary>
    public static void Ready() => Play("Ready", () => Chimes.Play(ReadyChime, replace: true));

    /// <summary>A single low note: the message went out.</summary>
    public static void Sent() => Play("Sent", () => Chimes.Play(SentChime, replace: true));

    /// <summary>Three rising notes ending on a long one: Claude started replying.</summary>
    public static void Responding() => Play("Responding", () => Chimes.Play(RespondingChime, replace: true));

    /// <summary>Three falling notes ending on a long one: the turn is done.</summary>
    public static void Done() => Play("Done", () => Chimes.Play(DoneChime, replace: true));

    /// <summary>The Done chime through the mute: for a window put away whose one switch says a turn's end is still heard (axClaude FR-9.6, <c>DoneChimeWhilePutAway</c>, off).</summary>
    public static void DoneWhileMuted()
    {
        Played?.Invoke("Done");
        if (!Silent)
        {
            Chimes.Play(DoneChime, replace: true);
        }
    }

    /// <summary>Two equal notes: Claude needs an answer.</summary>
    public static void Question() => Play("Question", () => Chimes.Play(QuestionChime, replace: true));

    /// <summary>A quick high trill: the answer notice opened and waits for an answer.</summary>
    public static void Notice() => Play("Notice", () => Chimes.Play(NoticeChime, replace: true));

    /// <summary>Five short high falling swoops, an alarm: a command is offered for the user to run themselves, not a question of Claude's.</summary>
    public static void Offer() => Play("Offer", () => Chimes.Play(OfferChime, replace: true));

    /// <summary>One short low note: the edge, nothing there (a key that moves nothing, a typed name no entry starts with).</summary>
    public static void Edge() => Play("Edge", () => Chimes.Play(EdgeTick, replace: true));

    /// <summary>A high glassy tick: a plain letter or digit was taken as a command in a text's navigation mode (AX-8.13), played before the command runs. It replaces what plays, as the edge does.</summary>
    public static void NavKey() => Play("NavKey", () => Chimes.Play(NavKeyClick, replace: true));

    /// <summary>One low buzz: a navigation key ran its command and there was nothing of that kind to go to (AX-8.13).</summary>
    public static void Nothing() => Play("Nothing", () => Chimes.Play(NothingTone, replace: true));

    /// <summary>A wooden bonk: a plain key in navigation mode that has no command at all (AX-8.13); the edge stays for the ends of lists and texts and for a refused change.</summary>
    public static void Unassigned() => Play("Unassigned", () => Chimes.Play(UnassignedKnock, replace: true));

    /// <summary>Two quick rising notes: another command came into the field from the history (axTerminal AT-3.9).</summary>
    public static void Recall() => Play("Recall", () => Chimes.Play(RecallTick, replace: true));

    /// <summary>A low note leaping an octave: the user's key moved onto a heading (AX-7.9), played before NVDA reads it.</summary>
    public static void Heading() => Play("Heading", () => Chimes.Play(HeadingTick, replace: true));

    /// <summary>A longer, louder call, three rising notes to a held one and again: Claude is done or asks while the window is in the background.</summary>
    public static void Alert() => Play("Alert", () => Chimes.Play(AlertChime, replace: true));

    /// <summary>Three quick bell notes up, with a sparkle: work started in the background (axClaude FR-7.10). Behind whatever plays, never cutting a chime.</summary>
    public static void BackgroundStart() => Play("BackgroundStart", () => Chimes.Play(BackgroundStartTone, replace: false));

    /// <summary>The same three notes down: work in the background ended.</summary>
    public static void BackgroundFinish() => Play("BackgroundFinish", () => Chimes.Play(BackgroundFinishTone, replace: false));

    /// <summary>
    /// One chord of the recurring sound while work runs in the background: <paramref name="chord"/> of
    /// <see cref="AmbientSounds.ChordNames"/>, with a tone more for each of <paramref name="things"/> running, five at
    /// most; nothing for none. On its own device, so it sounds with the chimes, the ticks and the music and delays
    /// none of them; the caller (axClaude's <c>BackgroundChords</c>) starts one only after the last has ended. The
    /// first time a chord is asked for, it is made off the UI thread and played as soon as it is ready, a few tens
    /// of milliseconds later; from then on it is kept. Recorded as "BackgroundChord 2", by the number of things.
    /// </summary>
    public static void BackgroundChord(int chord, int things)
    {
        if (things < 1 || chord < 0 || chord >= AmbientSounds.ChordNames.Count)
        {
            return;
        }

        var count = Math.Min(things, AmbientSounds.MostThings);
        Play("BackgroundChord " + count, () => PlayChord(chord, count));
    }

    /// <summary>Plays the chord when it is made; otherwise makes it on a pool thread and plays it back on this, the UI thread.</summary>
    private static void PlayChord(int chord, int count)
    {
        if (BackgroundChords[chord, count - 1] is { } made)
        {
            ChordDevice.Play(made, replace: false);
            return;
        }

        if (BackgroundChordsBeingMade[chord, count - 1])
        {
            return;
        }

        var ui = SynchronizationContext.Current;
        if (ui is null)
        {
            // Not on a window's thread (no such caller today): made here, at the cost of the wait.
            Keep(chord, count, AmbientSounds.BackgroundChord(chord, count));
            return;
        }

        BackgroundChordsBeingMade[chord, count - 1] = true;
        Task.Run(() => AmbientSounds.BackgroundChord(chord, count)).ContinueWith(task => ui.Post(_ =>
        {
            BackgroundChordsBeingMade[chord, count - 1] = false;
            if (task.IsFaulted)
            {
                Log.Error("The background work sound could not be made", task.Exception);
                return;
            }

            Keep(chord, count, task.Result);
        }, null), TaskScheduler.Default);
    }

    /// <summary>A chord just made: kept, prepared for its device and played, unless the window was put away meanwhile.</summary>
    private static void Keep(int chord, int count, byte[] wav)
    {
        var sound = new Sound(wav);
        BackgroundChords[chord, count - 1] = sound;
        ChordDevice.Add(sound);
        if (!_muted)
        {
            ChordDevice.Play(sound, replace: false);
        }
    }

    /// <summary>Starts the soft pad loop that plays while Claude works (axClaude FR-7.9); nothing while it plays already.</summary>
    public static void StartMusic()
    {
        _musicWanted = true;
        Play("StartMusic", () => Music.Play(WorkingMusic, replace: false));
    }

    /// <summary>Stops the music loop at once; nothing when it is not playing.</summary>
    public static void StopMusic()
    {
        _musicWanted = false;
        Play("StopMusic", () => Music.Reset());
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct WAVEFORMATEX
    {
        public ushort wFormatTag;
        public ushort nChannels;
        public uint nSamplesPerSec;
        public uint nAvgBytesPerSec;
        public ushort nBlockAlign;
        public ushort wBitsPerSample;
        public ushort cbSize;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct WAVEHDR
    {
        public nint lpData;
        public uint dwBufferLength;
        public uint dwBytesRecorded;
        public nint dwUser;
        public uint dwFlags;
        public uint dwLoops;
        public nint lpNext;
        public nint reserved;
    }

    [DllImport("winmm.dll")]
    private static extern uint waveOutOpen(out nint hwo, uint uDeviceID, ref WAVEFORMATEX pwfx, nint dwCallback, nint dwInstance, uint fdwOpen);

    [DllImport("winmm.dll")]
    private static extern uint waveOutPrepareHeader(nint hwo, nint pwh, int cbwh);

    [DllImport("winmm.dll")]
    private static extern uint waveOutUnprepareHeader(nint hwo, nint pwh, int cbwh);

    [DllImport("winmm.dll")]
    private static extern uint waveOutWrite(nint hwo, nint pwh, int cbwh);

    [DllImport("winmm.dll")]
    private static extern uint waveOutReset(nint hwo);

    [DllImport("winmm.dll")]
    private static extern uint waveOutClose(nint hwo);
}
