using System.Text.Json.Nodes;
using AxKit.Core;
using AxKit.Core.Mcp;
using AxKit.Forms;

namespace AxClaude;

/// <summary>
/// axClaude's part of the AI tools (docs/axKit/mcp.md MC-9, docs/axClaude/SPEC.md FR-2.12): a command Claude offers
/// the user to run themselves (<c>axkit_offer_command</c>). The offer waits until Claude's turn has ended and the
/// window shows nothing else; then a notice reads the command whole, with Do not run as its default; only the user's
/// Run it sends the command, as a <c>!</c> line, the way a message of the user's own goes. Asked every time: there is
/// no switch that lets a command through unasked.
/// </summary>
internal sealed partial class MainForm
{
    private sealed record Offer(string Client, string Command, string? Reason);

    /// <summary>The offers that wait for the user, the oldest first; <see cref="McpOffer.MaxWaiting"/> at most.</summary>
    private readonly Queue<Offer> _offers = new();

    /// <summary>Counts the settles started, so that only the newest one shows the question.</summary>
    private int _offerSettle;

    /// <summary>How long Claude must have been idle before an offer is shown: its turn has ended, not paused between two steps.</summary>
    private const int OfferSettleMs = 1500;

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

        if (_offers.Count >= McpOffer.MaxWaiting)
        {
            return new { error = $"{McpOffer.MaxWaiting} offered commands already wait for the user: end your turn so that they can be answered." };
        }

        _offers.Enqueue(new Offer(client, command!.Trim(), McpProtocol.Text(arguments["reason"])));
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
        if (_offers.Count == 0 || !OfferMayShow)
        {
            return;
        }

        var settle = ++_offerSettle;
        After(OfferSettleMs, () =>
        {
            if (settle == _offerSettle && !IsDisposed && _offers.Count > 0 && OfferMayShow)
            {
                AskOffer(_offers.Dequeue());
            }
        });
    }

    /// <summary>An offer that waited behind a notice gets its turn when the notice closes.</summary>
    protected override void OnNoticeClosed()
    {
        base.OnNoticeClosed();
        ShowOfferWhenIdle();
    }

    /// <summary>Claude's turn has ended, it asks nothing, no notice shows and nothing is in the control area.</summary>
    private bool OfferMayShow => _readyAnnounced && ClaudeIdle && !NoticeOpen && Area.Current is null;

    private void AskOffer(Offer offer)
    {
        ShowNotice(new Notice(McpOffer.AskTitle(offer.Client), McpOffer.AskText(offer.Client, offer.Command, offer.Reason),
        [
            new OverlayChoice("&Run it", () =>
            {
                // Claude may have started on something since the question showed (a queued message taken up).
                if (!ClaudeIdle)
                {
                    Announce("Not run: Claude is busy. Type it yourself after an exclamation mark when Claude is done", true);
                }
                else
                {
                    SendOffered(offer.Command);
                }

                ShowOfferWhenIdle();
            }),
            new OverlayChoice("&Do not run", () =>
            {
                Announce("Not run", true);
                ShowOfferWhenIdle();
            }, IsDefault: true, IsCancel: true),
        ])
        {
            Unprompted = true,
        });
    }

    /// <summary>The command sent as the user's own ! line would go: its block in the conversation, the text and Enter as separate writes.</summary>
    private void SendOffered(string command)
    {
        var line = McpOffer.Line(command);
        _lastCommand = null;
        _model.Send(line, evenWhilePrompting: true);
        _messageSent = true;
        QueueWrites(line);
        ResendEnterWhileHeld(line, attempt: 1);
        Log.Info("A command an AI tool offered was sent as a ! line at the user's Run");
        Announce("Sent", false);
        if (_settings.SoundOnBell)
        {
            Sounds.Sent();
        }
    }
}
