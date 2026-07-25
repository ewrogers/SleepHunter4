using System.Collections.Immutable;
using SleepHunter.Runtime.Automation;

namespace SleepHunter.Interop.Snapshots;

public sealed class AbilitySnapshotCatalog
{
    private readonly ImmutableDictionary<string, SkillSnapshotMetadata> skills;
    private readonly ImmutableDictionary<string, SpellSnapshotMetadata> spells;

    public AbilitySnapshotCatalog(
        IEnumerable<SkillSnapshotMetadata> skills,
        IEnumerable<SpellSnapshotMetadata> spells)
    {
        ArgumentNullException.ThrowIfNull(skills);
        ArgumentNullException.ThrowIfNull(spells);

        this.skills = BuildCatalog(skills, static metadata => metadata.Name);
        this.spells = BuildCatalog(spells, static metadata => metadata.Name);
    }

    public static AbilitySnapshotCatalog Empty { get; } = new([], []);

    public int SkillCount => skills.Count;

    public int SpellCount => spells.Count;

    public SkillSnapshotMetadata? FindSkill(string name) =>
        skills.GetValueOrDefault(name);

    public SpellSnapshotMetadata? FindSpell(string name) =>
        spells.GetValueOrDefault(name);

    private static ImmutableDictionary<string, TMetadata> BuildCatalog<TMetadata>(
        IEnumerable<TMetadata> entries,
        Func<TMetadata, string> nameSelector)
        where TMetadata : class
    {
        var catalog = ImmutableDictionary.CreateBuilder<string, TMetadata>(
            StringComparer.OrdinalIgnoreCase);
        foreach (var entry in entries)
        {
            ArgumentNullException.ThrowIfNull(entry);

            var name = nameSelector(entry);
            if (!catalog.TryAdd(name, entry))
            {
                throw new ArgumentException(
                    $"Ability metadata names must be unique. Duplicate name: '{name}'.",
                    nameof(entries));
            }
        }

        return catalog.ToImmutable();
    }
}

public sealed record SkillSnapshotMetadata
{
    public SkillSnapshotMetadata(
        string name,
        int manaCost,
        TimeSpan cooldown,
        bool isAssail = false,
        bool opensDialog = false,
        bool requiresDisarm = false,
        HealthCondition? healthCondition = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

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
        ManaCost = manaCost;
        Cooldown = cooldown;
        IsAssail = isAssail;
        OpensDialog = opensDialog;
        RequiresDisarm = requiresDisarm;
        HealthCondition = healthCondition ?? HealthCondition.Any;
    }

    public string Name { get; }

    public int ManaCost { get; }

    public TimeSpan Cooldown { get; }

    public bool IsAssail { get; }

    public bool OpensDialog { get; }

    public bool RequiresDisarm { get; }

    public HealthCondition HealthCondition { get; }
}

public sealed record SpellSnapshotMetadata
{
    public SpellSnapshotMetadata(
        string name,
        int castLines,
        int manaCost,
        TimeSpan cooldown)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

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
        CastLines = castLines;
        ManaCost = manaCost;
        Cooldown = cooldown;
    }

    public string Name { get; }

    public int CastLines { get; }

    public int ManaCost { get; }

    public TimeSpan Cooldown { get; }
}
