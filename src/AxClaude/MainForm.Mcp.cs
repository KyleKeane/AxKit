using System.Runtime.InteropServices;
using System.Text.Json.Nodes;
using AxClaude.Core.Transcript;
using AxKit.Core;
using AxKit.Core.Events;
using AxKit.Core.Mcp;
using AxKit.Forms;

namespace AxClaude;

/// <summary>
/// axClaude's part of the AI tools (docs/axKit/mcp.md MC-9, docs/axClaude/SPEC.md FR-2.12): a command Claude offers
/// the user to run themselves (<c>axkit_offer_command</c>). The offer waits until Claude's turn has ended and the
/// window shows nothing else; then it is a question in the control area, as Claude's own questions are, so that the
/// conversation stays there to read: the list is named with the command, and starts on Explain this command, which
/// only opens a text; Yes, run it sends the command as a <c>!</c> line, the way a message of the user's own goes.
/// Asked every time: there is no switch that lets a command through unasked.
/// </summary>
internal sealed partial class MainForm
{
    /// <summary>A shell command offered, or, with <paramref name="Request"/>, an AI tool's request to run a command of this window (MC-7.3), which waits in the same line and is asked in the same list.</summary>
    private sealed record Offer(string Client, string Command, string? Reason, string? Details, OfferedRequest? Request = null);

    /// <summary>
    /// An AI tool's request to run a command of this window joins the offers (MC-7.3): asked in the control area as they
    /// are, one after another, and never over a question of Claude's. It does not wait for Claude's turn to end, since
    /// nothing is sent to Claude Code. False, and a notice asks instead, when three already wait.
    /// </summary>
    protected override bool ShowOfferedRequest(OfferedRequest request)
    {
        if (_offers.Count + (_offerShown is null ? 0 : 1) >= McpOffer.MaxWaiting)
        {
            return false;
        }

        _offers.Add(new Offer(string.Empty, string.Empty, null, null, request));
        ShowOfferWhenIdle();
        return true;
    }

    /// <summary>The offers that wait for the user, the oldest first; <see cref="McpOffer.MaxWaiting"/> at most.</summary>
    private readonly List<Offer> _offers = [];

    /// <summary>The offer the control area shows, or null.</summary>
    private Offer? _offerShown;

    /// <summary>Counts the settles started, so that only the newest one shows the question.</summary>
    private int _offerSettle;

    /// <summary>How long Claude must have been idle before an offer is shown: its turn has ended, not paused between two steps.</summary>
    private const int OfferSettleMs = 1500;

    /// <summary>The signature of an offer's question in the control area's record (<see cref="_wait"/>), which no question of Claude's has.</summary>
    private const string OfferSignature = "axkit: an offered command";

    protected override object? McpAppRequest(string tool, JsonObject arguments, string client)
    {
        if (tool != McpTools.Offer)
        {
            return null;
        }

        var command = McpProtocol.Text(arguments["command"]);
        if (McpOffer.Refusal(command) is { } refusal)
        {
            return new { error = refusal };
        }

        if (!ProcessRunning)
        {
            return new { error = "Claude Code is not running in this axClaude window: nothing to send the command to." };
        }

        if (Streams)
        {
            // stream-mode.md SM-5.6 (measured 2026-11-25): over the stream a ! line runs nothing and goes to the model as
            // a message, so nothing is offered that could not be sent; the tool is told, and can tell the user what to
            // run and where.
            return new { error = "This axClaude window runs Claude Code over its stream, where a command cannot be sent for the user to run. Tell the user the command and that they can run it themselves, for example in axTerminal." };
        }

        if (_offers.Count + (_offerShown is null ? 0 : 1) >= McpOffer.MaxWaiting)
        {
            return new { error = $"{McpOffer.MaxWaiting} offered commands already wait for the user: end your turn so that they can be answered." };
        }

        _offers.Add(new Offer(client, command!.Trim(), McpProtocol.Text(arguments["reason"]), McpProtocol.Text(arguments["details"])));
        ShowOfferWhenIdle();
        return new { offered = true, next = McpOffer.Waiting };
    }

    /// <summary>
    /// Starts the settle for the oldest offer when Claude is idle and the window shows nothing else; called when an
    /// offer arrives, when the transcript changes and when a notice of the window's closes. The question itself
    /// shows only if all of that still holds after <see cref="OfferSettleMs"/>.
    /// </summary>
    private void ShowOfferWhenIdle()
    {
        if (_offers.Count == 0 || !OfferMayShow(_offers[0]))
        {
            return;
        }

        var settle = ++_offerSettle;
        After(OfferSettleMs, () =>
        {
            if (settle == _offerSettle && _offers.Count > 0 && OfferMayShow(_offers[0]))
            {
                var offer = _offers[0];
                _offers.RemoveAt(0);
                AskOffer(offer, McpOffer.Explain, unprompted: true);
            }
        });
    }

    /// <summary>An offer that waited behind a notice gets its turn when the notice closes.</summary>
    protected override void OnNoticeClosed()
    {
        base.OnNoticeClosed();
        ShowOfferWhenIdle();
    }

    /// <summary>Claude's turn has ended (a shell command is sent to its prompt; a window command's request does not wait for that), it asks nothing, no notice shows and nothing is in the control area; and the window is on view: put away (FR-9.6), an offer waits as a question of Claude's does, and is asked, with its caution heard, when the window is brought up.</summary>
    private bool OfferMayShow(Offer offer) =>
        !_putAway && _readyAnnounced && (offer.Request is not null ? WaitSignature is null : ClaudeIdle) && !NoticeOpen && Area.Current is null && _offerShown is null;

    /// <summary>
    /// The question in the control area (MC-9.4): the list named with the command, starting on
    /// <paramref name="start"/>. Enter on Explain opens the text and comes back to the same list; Escape is No.
    /// </summary>
    private void AskOffer(Offer offer, int start, bool unprompted)
    {
        _offerShown = offer;
        var name = offer.Request?.Name ?? McpOffer.AskName(offer.Client, offer.Command);
        var text = offer.Request?.Text ?? McpOffer.AskText(offer.Client, offer.Command, offer.Reason);
        // The offer's sound, raised by its kind: a command Claude offers for the user to run (FR-2.12), or an AI
        // tool's request to run a command of this window, which is shown in the same list (MC-7.3).
        Action chime = offer.Request is null
            ? () => EventDoor.Raise(EventKind.CommandOffer)
            : () => EventDoor.Raise(EventKind.AiAsksToRun);
        // Held for a moment also when it comes back from the field of a concern, as every control the area shows is,
        // and bound to this offer (AreaList): an answer goes to the offer the list was shown for, while it is shown.
        ShowInArea(OfferSignature, text, unprompted, focus =>
            AreaList(name, offer.Request?.Answers ?? McpOffer.Answers, start, null, focus, () => OfferStillShown(offer), (chosen, _) => AnswerOffer(offer, chosen),
                () => AnswerOffer(offer, offer.Request?.No ?? McpOffer.No)), chime);
        // The gate takes the list for a notice, as it takes every window's list of an AI's request (docs/axKit/mcp.md
        // MC-7.3): no AI tool's run goes on in the window under it. Until 2026-11-27 this window's own list was not
        // reported, and runs went on.
        OfferedRequestShownIn(Area);
        if (unprompted)
        {
            // An offer has no signal of Claude's: the taskbar button flashes for it when the window is behind.
            FlashIfBehind();
        }
    }

    /// <summary>Whether <paramref name="offer"/> is still the one the control area asks about; when not (the run ended, Claude's own question took its place), nothing of it is answered and the window says so.</summary>
    private bool OfferStillShown(Offer offer)
    {
        if (ReferenceEquals(_offerShown, offer))
        {
            return true;
        }

        Refuse(NoLongerAskedWords);
        return false;
    }

    private void AnswerOffer(Offer offer, int answer)
    {
        if (offer.Request is { } request)
        {
            if (answer == request.Explain)
            {
                ShowNotice(Notice.Plain(request.ExplainTitle, request.ExplainText).ByUser());
                return;
            }

            // The base runs it or says why not; the list has gone and the message field is back first.
            CloseOffer();
            request.Answer(answer);
            ShowOfferWhenIdle();
            return;
        }

        if (answer == McpOffer.Explain)
        {
            // A text with one button; the list is still there under it, and the focus comes back to it (the notice rule).
            ShowNotice(Notice.Plain(McpOffer.ExplainTitle, McpOffer.ExplainText(offer.Client, offer.Command, offer.Reason, offer.Details)).ByUser());
            return;
        }

        if (answer == McpOffer.Concern)
        {
            AskConcern(offer);
            return;
        }

        CloseOffer();
        if (answer == McpOffer.Run)
        {
            // Claude may have started on something since the question showed.
            if (!ClaudeIdle)
            {
                Refuse("Not run: Claude is busy. Type it yourself after an exclamation mark when Claude is done");
            }
            else
            {
                SendOffered(offer);
            }
        }
        else if (answer == McpOffer.Copy)
        {
            try
            {
                Clipboard.SetText(offer.Command);
                Announce("Copied, not run", true);
            }
            catch (ExternalException)
            {
                // Another program holds the clipboard.
                Refuse("Not copied: the clipboard is in use. Not run");
            }
        }
        else
        {
            Announce("Not run", true);
        }

        ShowOfferWhenIdle();
    }

    /// <summary>
    /// No, and report a concern (MC-9.8): a field for the user's words in the list's place; Enter sends nothing of
    /// the command and opens Claude Code's own feedback with the command, the reason and the words, which Claude Code
    /// shows for review before it sends anything; Escape goes back to the list, on the same answer.
    /// </summary>
    private void AskConcern(Offer offer)
    {
        UserActedNow();
        ShowInArea(OfferSignature + ", the concern", McpOffer.ConcernName, unprompted: false, focus =>
            AreaField(McpOffer.ConcernName, focus, () => OfferStillShown(offer), typed =>
            {
                CloseOffer();
                if (!ClaudeIdle)
                {
                    Refuse("Not run, and not reported: Claude is busy. Type /feedback yourself when Claude is done");
                    return;
                }

                var line = McpOffer.ConcernLine(offer.Client, offer.Command, offer.Reason, typed);
                _lastCommand = SlashCommands.Find(ClaudeCodeTexts.FeedbackCommand);
                _model.Send(line, evenWhilePrompting: true);
                _messageSent = true;
                // Written out under the AI caution's words when it is given back (K5, M4): the command in it is the AI's.
                QueueWrites(line, shown: $"{AiCaution.LineNotSent(McpRun.Speaker(offer.Client))}: {McpOffer.Shown(line)}");
                Announce("Not run. Claude Code's feedback opens with your concern", true);
            }, () => AskOffer(offer, McpOffer.Concern, unprompted: false)));
        // The offer's question still waits, in this field: the gate holds as under its list.
        OfferedRequestShownIn(Area);
    }

    /// <summary>The offer's question leaves the control area and the message field comes back.</summary>
    private void CloseOffer()
    {
        _offerShown = null;
        UserActedNow();
        CloseArea();
        if (_wait is { } wait && wait.Signature.StartsWith(OfferSignature, StringComparison.Ordinal))
        {
            _wait = null;
        }
    }

    /// <summary>
    /// The run of Claude Code has ended (a stop, a restart, New session, another folder: every end passes
    /// <see cref="StopProcess"/>): the offer shown and those that wait go with it, and one line says so when there were
    /// any. Until 2026-11-27 they stayed: the offer shown, its list closed with the area, kept every later offer from
    /// being asked, and one that waited was asked in the next run and sent as a ! line in the folder of that run.
    /// Nothing waits on them: the tool was answered when it offered.
    /// </summary>
    private void ForgetOffers()
    {
        var dropped = _offers.Count + (_offerShown is null ? 0 : 1);
        _offers.Clear();
        _offerShown = null;
        _offerSettle++;
        if (dropped > 0)
        {
            Log.Info($"{dropped} offered commands or requests of an AI tool were dropped unanswered: the run of Claude Code ended");
            _model.AddSystemLine(dropped == 1
                ? "A command an AI tool offered was not answered when Claude stopped, and is dropped. Nothing of it ran."
                : $"{dropped} commands an AI tool offered were not answered when Claude stopped, and are dropped. Nothing of them ran.");
        }
    }

    /// <summary>
    /// Claude asks something of its own while an offer shows (a message taken up, a survey): Claude's question takes
    /// the control area, and the offer waits again, first in line, until Claude is idle.
    /// </summary>
    private void OfferGivesWay()
    {
        if (_offerShown is { } offer)
        {
            _offerShown = null;
            _offers.Insert(0, offer);
        }
    }

    /// <summary>
    /// The command sent as the user's own ! line would go: its block in the conversation, the text and Enter as
    /// separate writes. A message an interrupt put back on Claude Code's prompt row is emptied first, and the flag of
    /// the interrupt taken down, where every message passes (<see cref="QueueWrites"/>, since 2026-11-27): until then
    /// the line was typed after the restored message, and its Enter, when Claude Code dropped it, was never sent again.
    /// </summary>
    private void SendOffered(Offer offer)
    {
        var line = McpOffer.Line(offer.Command);
        _lastCommand = null;
        _model.Send(line, evenWhilePrompting: true);
        _messageSent = true;
        Log.Info("A command an AI tool offered was sent as a ! line at the user's Yes");
        // "Sent" when its Enter goes (QueueWrites), which also looks again for an Enter Claude Code dropped. Given back,
        // the line is written out as the question showed it, under the AI caution's words (the seventh review of
        // 2026-11-27, K5; all of rule 11 since the eighth, M4): never raw, and never in axKit's voice.
        QueueWrites(line, sent: () =>
        {
            Announce("Sent", false);
            if (_settings.SoundOnBell)
            {
                EventDoor.Raise(EventKind.Sent);
            }
        }, shown: $"{AiCaution.LineNotSent(McpRun.Speaker(offer.Client))}: {McpOffer.Shown(line)}");
    }
}
