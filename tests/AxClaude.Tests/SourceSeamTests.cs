using System.Reflection;
using System.Text;
using AxClaude.Core.Transcript;

namespace AxClaude.Tests;

/// <summary>
/// The seam between axClaude's window and what feeds it (docs/axClaude/stream-mode.md SM-3): the window reads its
/// source through <see cref="ClaudeModel"/> alone, so that a second source, Claude Code's stream, can stand where the
/// screen stands. Two things would wear the seam away without a word from the compiler, and both are read off the
/// window's sources here: a member typed as the screen's own model, and the window's own logic asking the screen's
/// model where it should ask the one it is given. The machinery beside the screen (the turn marks, the session file,
/// the hooks, the event core) is the screen source's own and keeps asking the screen's model, in its own files.
/// Comments are not looked at, since they name the classes to explain the code.
/// </summary>
public sealed class SourceSeamTests
{
    private const string Window = "src/AxClaude";

    /// <summary>The file that makes the model and hands it to the window base, and so names its class.</summary>
    private const string Maker = "MainForm.cs";

    /// <summary>The window's field for the screen's own model.</summary>
    private const string ScreenField = "_screenModel";

    /// <summary>The files of the machinery beside the screen (SM-3.6).</summary>
    private static readonly string[] Machinery = ["MainForm.Flow.cs", "MainForm.Record.cs", "MainForm.Hooks.cs"];

    /// <summary>The window's field for the stream's own model, and the one file of the stream source (SM-9.2).</summary>
    private const string StreamField = "_streamModel";

    private const string StreamSource = "MainForm.Stream.cs";

    [Fact]
    public void The_window_names_the_screens_model_only_where_it_makes_it()
    {
        var uses = UsesOf(nameof(SessionModel));
        var elsewhere = uses.Where(use => use.File != Maker).Select(use => use.ToString()).ToList();
        Assert.True(elsewhere.Count == 0, "The window knows ClaudeModel alone; the screen's SessionModel is named where the window makes it, in MainForm.cs (stream-mode.md SM-3.7): " + string.Join("; ", elsewhere));

        // Three lines of MainForm.cs: the field of the machinery, the model made, and the field set.
        Assert.Equal(3, uses.Count);
    }

    [Fact]
    public void The_screens_model_is_asked_by_the_machinery_beside_the_screen_alone()
    {
        var uses = UsesOf(ScreenField);
        var elsewhere = uses.Where(use => use.File != Maker && !Machinery.Contains(use.File)).Select(use => use.ToString()).ToList();
        Assert.True(elsewhere.Count == 0, "The window's own logic reads _model, whichever source feeds it; _screenModel is for the turn marks, the session file, the hooks and the event core, in their files (stream-mode.md SM-3.6): " + string.Join("; ", elsewhere));

        // In the file that makes it: declared and set, and never asked. It is the model only when the model is the
        // screen's, and nothing in a window the stream feeds.
        var made = uses.Where(use => use.File == Maker).Select(use => use.Code.Trim()).ToList();
        Assert.Equal(new[] { "private readonly SessionModel? _screenModel;", "_screenModel = model as SessionModel;" }, made);

        // No stale rule: the machinery does ask it.
        Assert.Contains(uses, use => Machinery.Contains(use.File));
    }

    [Fact]
    public void The_window_names_the_streams_model_only_where_it_makes_it_and_in_the_stream_sources_file()
    {
        var uses = UsesOf(nameof(AxClaude.Core.Streaming.StreamModel));
        var elsewhere = uses.Where(use => use.File != Maker && use.File != StreamSource).Select(use => use.ToString()).ToList();
        Assert.True(elsewhere.Count == 0, "The window knows ClaudeModel alone; the stream's StreamModel is named where the window makes it and in the stream source's own file (stream-mode.md SM-9.2): " + string.Join("; ", elsewhere));

        // Three lines of MainForm.cs, as for the screen's: the field, the model made, and the field set.
        Assert.Equal(3, uses.Count(use => use.File == Maker));
    }

    [Fact]
    public void The_streams_model_is_asked_by_the_stream_source_alone()
    {
        var uses = UsesOf(StreamField);
        var elsewhere = uses.Where(use => use.File != Maker && use.File != StreamSource).Select(use => use.ToString()).ToList();
        Assert.True(elsewhere.Count == 0, "The window's own logic reads _model, whichever source feeds it, and asks Streams where the two sources part; _streamModel is the stream source's, in MainForm.Stream.cs (stream-mode.md SM-9.2): " + string.Join("; ", elsewhere));

        var made = uses.Where(use => use.File == Maker).Select(use => use.Code.Trim()).ToList();
        Assert.Equal(new[] { "private readonly StreamModel? _streamModel;", "_streamModel = model as StreamModel;" }, made);

        Assert.Contains(uses, use => use.File == StreamSource);
    }

    [Fact]
    public void One_model_is_made_by_the_source_the_start_names()
    {
        // One window, one source (SM-9.2): the one line that makes a model makes the stream's or the screen's, by
        // what the command line and the setting say, and nothing else in the window makes one.
        var makes = CodeLines().Where(line => line.Code.Contains("new SessionModel(", StringComparison.Ordinal) || line.Code.Contains("new StreamModel(", StringComparison.Ordinal)).ToList();
        var made = Assert.Single(makes);
        Assert.Equal(Maker, made.File);
        // The Store's copy is told, since there the setting does not choose the stream (SM-9.6).
        Assert.Contains("options.Streams(settings, PackageIdentity.IsPackaged) ? new StreamModel() : new SessionModel(", made.Code);
    }

    [Fact]
    public void The_window_holds_its_source_as_the_base_class()
    {
        // The field's declaration: the one line of the window that ends with the field's name.
        var declared = CodeLines().Select(line => line.Code.Trim()).Where(code => code.EndsWith(" _model;", StringComparison.Ordinal)).ToList();
        Assert.Equal(new[] { "private readonly ClaudeModel _model;" }, declared);
    }

    /// <summary>
    /// What only the machinery beside the screen asks stays off the base class (SM-3.6): a model without a screen has
    /// no turn marks and no screen's work, and the event core's word about a turn is handed to the screen's model alone.
    /// </summary>
    [Theory]
    [InlineData(nameof(SessionModel.Marked))]
    [InlineData(nameof(SessionModel.Work))]
    [InlineData(nameof(SessionModel.Epoch))]
    [InlineData(nameof(SessionModel.TurnOpen))]
    public void What_the_screens_machinery_asks_is_the_screen_models_own(string member)
    {
        const BindingFlags all = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;
        Assert.Empty(typeof(ClaudeModel).GetMember(member, all));
        Assert.NotEmpty(typeof(SessionModel).GetMember(member, all | BindingFlags.DeclaredOnly));
    }

    [Fact]
    public void The_screens_model_is_one_model_of_the_base_class()
    {
        Assert.True(typeof(ClaudeModel).IsAbstract);
        Assert.Equal(typeof(ClaudeModel), typeof(SessionModel).BaseType);
        Assert.True(typeof(SessionModel).IsSealed);
    }

    [Theory]
    [InlineData("    private readonly SessionModel _screenModel;", "    private readonly SessionModel _screenModel;")]
    [InlineData("    /// <see cref=\"SessionModel.TurnOpen\"/>", "")]
    [InlineData("    // SessionModel", "")]
    [InlineData("    Call(x); // SessionModel", "    Call(x); ")]
    [InlineData("    Say(\"SessionModel // not a comment\", y);", "    Say(, y);")]
    [InlineData("    Say(\"a \\\" SessionModel\", y);", "    Say(, y);")]
    [InlineData("    Say(@\"C:\\\", SessionModel);", "    Say(@, SessionModel);")]
    [InlineData("    Say($@\"a \"\"b\"\" c\", z);", "    Say($@, z);")]
    [InlineData("    var c = '\"'; SessionModel m;", "    var c = ; SessionModel m;")]
    [InlineData("    var c = '\\''; SessionModel m;", "    var c = ; SessionModel m;")]
    public void A_lines_code_leaves_out_comments_and_what_stands_in_quotes(string line, string code)
    {
        Assert.Equal(code, CodeOf(line));
    }

    [Theory]
    [InlineData("_screenModel.Marked", "_screenModel", true)]
    [InlineData("x = _model;", "_model", true)]
    [InlineData("_screenModel.Marked", "_model", false)]
    [InlineData("_modelOld", "_model", false)]
    [InlineData("new SessionModel(", "SessionModel", true)]
    [InlineData("new SessionModelish(", "SessionModel", false)]
    public void A_name_is_found_whole_only(string code, string name, bool found)
    {
        Assert.Equal(found, HoldsWord(code, name));
    }

    /// <summary>One line of the window's code: the file's name, the line's number, the line without comments and quoted text.</summary>
    private sealed record CodeLine(string File, int Line, string Code)
    {
        public override string ToString() => $"{File}:{Line}: {Code.Trim()}";
    }

    private static List<CodeLine> UsesOf(string name) => CodeLines().Where(line => HoldsWord(line.Code, name)).ToList();

    /// <summary>Every line of the window's C# files that holds code, bin and obj left out.</summary>
    private static List<CodeLine> CodeLines()
    {
        var root = SourceTexts.RepositoryRoot();
        var separator = Path.DirectorySeparatorChar;
        var found = new List<CodeLine>();
        foreach (var file in Directory.GetFiles(Path.Combine(root, Window.Replace('/', separator)), "*.cs", SearchOption.AllDirectories))
        {
            var relative = Path.GetRelativePath(root, file).Replace(separator, '/');
            if (relative.Contains("/obj/", StringComparison.Ordinal) || relative.Contains("/bin/", StringComparison.Ordinal))
            {
                continue;
            }

            var number = 0;
            foreach (var line in File.ReadLines(file))
            {
                number++;
                var code = CodeOf(line);
                if (code.Trim().Length > 0)
                {
                    found.Add(new CodeLine(Path.GetFileName(file), number, code));
                }
            }
        }

        // The reading found the window: its files are many, and the one that makes the model is among them.
        Assert.Contains(found, line => line.File == Maker);
        return found;
    }

    /// <summary>
    /// A line of C# without its comment and without what stands between quotes: a line that is a comment gives
    /// nothing, a comment that starts outside a string ends the line, and a string or a character is left out with
    /// its quotes (plain strings with backslash escapes, verbatim strings with doubled quotes). A block comment or a
    /// raw string over several lines is not understood, as in <see cref="SourceTexts"/>: the window has neither around
    /// the names looked for.
    /// </summary>
    private static string CodeOf(string line)
    {
        if (line.TrimStart().StartsWith("//", StringComparison.Ordinal))
        {
            return string.Empty;
        }

        var code = new StringBuilder();
        var i = 0;
        while (i < line.Length)
        {
            var c = line[i];
            if (c == '/' && i + 1 < line.Length && line[i + 1] == '/')
            {
                break;
            }

            if (c == '\'')
            {
                // A character: past what it holds, then past its closing quote.
                i += i + 1 < line.Length && line[i + 1] == '\\' ? 3 : 2;
                while (i < line.Length && line[i] != '\'')
                {
                    i++;
                }

                i++;
                continue;
            }

            if (c != '"')
            {
                code.Append(c);
                i++;
                continue;
            }

            var verbatim = i > 0 && (line[i - 1] == '@' || (line[i - 1] == '$' && i > 1 && line[i - 2] == '@'));
            i++;
            while (i < line.Length)
            {
                if (verbatim && line[i] == '"' && i + 1 < line.Length && line[i + 1] == '"')
                {
                    i += 2;
                    continue;
                }

                if (!verbatim && line[i] == '\\')
                {
                    i += 2;
                    continue;
                }

                if (line[i] == '"')
                {
                    break;
                }

                i++;
            }

            i++;
        }

        return code.ToString();
    }

    /// <summary>Whether the code holds the name with no letter, digit or underscore directly in front of it or behind it.</summary>
    private static bool HoldsWord(string code, string name)
    {
        for (var at = code.IndexOf(name, StringComparison.Ordinal); at >= 0; at = code.IndexOf(name, at + 1, StringComparison.Ordinal))
        {
            var before = at == 0 || !IsWordCharacter(code[at - 1]);
            var end = at + name.Length;
            var after = end == code.Length || !IsWordCharacter(code[end]);
            if (before && after)
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsWordCharacter(char c) => char.IsLetterOrDigit(c) || c == '_';
}
