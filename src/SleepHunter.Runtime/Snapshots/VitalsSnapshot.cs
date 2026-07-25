namespace SleepHunter.Runtime.Snapshots;

public sealed record VitalsSnapshot
{
    public VitalsSnapshot(
        int currentHealth,
        int maximumHealth,
        int currentMana,
        int maximumMana)
    {
        if (currentHealth < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(currentHealth),
                currentHealth,
                "Current health cannot be negative.");
        }

        if (maximumHealth < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(maximumHealth),
                maximumHealth,
                "Maximum health cannot be negative.");
        }

        if (currentMana < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(currentMana),
                currentMana,
                "Current mana cannot be negative.");
        }

        if (maximumMana < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(maximumMana),
                maximumMana,
                "Maximum mana cannot be negative.");
        }

        CurrentHealth = currentHealth;
        MaximumHealth = maximumHealth;
        CurrentMana = currentMana;
        MaximumMana = maximumMana;
    }

    public int CurrentHealth { get; }

    public int MaximumHealth { get; }

    public int CurrentMana { get; }

    public int MaximumMana { get; }

    public double HealthPercent =>
        CalculatePercent(CurrentHealth, MaximumHealth);

    public double ManaPercent =>
        CalculatePercent(CurrentMana, MaximumMana);

    private static double CalculatePercent(int current, int maximum) =>
        maximum <= 0
            ? 0
            : Math.Min(100, current * 100.0 / maximum);
}
