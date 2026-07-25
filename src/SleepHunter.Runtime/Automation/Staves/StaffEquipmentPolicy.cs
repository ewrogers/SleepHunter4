namespace SleepHunter.Runtime.Automation.Staves;

public sealed record StaffEquipmentPolicy
{
    public static StaffEquipmentPolicy Default { get; } = new(
        TimeSpan.FromSeconds(1),
        maximumAttempts: 2);

    public StaffEquipmentPolicy(
        TimeSpan attemptTimeout,
        int maximumAttempts)
    {
        if (attemptTimeout <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(
                nameof(attemptTimeout),
                attemptTimeout,
                "Staff equipment attempt timeouts must be positive.");
        }

        if (maximumAttempts <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(maximumAttempts),
                maximumAttempts,
                "Staff equipment transitions require at least one attempt.");
        }

        AttemptTimeout = attemptTimeout;
        MaximumAttempts = maximumAttempts;
    }

    public TimeSpan AttemptTimeout { get; }

    public int MaximumAttempts { get; }
}
