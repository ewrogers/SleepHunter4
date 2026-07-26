using SleepHunter.Runtime.Automation.Panels;

namespace SleepHunter.Runtime.Snapshots;

public sealed record SpellSnapshot
{
    public const int MaximumSlot = 90;

    public SpellSnapshot(
        string name,
        int slot,
        int currentLevel,
        int maximumLevel,
        int castLines,
        int manaCost,
        TimeSpan cooldown,
        bool isActionDelayed = false,
        bool opensDialog = false,
        ushort icon = 0,
        byte argumentType = 0,
        string? prompt = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        if (slot is <= 0 or > MaximumSlot)
        {
            throw new ArgumentOutOfRangeException(
                nameof(slot),
                slot,
                "Spell slots must be within the supported spellbook range.");
        }

        if (currentLevel < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(currentLevel),
                currentLevel,
                "Current spell level cannot be negative.");
        }

        if (maximumLevel < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(maximumLevel),
                maximumLevel,
                "Maximum spell level cannot be negative.");
        }

        if (castLines < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(castLines),
                castLines,
                "Spell cast lines cannot be negative.");
        }

        if (manaCost < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(manaCost),
                manaCost,
                "Spell mana cost cannot be negative.");
        }

        if (cooldown < TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(
                nameof(cooldown),
                cooldown,
                "Spell cooldown cannot be negative.");
        }

        Name = name.Trim();
        Slot = slot;
        CurrentLevel = currentLevel;
        MaximumLevel = maximumLevel;
        CastLines = castLines;
        ManaCost = manaCost;
        Cooldown = cooldown;
        IsActionDelayed = isActionDelayed;
        OpensDialog = opensDialog;
        Icon = icon;
        ArgumentType = argumentType;
        Prompt = string.IsNullOrWhiteSpace(prompt)
            ? null
            : prompt.Trim();
    }

    public string Name { get; }

    public int Slot { get; }

    public ClientPanel Panel => GetPanelForSlot(Slot);

    public static ClientPanel GetPanelForSlot(int slot)
    {
        if (slot is <= 0 or > MaximumSlot)
        {
            throw new ArgumentOutOfRangeException(
                nameof(slot),
                slot,
                "Spell slots must be within the supported spellbook range.");
        }

        return slot switch
        {
            <= 36 => ClientPanel.TemuairSpells,
            <= 72 => ClientPanel.MedeniaSpells,
            _ => ClientPanel.WorldSpells
        };
    }

    public int CurrentLevel { get; }

    public int MaximumLevel { get; }

    public int CastLines { get; }

    public int ManaCost { get; }

    public TimeSpan Cooldown { get; }

    public bool IsActionDelayed { get; }

    public bool OpensDialog { get; }

    public ushort Icon { get; }

    public byte ArgumentType { get; }

    public string? Prompt { get; }
}
