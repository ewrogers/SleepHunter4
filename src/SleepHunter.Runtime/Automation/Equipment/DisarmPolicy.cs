namespace SleepHunter.Runtime.Automation.Equipment;

public sealed record DisarmPolicy
{
    public static DisarmPolicy Default { get; } = new(
        TimeSpan.FromSeconds(1),
        maximumAttempts: 2);

    public DisarmPolicy(TimeSpan attemptTimeout, int maximumAttempts)
    {
        if (attemptTimeout <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(
                nameof(attemptTimeout),
                attemptTimeout,
                "Disarm attempt timeouts must be positive.");
        }

        if (maximumAttempts <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(maximumAttempts),
                maximumAttempts,
                "Disarm attempt counts must be positive.");
        }

        AttemptTimeout = attemptTimeout;
        MaximumAttempts = maximumAttempts;
    }

    public TimeSpan AttemptTimeout { get; }

    public int MaximumAttempts { get; }
}
