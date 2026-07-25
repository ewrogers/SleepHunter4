using System.Collections.Immutable;

namespace SleepHunter.Runtime.Automation.Spells;

public sealed class SpellQueueState : IEquatable<SpellQueueState>
{
    public static SpellQueueState Empty { get; } = new(
        ImmutableArray<SpellQueueEntry>.Empty,
        SpellQueueRotation.Priority,
        cursor: 0);

    internal SpellQueueState(
        ImmutableArray<SpellQueueEntry> entries,
        SpellQueueRotation rotation,
        int cursor)
    {
        if (entries.IsDefault)
        {
            throw new ArgumentException(
                "Spell queue entries must be an initialized immutable array.",
                nameof(entries));
        }

        if (!Enum.IsDefined(rotation))
        {
            throw new ArgumentOutOfRangeException(
                nameof(rotation),
                rotation,
                "Spell queue rotation is not supported.");
        }

        if (entries.Any(entry => entry is null))
        {
            throw new ArgumentException(
                "Spell queue entries cannot contain null values.",
                nameof(entries));
        }

        var maximumCursor = entries.IsEmpty ? 0 : entries.Length - 1;
        if (cursor < 0 || cursor > maximumCursor)
        {
            throw new ArgumentOutOfRangeException(
                nameof(cursor),
                cursor,
                "Spell queue cursor must refer to an existing entry.");
        }

        if (entries.Select(entry => entry.Id).Distinct().Count() != entries.Length)
        {
            throw new ArgumentException(
                "Spell queue entry identifiers must be unique.",
                nameof(entries));
        }

        Entries = entries;
        Rotation = rotation;
        Cursor = cursor;
    }

    public ImmutableArray<SpellQueueEntry> Entries { get; }

    public SpellQueueRotation Rotation { get; }

    public int Cursor { get; }

    public SpellQueueState Add(SpellQueueEntry entry, int? index = null)
    {
        ArgumentNullException.ThrowIfNull(entry);

        var insertionIndex = index ?? Entries.Length;
        if (insertionIndex < 0 ||
            insertionIndex > Entries.Length ||
            IndexOf(entry.Id) >= 0)
        {
            return this;
        }

        var currentId = GetCurrentEntryId();
        var builder = Entries.ToBuilder();
        builder.Insert(insertionIndex, entry);
        return CreatePreservingCurrent(builder.ToImmutable(), currentId);
    }

    public SpellQueueState Update(SpellQueueEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);

        var index = IndexOf(entry.Id);
        if (index < 0 || Entries[index] == entry)
        {
            return this;
        }

        var builder = Entries.ToBuilder();
        builder[index] = entry;
        return new SpellQueueState(builder.ToImmutable(), Rotation, Cursor);
    }

    public SpellQueueState Remove(SpellQueueEntryId id)
    {
        var removalIndex = IndexOf(id);
        if (removalIndex < 0)
        {
            return this;
        }

        var currentId = GetCurrentEntryId();
        var removedCurrent = currentId == id;
        var builder = Entries.ToBuilder();
        builder.RemoveAt(removalIndex);
        var entries = builder.ToImmutable();

        if (entries.IsEmpty)
        {
            return new SpellQueueState(entries, Rotation, cursor: 0);
        }

        if (removedCurrent)
        {
            var nextCursor = removalIndex < entries.Length
                ? removalIndex
                : 0;
            return new SpellQueueState(entries, Rotation, nextCursor);
        }

        return CreatePreservingCurrent(entries, currentId);
    }

    public SpellQueueState Move(SpellQueueEntryId id, int targetIndex)
    {
        var sourceIndex = IndexOf(id);
        if (sourceIndex < 0 ||
            targetIndex < 0 ||
            targetIndex >= Entries.Length ||
            sourceIndex == targetIndex)
        {
            return this;
        }

        var currentId = GetCurrentEntryId();
        var entry = Entries[sourceIndex];
        var builder = Entries.ToBuilder();
        builder.RemoveAt(sourceIndex);
        builder.Insert(targetIndex, entry);
        return CreatePreservingCurrent(builder.ToImmutable(), currentId);
    }

    public SpellQueueState Clear() =>
        Entries.IsEmpty
            ? this
            : new SpellQueueState(
                ImmutableArray<SpellQueueEntry>.Empty,
                Rotation,
                cursor: 0);

    public SpellQueueState SetRotation(SpellQueueRotation rotation)
    {
        if (!Enum.IsDefined(rotation))
        {
            throw new ArgumentOutOfRangeException(
                nameof(rotation),
                rotation,
                "Spell queue rotation is not supported.");
        }

        return Rotation == rotation
            ? this
            : new SpellQueueState(Entries, rotation, cursor: 0);
    }

    public SpellQueueEvaluation EvaluateNext(
        IReadOnlyDictionary<SpellQueueEntryId, SpellQueueAvailability> availability)
    {
        ArgumentNullException.ThrowIfNull(availability);

        if (Entries.IsEmpty)
        {
            return new SpellQueueEvaluation(SelectedEntry: null, this);
        }

        return Rotation switch
        {
            SpellQueueRotation.Priority => EvaluatePriority(availability),
            SpellQueueRotation.Sequential => EvaluateSequential(availability),
            SpellQueueRotation.RoundRobin => EvaluateRoundRobin(availability),
            _ => throw new InvalidOperationException(
                "Spell queue rotation is not supported.")
        };
    }

    public bool Equals(SpellQueueState? other) =>
        other is not null &&
        Rotation == other.Rotation &&
        Cursor == other.Cursor &&
        Entries.SequenceEqual(other.Entries);

    public override bool Equals(object? obj) =>
        obj is SpellQueueState other && Equals(other);

    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(Rotation);
        hash.Add(Cursor);

        foreach (var entry in Entries)
        {
            hash.Add(entry);
        }

        return hash.ToHashCode();
    }

    private SpellQueueEvaluation EvaluatePriority(
        IReadOnlyDictionary<SpellQueueEntryId, SpellQueueAvailability> availability)
    {
        var state = SetCursor(0);
        SpellQueueEntry? selected = null;
        foreach (var entry in Entries)
        {
            var entryAvailability = GetAvailability(
                availability,
                entry.Id);
            if (entryAvailability == SpellQueueAvailability.Blocked)
            {
                break;
            }

            if (entryAvailability == SpellQueueAvailability.Ready)
            {
                selected = entry;
                break;
            }
        }

        return new SpellQueueEvaluation(selected, state);
    }

    private SpellQueueEvaluation EvaluateSequential(
        IReadOnlyDictionary<SpellQueueEntryId, SpellQueueAvailability> availability)
    {
        for (var offset = 0; offset < Entries.Length; offset++)
        {
            var index = (Cursor + offset) % Entries.Length;
            var entry = Entries[index];
            var entryAvailability = GetAvailability(availability, entry.Id);

            if (entryAvailability == SpellQueueAvailability.Ready)
            {
                return new SpellQueueEvaluation(entry, SetCursor(index));
            }

            if (entryAvailability is
                SpellQueueAvailability.TemporarilyUnavailable or
                SpellQueueAvailability.Blocked)
            {
                return new SpellQueueEvaluation(
                    SelectedEntry: null,
                    SetCursor(index));
            }
        }

        return new SpellQueueEvaluation(SelectedEntry: null, SetCursor(0));
    }

    private SpellQueueEvaluation EvaluateRoundRobin(
        IReadOnlyDictionary<SpellQueueEntryId, SpellQueueAvailability> availability)
    {
        for (var offset = 0; offset < Entries.Length; offset++)
        {
            var index = (Cursor + offset) % Entries.Length;
            var entry = Entries[index];
            var entryAvailability = GetAvailability(
                availability,
                entry.Id);
            if (entryAvailability == SpellQueueAvailability.Blocked)
            {
                return new SpellQueueEvaluation(
                    SelectedEntry: null,
                    SetCursor(index));
            }

            if (entryAvailability != SpellQueueAvailability.Ready)
            {
                continue;
            }

            var nextCursor = (index + 1) % Entries.Length;
            return new SpellQueueEvaluation(entry, SetCursor(nextCursor));
        }

        return new SpellQueueEvaluation(SelectedEntry: null, this);
    }

    private SpellQueueState SetCursor(int cursor) =>
        cursor == Cursor
            ? this
            : new SpellQueueState(Entries, Rotation, cursor);

    private SpellQueueState CreatePreservingCurrent(
        ImmutableArray<SpellQueueEntry> entries,
        SpellQueueEntryId? currentId)
    {
        if (currentId is null)
        {
            return new SpellQueueState(entries, Rotation, cursor: 0);
        }

        var cursor = IndexOf(entries, currentId.Value);
        if (cursor < 0)
        {
            throw new InvalidOperationException(
                "The current spell queue entry was not preserved.");
        }

        return new SpellQueueState(entries, Rotation, cursor);
    }

    private SpellQueueEntryId? GetCurrentEntryId() =>
        Entries.IsEmpty
            ? null
            : Entries[Cursor].Id;

    private int IndexOf(SpellQueueEntryId id) => IndexOf(Entries, id);

    private static int IndexOf(
        ImmutableArray<SpellQueueEntry> entries,
        SpellQueueEntryId id)
    {
        for (var index = 0; index < entries.Length; index++)
        {
            if (entries[index].Id == id)
            {
                return index;
            }
        }

        return -1;
    }

    private static SpellQueueAvailability GetAvailability(
        IReadOnlyDictionary<SpellQueueEntryId, SpellQueueAvailability> availability,
        SpellQueueEntryId id) =>
        availability.TryGetValue(id, out var value)
            ? value
            : SpellQueueAvailability.Missing;
}
