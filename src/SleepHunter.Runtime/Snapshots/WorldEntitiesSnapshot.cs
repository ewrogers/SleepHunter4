using System.Collections.Immutable;

namespace SleepHunter.Runtime.Snapshots;

public sealed class WorldEntitiesSnapshot :
    IEquatable<WorldEntitiesSnapshot>
{
    public static WorldEntitiesSnapshot Empty { get; } = new([]);

    public WorldEntitiesSnapshot(IEnumerable<WorldEntitySnapshot> entities)
    {
        ArgumentNullException.ThrowIfNull(entities);

        var entries = entities.ToImmutableArray();
        if (entries.Any(entity => entity is null))
        {
            throw new ArgumentException(
                "World entity snapshots cannot contain null entries.",
                nameof(entities));
        }

        if (entries.Select(entity => entity.Id).Distinct().Count() !=
            entries.Length)
        {
            throw new ArgumentException(
                "World entity identifiers must be unique.",
                nameof(entities));
        }

        Entities = entries.Sort(
            static (left, right) => left.Id.CompareTo(right.Id));
    }

    public ImmutableArray<WorldEntitySnapshot> Entities { get; }

    public WorldEntitySnapshot? Find(uint id) =>
        Entities.FirstOrDefault(entity => entity.Id == id);

    public bool Equals(WorldEntitiesSnapshot? other) =>
        other is not null &&
        Entities.SequenceEqual(other.Entities);

    public override bool Equals(object? obj) =>
        obj is WorldEntitiesSnapshot other && Equals(other);

    public override int GetHashCode()
    {
        var hash = new HashCode();
        foreach (var entity in Entities)
        {
            hash.Add(entity);
        }

        return hash.ToHashCode();
    }
}
