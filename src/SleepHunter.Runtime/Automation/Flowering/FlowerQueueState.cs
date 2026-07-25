using System.Collections.Immutable;
using SleepHunter.Runtime.Automation.Spells;

namespace SleepHunter.Runtime.Automation.Flowering;

public sealed class FlowerQueueState : IEquatable<FlowerQueueState>
{
    public static FlowerQueueState Empty { get; } = new(
        ImmutableArray<FlowerQueueEntry>.Empty,
        cursor: 0);

    internal FlowerQueueState(
        ImmutableArray<FlowerQueueEntry> entries,
        int cursor)
    {
        if (entries.IsDefault)
        {
            throw new ArgumentException(
                "Flower queue entries must be an initialized immutable array.",
                nameof(entries));
        }

        if (entries.Any(entry => entry is null))
        {
            throw new ArgumentException(
                "Flower queue entries cannot contain null values.",
                nameof(entries));
        }

        var maximumCursor = entries.IsEmpty ? 0 : entries.Length - 1;
        if (cursor < 0 || cursor > maximumCursor)
        {
            throw new ArgumentOutOfRangeException(
                nameof(cursor),
                cursor,
                "Flower queue cursor must refer to an existing entry.");
        }

        if (entries.Select(entry => entry.Id).Distinct().Count() !=
            entries.Length)
        {
            throw new ArgumentException(
                "Flower queue entry identifiers must be unique.",
                nameof(entries));
        }

        Entries = entries;
        Cursor = cursor;
    }

    public ImmutableArray<FlowerQueueEntry> Entries { get; }

    public int Cursor { get; }

    public FlowerQueueState Add(
        FlowerQueueEntry entry,
        int? index = null)
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

    public FlowerQueueState Update(FlowerQueueEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);

        var index = IndexOf(entry.Id);
        if (index < 0 || Entries[index] == entry)
        {
            return this;
        }

        var builder = Entries.ToBuilder();
        builder[index] = entry;
        return new FlowerQueueState(builder.ToImmutable(), Cursor);
    }

    public FlowerQueueState Remove(FlowerQueueEntryId id)
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
            return new FlowerQueueState(entries, cursor: 0);
        }

        if (removedCurrent)
        {
            var nextCursor = removalIndex < entries.Length
                ? removalIndex
                : 0;
            return new FlowerQueueState(entries, nextCursor);
        }

        return CreatePreservingCurrent(entries, currentId);
    }

    public FlowerQueueState Move(
        FlowerQueueEntryId id,
        int targetIndex)
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

    public FlowerQueueState Clear() =>
        Entries.IsEmpty
            ? this
            : new FlowerQueueState(
                ImmutableArray<FlowerQueueEntry>.Empty,
                cursor: 0);

    public bool Equals(FlowerQueueState? other) =>
        other is not null &&
        Cursor == other.Cursor &&
        Entries.SequenceEqual(other.Entries);

    public override bool Equals(object? obj) =>
        obj is FlowerQueueState other && Equals(other);

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

    internal FlowerQueueEvaluation EvaluateNext(
        IReadOnlyDictionary<FlowerQueueEntryId, FlowerQueueAvailability>
            availability,
        bool prioritizeAlternateCharacters)
    {
        ArgumentNullException.ThrowIfNull(availability);

        if (Entries.IsEmpty)
        {
            return new FlowerQueueEvaluation(
                SelectedEntry: null,
                this);
        }

        if (prioritizeAlternateCharacters)
        {
            var characterEntry = FindNext(
                availability,
                requireCharacterTarget: true);
            if (characterEntry is not null)
            {
                return Select(characterEntry);
            }
        }

        var selectedEntry = FindNext(
            availability,
            requireCharacterTarget: false);
        return selectedEntry is null
            ? new FlowerQueueEvaluation(SelectedEntry: null, this)
            : Select(selectedEntry);
    }

    private FlowerQueueEntry? FindNext(
        IReadOnlyDictionary<FlowerQueueEntryId, FlowerQueueAvailability>
            availability,
        bool requireCharacterTarget)
    {
        for (var offset = 0; offset < Entries.Length; offset++)
        {
            var index = (Cursor + offset) % Entries.Length;
            var entry = Entries[index];
            if (requireCharacterTarget &&
                entry.Target.Kind != SpellTargetKind.Character)
            {
                continue;
            }

            if (GetAvailability(availability, entry.Id) ==
                FlowerQueueAvailability.Ready)
            {
                return entry;
            }
        }

        return null;
    }

    private FlowerQueueEvaluation Select(FlowerQueueEntry entry)
    {
        var index = IndexOf(entry.Id);
        var nextCursor = (index + 1) % Entries.Length;
        return new FlowerQueueEvaluation(
            entry,
            SetCursor(nextCursor));
    }

    private FlowerQueueState SetCursor(int cursor) =>
        cursor == Cursor
            ? this
            : new FlowerQueueState(Entries, cursor);

    private static FlowerQueueState CreatePreservingCurrent(
        ImmutableArray<FlowerQueueEntry> entries,
        FlowerQueueEntryId? currentId)
    {
        if (currentId is null)
        {
            return new FlowerQueueState(entries, cursor: 0);
        }

        var cursor = IndexOf(entries, currentId.Value);
        if (cursor < 0)
        {
            throw new InvalidOperationException(
                "The current flower queue entry was not preserved.");
        }

        return new FlowerQueueState(entries, cursor);
    }

    private FlowerQueueEntryId? GetCurrentEntryId() =>
        Entries.IsEmpty
            ? null
            : Entries[Cursor].Id;

    private int IndexOf(FlowerQueueEntryId id) =>
        IndexOf(Entries, id);

    private static int IndexOf(
        ImmutableArray<FlowerQueueEntry> entries,
        FlowerQueueEntryId id)
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

    private static FlowerQueueAvailability GetAvailability(
        IReadOnlyDictionary<FlowerQueueEntryId, FlowerQueueAvailability>
            availability,
        FlowerQueueEntryId id) =>
        availability.TryGetValue(id, out var value)
            ? value
            : FlowerQueueAvailability.Unavailable;
}
