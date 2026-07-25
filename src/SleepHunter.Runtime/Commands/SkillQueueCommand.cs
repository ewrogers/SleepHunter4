using SleepHunter.Runtime.Automation.Skills;

namespace SleepHunter.Runtime.Commands;

public sealed record AddSkillQueueEntryCommand : MacroCommand
{
    public AddSkillQueueEntryCommand(
        SkillQueueEntry entry,
        int? index = null)
    {
        ArgumentNullException.ThrowIfNull(entry);

        if (index < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(index),
                index,
                "Skill queue insertion indices cannot be negative.");
        }

        Entry = entry;
        Index = index;
    }

    public SkillQueueEntry Entry { get; }

    public int? Index { get; }
}

public sealed record UpdateSkillQueueEntryCommand : MacroCommand
{
    public UpdateSkillQueueEntryCommand(SkillQueueEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);
        Entry = entry;
    }

    public SkillQueueEntry Entry { get; }
}

public sealed record RemoveSkillQueueEntryCommand : MacroCommand
{
    public RemoveSkillQueueEntryCommand(SkillQueueEntryId entryId)
    {
        if (entryId.Value <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(entryId),
                entryId,
                "Skill queue removal requires a valid entry identifier.");
        }

        EntryId = entryId;
    }

    public SkillQueueEntryId EntryId { get; }
}

public sealed record MoveSkillQueueEntryCommand : MacroCommand
{
    public MoveSkillQueueEntryCommand(
        SkillQueueEntryId entryId,
        int targetIndex)
    {
        if (entryId.Value <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(entryId),
                entryId,
                "Skill queue movement requires a valid entry identifier.");
        }

        if (targetIndex < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(targetIndex),
                targetIndex,
                "Skill queue target indices cannot be negative.");
        }

        EntryId = entryId;
        TargetIndex = targetIndex;
    }

    public SkillQueueEntryId EntryId { get; }

    public int TargetIndex { get; }
}

public sealed record ClearSkillQueueCommand : MacroCommand;
