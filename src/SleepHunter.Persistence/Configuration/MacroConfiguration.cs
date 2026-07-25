using System.Collections.Immutable;
using SleepHunter.Runtime.Automation.Flowering;
using SleepHunter.Runtime.Automation.Skills;
using SleepHunter.Runtime.Automation.Spells;

namespace SleepHunter.Persistence.Configuration;

public sealed record MacroConfiguration
{
    public static MacroConfiguration Empty { get; } = new();

    public MacroConfiguration(
        string? name = null,
        string? description = null,
        HotkeyConfiguration? hotkey = null,
        SpellQueueRotation? spellRotation = null,
        ImmutableArray<SkillQueueEntry> skills = default,
        ImmutableArray<SpellQueueEntry> spells = default,
        ImmutableArray<FlowerQueueEntry> flowers = default,
        FlowerOptions? flowerOptions = null)
    {
        Skills = skills.IsDefault
            ? ImmutableArray<SkillQueueEntry>.Empty
            : skills;
        Spells = spells.IsDefault
            ? ImmutableArray<SpellQueueEntry>.Empty
            : spells;
        Flowers = flowers.IsDefault
            ? ImmutableArray<FlowerQueueEntry>.Empty
            : flowers;

        ValidateEntries(Skills, nameof(skills));
        ValidateEntries(Spells, nameof(spells));
        ValidateEntries(Flowers, nameof(flowers));
        ValidateUniqueIds(Skills.Select(entry => entry.Id.Value), nameof(skills));
        ValidateUniqueNames(Skills.Select(entry => entry.Name), nameof(skills));
        ValidateUniqueIds(Spells.Select(entry => entry.Id.Value), nameof(spells));
        ValidateUniqueIds(
            Flowers.Select(entry => entry.Id.Value),
            nameof(flowers));
        if (spellRotation is { } rotation &&
            !Enum.IsDefined(rotation))
        {
            throw new ArgumentOutOfRangeException(
                nameof(spellRotation),
                spellRotation,
                "The spell queue rotation is not supported.");
        }

        Name = NormalizeOptional(name);
        Description = NormalizeOptional(description);
        Hotkey = hotkey;
        SpellRotation = spellRotation;
        FlowerOptions = flowerOptions ?? FlowerOptions.Default;
    }

    public string? Name { get; }

    public string? Description { get; }

    public HotkeyConfiguration? Hotkey { get; }

    public SpellQueueRotation? SpellRotation { get; }

    public ImmutableArray<SkillQueueEntry> Skills { get; }

    public ImmutableArray<SpellQueueEntry> Spells { get; }

    public ImmutableArray<FlowerQueueEntry> Flowers { get; }

    public FlowerOptions FlowerOptions { get; }

    public SpellQueueState CreateSpellQueue(
        SpellQueueRotation fallbackRotation = SpellQueueRotation.Priority)
    {
        var queue = SpellQueueState.Empty.SetRotation(
            SpellRotation ?? fallbackRotation);
        foreach (var entry in Spells)
        {
            queue = queue.Add(entry);
        }

        return queue;
    }

    public SkillQueueState CreateSkillQueue()
    {
        var queue = SkillQueueState.Empty;
        foreach (var entry in Skills)
        {
            queue = queue.Add(entry);
        }

        return queue;
    }

    public FlowerQueueState CreateFlowerQueue()
    {
        var queue = FlowerQueueState.Empty;
        foreach (var entry in Flowers)
        {
            queue = queue.Add(entry);
        }

        return queue;
    }

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();

    private static void ValidateUniqueIds(
        IEnumerable<long> ids,
        string parameterName)
    {
        var seen = new HashSet<long>();
        if (!ids.All(seen.Add))
        {
            throw new ArgumentException(
                "Macro configuration entry identifiers must be unique.",
                parameterName);
        }
    }

    private static void ValidateEntries<T>(
        ImmutableArray<T> entries,
        string parameterName)
        where T : class
    {
        if (entries.Any(entry => entry is null))
        {
            throw new ArgumentException(
                "Macro configuration entries cannot contain null values.",
                parameterName);
        }
    }

    private static void ValidateUniqueNames(
        IEnumerable<string> names,
        string parameterName)
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (!names.All(seen.Add))
        {
            throw new ArgumentException(
                "Macro configuration skill names must be unique.",
                parameterName);
        }
    }
}
