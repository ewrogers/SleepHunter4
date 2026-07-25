using System.Collections.Immutable;

namespace SleepHunter.Runtime.Automation.Skills;

public sealed class SkillQueueState : IEquatable<SkillQueueState>
{
    public static SkillQueueState Empty { get; } = new(
        ImmutableArray<SkillQueueEntry>.Empty,
        cursor: 0);

    internal SkillQueueState(
        ImmutableArray<SkillQueueEntry> entries,
        int cursor)
    {
        if (entries.IsDefault)
        {
            throw new ArgumentException(
                "Skill queue entries must be an initialized immutable array.",
                nameof(entries));
        }

        if (entries.Any(entry => entry is null))
        {
            throw new ArgumentException(
                "Skill queue entries cannot contain null values.",
                nameof(entries));
        }

        var maximumCursor = entries.IsEmpty ? 0 : entries.Length - 1;
        if (cursor < 0 || cursor > maximumCursor)
        {
            throw new ArgumentOutOfRangeException(
                nameof(cursor),
                cursor,
                "Skill queue cursor must refer to an existing entry.");
        }

        if (entries.Select(entry => entry.Id).Distinct().Count() !=
            entries.Length)
        {
            throw new ArgumentException(
                "Skill queue entry identifiers must be unique.",
                nameof(entries));
        }

        if (entries
            .Select(entry => entry.Name)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Count() != entries.Length)
        {
            throw new ArgumentException(
                "Skill queue entry names must be unique.",
                nameof(entries));
        }

        Entries = entries;
        Cursor = cursor;
    }

    public ImmutableArray<SkillQueueEntry> Entries { get; }

    public int Cursor { get; }

    public SkillQueueState Add(SkillQueueEntry entry, int? index = null)
    {
        ArgumentNullException.ThrowIfNull(entry);

        var insertionIndex = index ?? Entries.Length;
        if (insertionIndex < 0 ||
            insertionIndex > Entries.Length ||
            IndexOf(entry.Id) >= 0 ||
            IndexOfName(entry.Name, excludedId: null) >= 0)
        {
            return this;
        }

        var currentId = GetCurrentEntryId();
        var builder = Entries.ToBuilder();
        builder.Insert(insertionIndex, entry);
        return CreatePreservingCurrent(builder.ToImmutable(), currentId);
    }

    public SkillQueueState Update(SkillQueueEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);

        var index = IndexOf(entry.Id);
        if (index < 0 ||
            Entries[index] == entry ||
            IndexOfName(entry.Name, entry.Id) >= 0)
        {
            return this;
        }

        var builder = Entries.ToBuilder();
        builder[index] = entry;
        return new SkillQueueState(builder.ToImmutable(), Cursor);
    }

    public SkillQueueState Remove(SkillQueueEntryId id)
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
            return new SkillQueueState(entries, cursor: 0);
        }

        if (removedCurrent)
        {
            var nextCursor = removalIndex < entries.Length
                ? removalIndex
                : 0;
            return new SkillQueueState(entries, nextCursor);
        }

        return CreatePreservingCurrent(entries, currentId);
    }

    public SkillQueueState Move(SkillQueueEntryId id, int targetIndex)
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

    public SkillQueueState Clear() =>
        Entries.IsEmpty
            ? this
            : new SkillQueueState(
                ImmutableArray<SkillQueueEntry>.Empty,
                cursor: 0);

    public SkillQueueEvaluation EvaluateNext(
        IReadOnlyDictionary<SkillQueueEntryId, SkillQueueAvailability>
            availability)
    {
        ArgumentNullException.ThrowIfNull(availability);

        if (Entries.IsEmpty)
        {
            return new SkillQueueEvaluation(SelectedEntry: null, this);
        }

        for (var offset = 0; offset < Entries.Length; offset++)
        {
            var index = (Cursor + offset) % Entries.Length;
            var entry = Entries[index];
            if (GetAvailability(availability, entry.Id) !=
                SkillQueueAvailability.Ready)
            {
                continue;
            }

            var nextCursor = (index + 1) % Entries.Length;
            return new SkillQueueEvaluation(entry, SetCursor(nextCursor));
        }

        return new SkillQueueEvaluation(SelectedEntry: null, this);
    }

    public bool Equals(SkillQueueState? other) =>
        other is not null &&
        Cursor == other.Cursor &&
        Entries.SequenceEqual(other.Entries);

    public override bool Equals(object? obj) =>
        obj is SkillQueueState other && Equals(other);

    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(Cursor);
        foreach (var entry in Entries)
        {
            hash.Add(entry);
        }

        return hash.ToHashCode();
    }

    private SkillQueueState SetCursor(int cursor) =>
        cursor == Cursor
            ? this
            : new SkillQueueState(Entries, cursor);

    private static SkillQueueState CreatePreservingCurrent(
        ImmutableArray<SkillQueueEntry> entries,
        SkillQueueEntryId? currentId)
    {
        if (currentId is null)
        {
            return new SkillQueueState(entries, cursor: 0);
        }

        var cursor = IndexOf(entries, currentId.Value);
        if (cursor < 0)
        {
            throw new InvalidOperationException(
                "The current skill queue entry was not preserved.");
        }

        return new SkillQueueState(entries, cursor);
    }

    private SkillQueueEntryId? GetCurrentEntryId() =>
        Entries.IsEmpty
            ? null
            : Entries[Cursor].Id;

    private int IndexOf(SkillQueueEntryId id) => IndexOf(Entries, id);

    private int IndexOfName(
        string name,
        SkillQueueEntryId? excludedId)
    {
        for (var index = 0; index < Entries.Length; index++)
        {
            var entry = Entries[index];
            if ((excludedId is null || entry.Id != excludedId.Value) &&
                string.Equals(
                    entry.Name,
                    name,
                    StringComparison.OrdinalIgnoreCase))
            {
                return index;
            }
        }

        return -1;
    }

    private static int IndexOf(
        ImmutableArray<SkillQueueEntry> entries,
        SkillQueueEntryId id)
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

    private static SkillQueueAvailability GetAvailability(
        IReadOnlyDictionary<SkillQueueEntryId, SkillQueueAvailability>
            availability,
        SkillQueueEntryId id) =>
        availability.TryGetValue(id, out var value)
            ? value
            : SkillQueueAvailability.Missing;
}
