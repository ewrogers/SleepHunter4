using SleepHunter.Runtime.Automation;
using SleepHunter.Runtime.Automation.Panels;

namespace SleepHunter.Runtime.Snapshots;

public sealed record SkillSnapshot
{
    public const int MaximumSlot = 90;

    public SkillSnapshot(
        string name,
        int slot,
        int currentLevel,
        int maximumLevel,
        int manaCost,
        TimeSpan cooldown,
        bool isAssail = false,
        bool opensDialog = false,
        bool requiresDisarm = false,
        HealthCondition? healthCondition = null,
        bool isActionDelayed = false,
        ushort icon = 0,
        uint cooldownProgress = 0,
        uint cooldownStartedAt = 0,
        uint cooldownEndsAt = 0,
        bool isCooldownVisualActive = false)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        if (slot is <= 0 or > MaximumSlot)
        {
            throw new ArgumentOutOfRangeException(
                nameof(slot),
                slot,
                "Skill slots must be within the supported skillbook range.");
        }

        if (currentLevel < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(currentLevel),
                currentLevel,
                "Current skill level cannot be negative.");
        }

        if (maximumLevel < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(maximumLevel),
                maximumLevel,
                "Maximum skill level cannot be negative.");
        }

        if (manaCost < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(manaCost),
                manaCost,
                "Skill mana cost cannot be negative.");
        }

        if (cooldown < TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(
                nameof(cooldown),
                cooldown,
                "Skill cooldown cannot be negative.");
        }

        Name = name.Trim();
        Slot = slot;
        CurrentLevel = currentLevel;
        MaximumLevel = maximumLevel;
        ManaCost = manaCost;
        Cooldown = cooldown;
        IsAssail = isAssail;
        OpensDialog = opensDialog;
        RequiresDisarm = requiresDisarm;
        HealthCondition =
            healthCondition ?? SleepHunter.Runtime.Automation.HealthCondition.Any;
        IsActionDelayed = isActionDelayed;
        Icon = icon;
        CooldownProgress = cooldownProgress;
        CooldownStartedAt = cooldownStartedAt;
        CooldownEndsAt = cooldownEndsAt;
        IsCooldownVisualActive = isCooldownVisualActive;
    }

    public string Name { get; }

    public int Slot { get; }

    public ClientPanel Panel => GetPanelForSlot(Slot);

    public int CurrentLevel { get; }

    public int MaximumLevel { get; }

    public int ManaCost { get; }

    public TimeSpan Cooldown { get; }

    public bool IsAssail { get; }

    public bool OpensDialog { get; }

    public bool RequiresDisarm { get; }

    public HealthCondition HealthCondition { get; }

    public bool IsActionDelayed { get; }

    public ushort Icon { get; }

    public uint CooldownProgress { get; }

    public uint CooldownStartedAt { get; }

    public uint CooldownEndsAt { get; }

    public bool IsCooldownVisualActive { get; }

    public static ClientPanel GetPanelForSlot(int slot)
    {
        if (slot is <= 0 or > MaximumSlot)
        {
            throw new ArgumentOutOfRangeException(
                nameof(slot),
                slot,
                "Skill slots must be within the supported skillbook range.");
        }

        return slot switch
        {
            <= 36 => ClientPanel.TemuairSkills,
            <= 72 => ClientPanel.MedeniaSkills,
            _ => ClientPanel.WorldSkills
        };
    }
}
