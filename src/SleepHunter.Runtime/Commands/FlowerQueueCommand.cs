using System.Collections.Immutable;
using SleepHunter.Runtime.Automation.Flowering;

namespace SleepHunter.Runtime.Commands;

public abstract record FlowerQueueCommand : MacroCommand;

public sealed record ReplaceFlowerQueueCommand : FlowerQueueCommand
{
    public ReplaceFlowerQueueCommand(
        IEnumerable<FlowerQueueEntry> entries)
    {
        ArgumentNullException.ThrowIfNull(entries);
        Queue = new FlowerQueueState(
            ImmutableArray.CreateRange(entries),
            cursor: 0);
    }

    public FlowerQueueState Queue { get; }
}

public sealed record AddFlowerQueueEntryCommand : FlowerQueueCommand
{
    public AddFlowerQueueEntryCommand(
        FlowerQueueEntry entry,
        int? index = null)
    {
        ArgumentNullException.ThrowIfNull(entry);

        if (index < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(index),
                index,
                "Flower queue insertion indices cannot be negative.");
        }

        Entry = entry;
        Index = index;
    }

    public FlowerQueueEntry Entry { get; }

    public int? Index { get; }
}

public sealed record UpdateFlowerQueueEntryCommand : FlowerQueueCommand
{
    public UpdateFlowerQueueEntryCommand(FlowerQueueEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);
        Entry = entry;
    }

    public FlowerQueueEntry Entry { get; }
}

public sealed record RemoveFlowerQueueEntryCommand : FlowerQueueCommand
{
    public RemoveFlowerQueueEntryCommand(FlowerQueueEntryId entryId)
    {
        if (entryId.Value <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(entryId),
                entryId,
                "Flower queue removal requires a valid entry identifier.");
        }

        EntryId = entryId;
    }

    public FlowerQueueEntryId EntryId { get; }
}

public sealed record MoveFlowerQueueEntryCommand : FlowerQueueCommand
{
    public MoveFlowerQueueEntryCommand(
        FlowerQueueEntryId entryId,
        int targetIndex)
    {
        if (entryId.Value <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(entryId),
                entryId,
                "Flower queue movement requires a valid entry identifier.");
        }

        if (targetIndex < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(targetIndex),
                targetIndex,
                "Flower queue target indices cannot be negative.");
        }

        EntryId = entryId;
        TargetIndex = targetIndex;
    }

    public FlowerQueueEntryId EntryId { get; }

    public int TargetIndex { get; }
}

public sealed record ClearFlowerQueueCommand : FlowerQueueCommand;
