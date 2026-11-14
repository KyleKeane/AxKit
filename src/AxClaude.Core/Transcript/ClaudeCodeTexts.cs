using System.Text.RegularExpressions;

namespace AxClaude.Core.Transcript;

/// <summary>
/// The one table of Claude Code's own texts (root CLAUDE.md rule 12; repair R-28): every label, row, hint, mark and
/// pattern that axClaude recognises on Claude Code's screen in screen reader mode, and the few texts it sends or
/// compares as Claude Code's own. The classes that recognise things (<see cref="LineClassifier"/>,
/// <see cref="QuestionReader"/>, <see cref="ScreenReader"/>, <see cref="ScreenActions"/>, <see cref="SurveyReader"/>,
/// <see cref="SessionModel"/>, <see cref="Arrivals"/>, the window) keep their logic and read the texts from here.
/// When Claude Code changes its words, this file is adjusted and nothing the user hears or presses moves; a test
/// (<c>ClaudeCodeTextsTests</c>) fails when one of the most characteristic texts is written anywhere else.
/// The names of the slash commands are the table <see cref="SlashCommands.All"/>, which stays where it is. The
/// prose catalogue of the screens is docs/axClaude/claude-screens.md. A second agent program would get a table of
/// its own of the same shape; nothing here is an abstraction over programs.
/// Constants and generated regexes only: reading one costs nothing, which <see cref="SessionModel"/>'s frame end needs.
/// Where an entry names a version, that is the Claude Code version it was recorded with; a fixture is one under
/// tests/fixtures.
/// </summary>
public static partial class ClaudeCodeTexts
{
    // ---- Line labels: what a row of the conversation starts with (SPEC.md §4.4; every fixture) ----

    /// <summary>The echo of the user's message, and since 2.1.281 of a slash command (<c>you: /model</c>).</summary>
    public const string YouLabel = "you:";

    /// <summary>The first row of a reply.</summary>
    public const string ClaudeLabel = "claude:";

    public const string ThinkingLabel = "thinking:";

    public const string ToolErrorLabel = "tool error:";

    /// <summary>The row that names a tool call, <c>tool: Read (a.txt)</c>.</summary>
    public const string ToolLabel = "tool:";

    public const string ErrorLabel = "error:";

    /// <summary>Also inside a question: "warning: You have not answered all questions" on the review of several answers.</summary>
    public const string WarningLabel = "warning:";

    /// <summary>The row Claude prints at startup, <c>[Screen Reader Mode: …]</c>.</summary>
    public const string ScreenReaderModeRow = "[Screen Reader Mode:";

    /// <summary>The hint Claude appends to a collapsed tool row (<c>tool: Reading 1 file… (ctrl+o to expand)</c>).</summary>
    public const string ExpandHint = "(ctrl+o to expand)";

    /// <summary>The echo of /clear, after which Claude's conversation is empty (2.1.319, the clear fixture).</summary>
    public const string ClearEcho = "you: /clear";

    /// <summary>The summary row after a turn, "Brewed for 12s · done …".</summary>
    [GeneratedRegex(@"^\S+ for [\dhms ]+ · done ")]
    public static partial Regex TurnSummary();

    // ---- Claude's own prompt row, the banner and the window title ----

    /// <summary>What Claude's prompt row starts with: the dollar sign, and the two other marks it has shown.</summary>
    public const char PromptDollar = '$';
    public const char PromptAngle = '>';
    public const char PromptBang = '!';

    /// <summary>The no-break space Claude prints after the dollar sign of a draft, which tells the draft row from a command line quoted in a reply.</summary>
    public const char DraftSpace = ' ';

    /// <summary>What a slash command starts with.</summary>
    public const char CommandStart = '/';

    /// <summary>The first row of the banner, "Claude Code v2.1.309": these words, then a digit.</summary>
    public const string BannerStart = "Claude Code v";

    /// <summary>The window title before a session has a name.</summary>
    public const string TitleWithoutName = "Claude Code";

    /// <summary>The state glyphs a session name's title starts with, "✳ name".</summary>
    public const string TitleGlyphs = "◐◑◒◓✳✶✻✽✢·";

    /// <summary>What Claude says when --continue finds nothing (SPEC.md FR-9.1).</summary>
    public const string NoConversationToContinue = "No conversation found to continue";

    // ---- Chrome: the rows of the interface around the prompt ----

    /// <summary>"Forming…" while thinking; "Running…  (3s · timeout 1m)" while a Bash tool runs.</summary>
    [GeneratedRegex(@"^(\S+)…(\s+\(.*\))?$")]
    public static partial Regex Spinner();

    /// <summary>The ellipsis of a spinner, and of "Press Enter to continue…".</summary>
    public const string Ellipsis = "…";
    public const char EllipsisCharacter = '…';

    /// <summary>
    /// The same timer on a row of its own under a running Bash tool, "(4s · timeout 3m)", redrawn every second; as a
    /// conversation row it moved the caret and NVDA's highlight once a second while a command ran (2026-10-22).
    /// </summary>
    [GeneratedRegex(@"^\s*\((?:\d+[hms] ?)+ · timeout [\dhms ]+\)$")]
    public static partial Regex ToolTimer();

    /// <summary>
    /// A running subagent, one row each above the spinner, redrawn every second with its time and token count
    /// (§4.4 item 21): "◯  general-purpose Checking callers 2m 15s · ↓ 1.2k tokens".
    /// </summary>
    [GeneratedRegex(@"^\s*◯\s+.*\d+[hms](?: \d+[ms])?(?: · [↓↑] [\d.]+[kM]? tokens)?$")]
    public static partial Regex AgentRow();

    /// <summary>
    /// The git branch Claude prints under its mode line in a repository, "  ● main" (2.1.300), rewritten with the
    /// mode line and gone when the next rows are drawn.
    /// </summary>
    [GeneratedRegex(@"^\s*● \S+$")]
    public static partial Regex BranchRow();

    /// <summary>Shown under a message that was sent while Claude was busy; the message itself is drawn just above it (the steering fixture).</summary>
    [GeneratedRegex(@"^ctrl\+x ctrl\+s to send now$")]
    public static partial Regex QueuedHint();

    [GeneratedRegex(@"^\(ctrl\+b to run in background\)$")]
    public static partial Regex BackgroundHint();

    /// <summary>
    /// The mode line: "auto mode on (shift+tab to cycle)", "auto mode on (shift+tab to cycle)  ·  esc to interrupt",
    /// "manual mode on" (the default mode shows no cycle hint), "accept edits on (shift+tab to cycle)". While
    /// background work runs the cycle hint gives way to a count and a hint: "auto mode on  ·  1 shell  ·  esc to
    /// interrupt · ↓ to manage". Group 1 is the mode, group 2 what follows the cycle hint.
    /// </summary>
    [GeneratedRegex(@"^((?:\w+ mode|accept edits|bypass permissions|dangerously skip permissions) (?:on|off))(?:\s+\(shift\+tab to cycle\))?((?:\s+·\s+(?:esc to interrupt|↓ to manage|\d+ [a-z][a-z -]*[a-z]))*)$")]
    public static partial Regex ModeLine();

    /// <summary>On the mode line while Claude works (also inside <see cref="ModeLine"/>).</summary>
    public const string InterruptHint = "esc to interrupt";

    /// <summary>On the mode line while background work can be managed (also inside <see cref="ModeLine"/>).</summary>
    public const string ManageHint = "↓ to manage";

    /// <summary>What the mode line counts for a queued feedback draft, "1 feedback draft": the words, as a counted part is tested for them.</summary>
    public const string FeedbackDraft = "feedback draft";

    /// <summary>The same words with the space after the count, as the count in front of them is found. Two spellings since before this table; both kept.</summary>
    public const string FeedbackDraftAfterCount = " feedback draft";

    /// <summary>The separator of the mode line's parts and of a hint row's keys.</summary>
    public const char PartSeparator = '·';

    /// <summary>Claude Code's own screen reader announcements: a bracketed row under the prompt for a moment, "[accept edits on]".</summary>
    [GeneratedRegex(@"^\[[a-z][^\[\]]{0,120}\]$")]
    public static partial Regex AnnouncementRow();

    /// <summary>Ctrl+O replaces the prompt with this status row; typed messages go nowhere until Ctrl+O is pressed again.</summary>
    [GeneratedRegex(@"^Showing detailed transcript · ctrl\+o to toggle")]
    public static partial Regex TranscriptViewRow();

    /// <summary>A tip row, "Tip: auto mode handles these prompts for you".</summary>
    [GeneratedRegex(@"^Tip: ")]
    public static partial Regex Tip();

    /// <summary>The same start as a plain prefix, as a question's rows are tested for a tip inside it (also inside <see cref="Tip"/>).</summary>
    public const string TipStart = "Tip: ";

    [GeneratedRegex(@"^ctrl\+g to edit in Notepad$")]
    public static partial Regex NotepadHint();

    [GeneratedRegex(@"^effort: .* · /effort$")]
    public static partial Regex EffortHint();

    /// <summary>The slash-command completion list: "/name    description" rows.</summary>
    [GeneratedRegex(@"^/\S*\s{2,}\S")]
    public static partial Regex CompletionRow();

    /// <summary>The indented continuation rows of the completion list.</summary>
    [GeneratedRegex(@"^\s{20,}\S")]
    public static partial Regex CompletionContinuation();

    // ---- Questions: a prompt row under numbered or y/n answers (SPEC.md D33) ----

    /// <summary>A permission or trust question's title row, which the trust dialog puts on a prompt row itself.</summary>
    public const string PermissionRequired = "Permission Required:";

    public const string EnterSelection = "Enter selection";

    public const string EnterYesNo = "Enter y/n";

    /// <summary>"Select with numbers [1-5]. …".</summary>
    public const string SelectWithNumbers = "Select with numbers";

    /// <summary>/config's prompt row, "Enter a number to change [1-44], or Escape to save and close:" (2.1.282).</summary>
    public const string EnterNumberToChange = "Enter a number to change";

    /// <summary>The row where Claude waits for the user's own words: "Enter text for option 4 (Other), or Escape for the list:" (the other-answer fixture).</summary>
    public const string EnterTextFor = "Enter text for";

    /// <summary>The answer a text row asks the words for, group 1: "Other" in "Enter text for option 4 (Other), or Escape for the list:".</summary>
    [GeneratedRegex(@"^Enter text for option \d+ \((.+?)\)[,:]")]
    public static partial Regex TextPrompt();

    /// <summary>On a prompt row that takes more than one answer: "Select with numbers [1-4] (comma- or space-separated for several)".</summary>
    public const string ForSeveral = "for several";

    /// <summary>The highest answer a prompt row names, group 1: "5" in "Select with numbers [1-5]".</summary>
    [GeneratedRegex(@"\[1-(\d+)\]")]
    public static partial Regex LastAnswer();

    /// <summary>The start of an answer row, "1. " or "y. ": how a row under a prompt row is classified.</summary>
    [GeneratedRegex(@"^(\d+|[yn])\. ")]
    public static partial Regex AnswerStart();

    /// <summary>An answer row whole: group 1 the key, group 2 "(selected)" before the current answer, group 3 the text.</summary>
    [GeneratedRegex(@"^(\d+|[yn])\. (?:(\(selected\)) )?(.*)$")]
    public static partial Regex AnswerRow();

    /// <summary>The keys of a yes-or-no question.</summary>
    public const string Yes = "y";
    public const string No = "n";

    /// <summary>The same keys as capitals, which the window takes as an answer too while a question could not be read.</summary>
    public const string YesCapital = "Y";
    public const string NoCapital = "N";

    /// <summary>The quote mark in front of a question's row since 2.1.316 ("&gt; Which colour …"), which is not read.</summary>
    public const string QuoteMark = "> ";

    /// <summary>
    /// AskUserQuestion draws its header as " ☐ Colour" (☒ once answered); only the recorded marks, since a plan's own
    /// rows can start with other ticks.
    /// </summary>
    [GeneratedRegex(@"^\s*[☐☒✔]\s*")]
    public static partial Regex CheckBox();

    /// <summary>The marks of a question still to answer and of one answered, on the tab row of several questions.</summary>
    public const char Unanswered = '☐';
    public const char Answered = '☒';

    /// <summary>With several questions in one call, a tab row above each: "←   ☐ Colour   ☐ Fruit   ✔ Submit   →".</summary>
    [GeneratedRegex(@"^←.*→$")]
    public static partial Regex QuestionTabs();

    /// <summary>
    /// Since Claude Code 2.1.316 a header row instead: "Questions: Colour (answered), Fruit", and each question's own
    /// row starts with "> " (the several-questions-claude2.1.316 fixture).
    /// </summary>
    [GeneratedRegex(@"^Questions: (.+)$")]
    public static partial Regex QuestionsHeader();

    /// <summary>What the header row says after each question answered.</summary>
    public const string AnsweredMark = " (answered)";

    /// <summary>What stands between the questions a header row names.</summary>
    public const string HeaderSeparator = ", ";

    /// <summary>
    /// One question alone has "Question: Colour" over its row since 2.1.319 (the one-question-claude2.1.319 fixture):
    /// the same header with nothing to count.
    /// </summary>
    [GeneratedRegex(@"^Question: .+$")]
    public static partial Regex QuestionHeader();

    /// <summary>The title of the review of several answers, which the app can submit itself.</summary>
    public const string ReviewTitle = "Review your answers";

    /// <summary>The answer after which Claude asks for the user's own words.</summary>
    public const string OtherAnswer = "Other";

    /// <summary>What stands between an answer's name and Claude's explanation of it.</summary>
    public const string AnswerDash = " — ";

    // ---- /config's rows ("Show tips: true", "Thinking mode: true (Thinking can't be turned off …)"; 2.1.282) ----

    public const string SettingSeparator = ": ";
    public const string SettingTrue = "true";
    public const string SettingFalse = "false";

    /// <summary>A switch's value with a remark after it.</summary>
    public const string SettingTrueThen = "true ";
    public const string SettingFalseThen = "false ";

    // ---- Screens that wait on a hint row or a tab row (docs/axClaude/claude-screens.md) ----

    /// <summary>
    /// One part of a hint row: "Esc to close", "Esc/Enter/Space to close", "↑/↓ to select", "x to stop", "ctrl+j to
    /// new line": a key, or keys joined by "/", then "to". "⇧←/→ to browse" (a second /btw, 2.1.310) is Shift with
    /// both arrows. The groups are named keys and action.
    /// </summary>
    [GeneratedRegex(@"^(?<keys>(?:Esc|Enter|Space|Tab|ctrl\+[a-z]|⇧?[←→↑↓]|[a-z])(?:/(?:Esc|Enter|Space|Tab|ctrl\+[a-z]|⇧?[←→↑↓]|[a-z]))*) to (?<action>.+)$")]
    public static partial Regex Hint();

    /// <summary>What joins the keys of one hint, "Esc/Enter/Space".</summary>
    public const char KeyJoin = '/';

    /// <summary>The names a hint row gives its keys (all inside <see cref="Hint"/> too).</summary>
    public const string KeyEsc = "Esc";
    public const string KeyEnter = "Enter";
    public const string KeySpace = "Space";
    public const string KeyTab = "Tab";
    public const string KeyLeft = "←";
    public const string KeyRight = "→";
    public const string KeyUp = "↑";
    public const string KeyDown = "↓";
    public const char KeyShift = '⇧';
    public const string KeyShiftLeft = "⇧←";
    public const string KeyShiftRight = "⇧→";
    public const string KeyShiftUp = "⇧↑";
    public const string KeyShiftDown = "⇧↓";

    /// <summary>"ctrl+j": this, then the letter.</summary>
    public const string KeyCtrl = "ctrl+";

    /// <summary>The actions of a hint row that close its screen.</summary>
    public const string ActionClose = "close";
    public const string ActionDismiss = "dismiss";
    public const string ActionCancel = "cancel";
    public const string ActionExit = "exit";

    /// <summary>The action of the one key of "Any key to continue" and "Press Enter to continue".</summary>
    public const string ActionContinue = "continue";

    /// <summary>The action Escape has while a field of /feedback's review is edited (2.1.302).</summary>
    public const string ActionCancelEdit = "cancel edit";

    /// <summary>The same as it stands on the hint row, "… · Esc to cancel edit": the cheap test under the cursor.</summary>
    public const string CancelEditHint = "to cancel edit";

    /// <summary>After a feedback report is sent from /feedback: "done: Feedback sent (receipt …). Thanks!" over this row (2.1.302).</summary>
    public const string AnyKeyToContinue = "Any key to continue";

    /// <summary>What a row that waits on Enter ends with ("Login successful. Press Enter to continue…", /login, 2.1.323), with the ellipsis as one character or three full stops.</summary>
    public const string PressEnterToContinue = "Press Enter to continue";

    /// <summary>A row of tab names (<c>Settings  Status   Config   Usage   Stats</c>): three of them at least, two spaces or more apart.</summary>
    [GeneratedRegex(@"^\S+(?: \S+)?(?:\s{2,}\S+(?: \S+)?){2,}$")]
    public static partial Regex TabRow();

    /// <summary>The gap between two tab names, as a pattern to split the row at.</summary>
    public const string TabGapPattern = @"\s{2,}";

    /// <summary>Three spaces: the cheap test for a row that can be a tab row.</summary>
    public const string WideGap = "   ";

    /// <summary>The mark in front of the line a screen highlights ("❯ Send feedback").</summary>
    public const char HighlightMark = '❯';

    /// <summary>What ends a highlighted field's name on /feedback's review ("Details:").</summary>
    public const char FieldNameEnd = ':';

    /// <summary>What the title of /btw's side answer starts with.</summary>
    public const string SideAnswerTitle = "/btw";

    // ---- Feedback drafts: the card, the list and its headings (D33; 2.1.302) ----

    /// <summary>
    /// The last row of the card Claude prints above its prompt when it has drafted feedback for Anthropic:
    /// "1 to review · 2 to send · 0 to dismiss". In screen reader mode those keys do nothing.
    /// </summary>
    [GeneratedRegex(@"^\d to review · \d to send · \d to dismiss$")]
    public static partial Regex FeedbackCardHint();

    /// <summary>On the card's first row, before the draft's title: "Product feedback drafted: &lt;title&gt;".</summary>
    public const string FeedbackCardTitleMarker = " drafted: ";

    /// <summary>The title of /feedback's list of drafts.</summary>
    public const string FeedbackDraftsTitle = "Feedback drafts";

    /// <summary>The headings /feedback puts between its drafts.</summary>
    public const string DraftsOfThisSession = "This session";
    public const string DraftsOfOtherSessions = "Other sessions";

    /// <summary>What follows a draft's title on the list, "Fixture card test, idea · 1m · …".</summary>
    public const string DraftTitleEnd = ",";

    // ---- Feedback surveys above the prompt (2.1.323, the survey fixture) ----

    /// <summary>"1: Bad 2: Fine 3: Good 0: Dismiss", "y: Yes n: No d: Don't ask again": a key, a colon, a label, two answers at least.</summary>
    [GeneratedRegex(@"^(?:[0-9a-z]: \S[^:]*?)(?: [0-9a-z]: \S[^:]*?)+$")]
    public static partial Regex SurveyAnswersRow();

    /// <summary>One answer's key on that row, the group named key.</summary>
    [GeneratedRegex(@"(?:^| )(?<key>[0-9a-z]): ")]
    public static partial Regex SurveyKey();

    /// <summary>The ways out of every survey Claude draws, as its answers row ends.</summary>
    public const string SurveyDismissEnd = " 0: Dismiss";
    public const string SurveyDontAskEnd = " d: Don't ask again";

    /// <summary>The key of Dismiss. The transcript survey has none: its way out is <see cref="No"/>.</summary>
    public const string SurveyDismissKey = "0";

    /// <summary>What a survey's title row starts with: Claude's bullet and a space.</summary>
    public const string SurveyBullet = "● ";

    /// <summary>What the link row between a survey's title and its answers starts with.</summary>
    public const string LinkStart = "https://";

    // ---- What the app sends or looks up as Claude Code's own ----

    /// <summary>The command that lists the feedback drafts, sent when Claude queued one (D33).</summary>
    public const string FeedbackCommand = "/feedback";

    /// <summary>The name of the command that signs in, after which the window says where signing in goes.</summary>
    public const string LoginCommandName = "login";
}
