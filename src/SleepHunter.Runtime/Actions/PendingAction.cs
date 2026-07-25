using SleepHunter.Runtime.Intents;
using SleepHunter.Runtime.Snapshots;
using SleepHunter.Runtime.Time;

namespace SleepHunter.Runtime.Actions;

public sealed record PendingAction
{
    public PendingAction(
        ClientActionIntent intent,
        MacroTimestamp issuedAt,
        MacroTimestamp deadline,
        int attempt,
        int maximumAttempts = 1,
        SnapshotSequence? baselineSnapshotSequence = null)
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

        if (maximumAttempts < attempt)
        {
            throw new ArgumentOutOfRangeException(
                nameof(maximumAttempts),
                maximumAttempts,
                "Maximum attempts cannot be lower than the current attempt.");
        }

        Intent = intent;
        IssuedAt = issuedAt;
        Deadline = deadline;
        Attempt = attempt;
        MaximumAttempts = maximumAttempts;
        BaselineSnapshotSequence = baselineSnapshotSequence;
    }

    public ClientActionIntent Intent { get; }

    public MacroTimestamp IssuedAt { get; }

    public MacroTimestamp Deadline { get; }

    public int Attempt { get; }

    public int MaximumAttempts { get; }

    public SnapshotSequence? BaselineSnapshotSequence { get; }

    public TimeSpan AttemptTimeout => Deadline.Elapsed - IssuedAt.Elapsed;
}
