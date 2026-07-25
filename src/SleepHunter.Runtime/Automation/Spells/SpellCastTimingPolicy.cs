namespace SleepHunter.Runtime.Automation.Spells;

public sealed record SpellCastTimingPolicy
{
    public static SpellCastTimingPolicy Default { get; } = new(
        TimeSpan.FromMilliseconds(200),
        TimeSpan.FromSeconds(1),
        TimeSpan.FromSeconds(1),
        TimeSpan.FromMilliseconds(100));

    public SpellCastTimingPolicy(
        TimeSpan zeroLineDuration,
        TimeSpan singleLineDuration,
        TimeSpan multiLineDurationPerLine,
        TimeSpan completionPadding)
    {
        if (zeroLineDuration < TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(
                nameof(zeroLineDuration),
                zeroLineDuration,
                "Zero-line cast duration cannot be negative.");
        }

        if (singleLineDuration < TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(
                nameof(singleLineDuration),
                singleLineDuration,
                "Single-line cast duration cannot be negative.");
        }

        if (multiLineDurationPerLine < TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(
                nameof(multiLineDurationPerLine),
                multiLineDurationPerLine,
                "Multi-line cast duration cannot be negative.");
        }

        if (completionPadding <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(
                nameof(completionPadding),
                completionPadding,
                "Spell cast completion padding must be positive.");
        }

        ZeroLineDuration = zeroLineDuration;
        SingleLineDuration = singleLineDuration;
        MultiLineDurationPerLine = multiLineDurationPerLine;
        CompletionPadding = completionPadding;
    }

    public TimeSpan ZeroLineDuration { get; }

    public TimeSpan SingleLineDuration { get; }

    public TimeSpan MultiLineDurationPerLine { get; }

    public TimeSpan CompletionPadding { get; }

    public TimeSpan CalculateDuration(int castLines)
    {
        if (castLines < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(castLines),
                castLines,
                "Spell cast lines cannot be negative.");
        }

        var lineDuration = castLines switch
        {
            0 => ZeroLineDuration,
            1 => SingleLineDuration,
            _ => TimeSpan.FromTicks(
                checked(MultiLineDurationPerLine.Ticks * castLines))
        };

        return lineDuration + CompletionPadding;
    }
}
