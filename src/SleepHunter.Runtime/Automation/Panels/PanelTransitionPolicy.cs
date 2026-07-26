namespace SleepHunter.Runtime.Automation.Panels;

public sealed record PanelTransitionPolicy
{
    public static PanelTransitionPolicy Default { get; } = new(
        TimeSpan.FromSeconds(1),
        maximumAttempts: 2);

    public PanelTransitionPolicy(
        TimeSpan attemptTimeout,
        int maximumAttempts)
    {
        if (attemptTimeout <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(
                nameof(attemptTimeout),
                attemptTimeout,
                "Panel transition attempt timeouts must be positive.");
        }

        if (maximumAttempts <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(maximumAttempts),
                maximumAttempts,
                "Panel transitions require at least one attempt.");
        }

        AttemptTimeout = attemptTimeout;
        MaximumAttempts = maximumAttempts;
    }

    public TimeSpan AttemptTimeout { get; }

    public int MaximumAttempts { get; }
}
