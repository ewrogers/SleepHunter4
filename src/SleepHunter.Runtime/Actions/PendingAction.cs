using SleepHunter.Runtime.Intents;
using SleepHunter.Runtime.Snapshots;
using SleepHunter.Runtime.Time;

namespace SleepHunter.Runtime.Actions;

public sealed record PendingAction
{
    public PendingAction(
        ClientActionIntent intent,
        MacroTimestamp requestedAt,
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

        if (deadline <= requestedAt)
        {
            throw new ArgumentOutOfRangeException(
                nameof(deadline),
                deadline,
                "Pending action deadlines must be later than the request.");
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
        RequestedAt = requestedAt;
        Deadline = deadline;
        Attempt = attempt;
        MaximumAttempts = maximumAttempts;
        BaselineSnapshotSequence = baselineSnapshotSequence;
    }

    public ClientActionIntent Intent { get; }

    public MacroTimestamp RequestedAt { get; }

    public MacroTimestamp Deadline { get; }

    public int Attempt { get; }

    public int MaximumAttempts { get; }

    public SnapshotSequence? BaselineSnapshotSequence { get; }

    public MacroTimestamp? IssuedAt { get; private init; }

    public bool IsIssued => IssuedAt.HasValue;

    public TimeSpan AttemptTimeout =>
        Deadline.Elapsed - RequestedAt.Elapsed;

    internal PendingAction MarkIssued(MacroTimestamp issuedAt)
    {
        if (issuedAt < RequestedAt)
        {
            throw new ArgumentOutOfRangeException(
                nameof(issuedAt),
                issuedAt,
                "An action cannot be issued before it was requested.");
        }

        return this with { IssuedAt = issuedAt };
    }
}
