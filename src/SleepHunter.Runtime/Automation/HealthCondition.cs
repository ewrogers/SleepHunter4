using SleepHunter.Runtime.Snapshots;

namespace SleepHunter.Runtime.Automation;

public sealed record HealthCondition
{
    public static HealthCondition Any { get; } = new();

    public HealthCondition(
        double? minimumPercentExclusive = null,
        double? maximumPercentInclusive = null)
    {
        ValidatePercent(
            minimumPercentExclusive,
            nameof(minimumPercentExclusive));
        ValidatePercent(
            maximumPercentInclusive,
            nameof(maximumPercentInclusive));

        if (minimumPercentExclusive is { } minimum &&
            maximumPercentInclusive is { } maximum &&
            minimum >= maximum)
        {
            throw new ArgumentException(
                "The minimum health percentage must be below the maximum.");
        }

        MinimumPercentExclusive = minimumPercentExclusive;
        MaximumPercentInclusive = maximumPercentInclusive;
    }

    public double? MinimumPercentExclusive { get; }

    public double? MaximumPercentInclusive { get; }

    public bool IsRestricted =>
        MinimumPercentExclusive is not null ||
        MaximumPercentInclusive is not null;

    public bool IsSatisfiedBy(VitalsSnapshot vitals)
    {
        ArgumentNullException.ThrowIfNull(vitals);

        if (!IsRestricted)
        {
            return true;
        }

        if (vitals.MaximumHealth <= 0)
        {
            return false;
        }

        var healthPercent = vitals.HealthPercent;
        return (MinimumPercentExclusive is not { } minimum ||
                healthPercent > minimum) &&
               (MaximumPercentInclusive is not { } maximum ||
                healthPercent <= maximum);
    }

    private static void ValidatePercent(double? value, string parameterName)
    {
        if (value is { } percent &&
            (!double.IsFinite(percent) || percent is < 0 or > 100))
        {
            throw new ArgumentOutOfRangeException(
                parameterName,
                value,
                "Health percentages must be finite values from 0 through 100.");
        }
    }
}
