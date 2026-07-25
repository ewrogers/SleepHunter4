using SleepHunter.Runtime.Intents;
using SleepHunter.Runtime.Time;

namespace SleepHunter.Runtime.Actions;

public sealed record PendingAction
{
    public PendingAction(
        ClientActionIntent intent,
        MacroTimestamp issuedAt,
        MacroTimestamp deadline,
        int attempt)
    {
        ArgumentNullException.ThrowIfNull(intent);

        if (intent.ActionId.Value <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(intent),
                intent.ActionId,
                "Pending actions require a valid action identifier.");
        }

        if (deadline <= issuedAt)
        {
            throw new ArgumentOutOfRangeException(
                nameof(deadline),
                deadline,
                "Pending action deadlines must be later than issuance.");
        }

        if (attempt <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(attempt),
                attempt,
                "Pending action attempts must be positive.");
        }

        Intent = intent;
        IssuedAt = issuedAt;
        Deadline = deadline;
        Attempt = attempt;
    }

    public ClientActionIntent Intent { get; }

    public MacroTimestamp IssuedAt { get; }

    public MacroTimestamp Deadline { get; }

    public int Attempt { get; }
}
