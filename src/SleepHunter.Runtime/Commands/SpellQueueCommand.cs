using SleepHunter.Runtime.Automation.Spells;

namespace SleepHunter.Runtime.Commands;

public sealed record AddSpellQueueEntryCommand : MacroCommand
{
    public AddSpellQueueEntryCommand(
        SpellQueueEntry entry,
        int? index = null)
    {
        ArgumentNullException.ThrowIfNull(entry);

        if (index < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(index),
                index,
                "Spell queue insertion indices cannot be negative.");
        }

        Entry = entry;
        Index = index;
    }

    public SpellQueueEntry Entry { get; }

    public int? Index { get; }
}

public sealed record UpdateSpellQueueEntryCommand : MacroCommand
{
    public UpdateSpellQueueEntryCommand(SpellQueueEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);
        Entry = entry;
    }

    public SpellQueueEntry Entry { get; }
}

public sealed record RemoveSpellQueueEntryCommand : MacroCommand
{
    public RemoveSpellQueueEntryCommand(SpellQueueEntryId entryId)
    {
        if (entryId.Value <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(entryId),
                entryId,
                "Spell queue removal requires a valid entry identifier.");
        }

        EntryId = entryId;
    }

    public SpellQueueEntryId EntryId { get; }
}

public sealed record MoveSpellQueueEntryCommand : MacroCommand
{
    public MoveSpellQueueEntryCommand(
        SpellQueueEntryId entryId,
        int targetIndex)
    {
        if (entryId.Value <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(entryId),
                entryId,
                "Spell queue movement requires a valid entry identifier.");
        }

        if (targetIndex < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(targetIndex),
                targetIndex,
                "Spell queue target indices cannot be negative.");
        }

        EntryId = entryId;
        TargetIndex = targetIndex;
    }

    public SpellQueueEntryId EntryId { get; }

    public int TargetIndex { get; }
}

public sealed record ClearSpellQueueCommand : MacroCommand;

public sealed record SetSpellQueueRotationCommand : MacroCommand
{
    public SetSpellQueueRotationCommand(SpellQueueRotation rotation)
    {
        if (!Enum.IsDefined(rotation))
        {
            throw new ArgumentOutOfRangeException(
                nameof(rotation),
                rotation,
                "Spell queue rotation is not supported.");
        }

        Rotation = rotation;
    }

    public SpellQueueRotation Rotation { get; }
}
