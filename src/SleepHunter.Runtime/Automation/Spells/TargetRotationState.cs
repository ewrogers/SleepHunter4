using System.Collections.Immutable;

namespace SleepHunter.Runtime.Automation.Spells;

public sealed class TargetRotationState : IEquatable<TargetRotationState>
{
    public static TargetRotationState Empty { get; } = new(
        ImmutableDictionary<long, TargetCursor>.Empty);

    private readonly ImmutableDictionary<long, TargetCursor> cursors;

    private TargetRotationState(
        ImmutableDictionary<long, TargetCursor> cursors)
    {
        this.cursors = cursors;
    }

    public int Count => cursors.Count;

    public int GetCursor(long entryId) =>
        cursors.TryGetValue(entryId, out var cursor)
            ? cursor.Index
            : 0;

    public TargetResolution Resolve(long entryId, SpellTarget target)
    {
        ArgumentNullException.ThrowIfNull(target);

        var cursor =
            cursors.TryGetValue(entryId, out var current) &&
            current.Target == target
                ? current.Index
                : 0;
        return TargetResolver.Resolve(target, cursor);
    }

    public TargetRotationState Advance(
        long entryId,
        SpellTarget target,
        TargetResolution resolution) =>
        resolution.PointCount <= 1
            ? Remove(entryId)
            : SetCursor(entryId, target, resolution.NextIndex);

    public TargetRotationState Synchronize(
        IEnumerable<KeyValuePair<long, SpellTarget>> entries)
    {
        ArgumentNullException.ThrowIfNull(entries);

        var synchronized =
            ImmutableDictionary.CreateBuilder<long, TargetCursor>();
        foreach (var (entryId, target) in entries)
        {
            if (entryId <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(entries),
                    entryId,
                    "Target rotation entry identifiers must be positive.");
            }

            ArgumentNullException.ThrowIfNull(target);
            if (!target.IsArea)
            {
                continue;
            }

            var cursor =
                cursors.TryGetValue(entryId, out var current) &&
                current.Target == target
                    ? current.Index
                    : 0;
            var resolution = TargetResolver.Resolve(target, cursor);
            synchronized[entryId] = new TargetCursor(
                target,
                resolution.SelectedIndex);
        }

        var next = synchronized.ToImmutable();
        return CursorsEqual(cursors, next)
            ? this
            : new TargetRotationState(next);
    }

    public bool Equals(TargetRotationState? other) =>
        other is not null &&
        CursorsEqual(cursors, other.cursors);

    public override bool Equals(object? obj) =>
        obj is TargetRotationState other && Equals(other);

    public override int GetHashCode()
    {
        var hash = cursors.Count;
        foreach (var entry in cursors)
        {
            hash ^= HashCode.Combine(entry.Key, entry.Value);
        }

        return hash;
    }

    private TargetRotationState SetCursor(
        long entryId,
        SpellTarget target,
        int cursor)
    {
        if (entryId <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(entryId),
                entryId,
                "Target rotation entry identifiers must be positive.");
        }

        if (cursor < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(cursor),
                cursor,
                "The target cursor cannot be negative.");
        }

        ArgumentNullException.ThrowIfNull(target);
        return new TargetRotationState(
            cursors.SetItem(entryId, new TargetCursor(target, cursor)));
    }

    private TargetRotationState Remove(long entryId) =>
        cursors.ContainsKey(entryId)
            ? new TargetRotationState(cursors.Remove(entryId))
            : this;

    private static bool CursorsEqual(
        ImmutableDictionary<long, TargetCursor> left,
        ImmutableDictionary<long, TargetCursor> right) =>
        left.Count == right.Count &&
        left.All(entry =>
            right.TryGetValue(entry.Key, out var value) &&
            value == entry.Value);

    private sealed record TargetCursor(SpellTarget Target, int Index);
}
