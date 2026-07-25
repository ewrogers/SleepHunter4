using SleepHunter.Runtime.Automation.Flowering;
using SleepHunter.Runtime.Automation.Skills;
using SleepHunter.Runtime.Automation.Spells;

namespace SleepHunter.Runtime.Commands;

public sealed record ReplaceQueuesCommand : MacroCommand
{
    public ReplaceQueuesCommand(
        IEnumerable<SpellQueueEntry> spells,
        SpellQueueRotation spellRotation,
        IEnumerable<SkillQueueEntry> skills,
        IEnumerable<FlowerQueueEntry> flowers)
    {
        SpellQueue = new ReplaceSpellQueueCommand(
            spells,
            spellRotation).Queue;
        SkillQueue = new ReplaceSkillQueueCommand(skills).Queue;
        FlowerQueue = new ReplaceFlowerQueueCommand(flowers).Queue;
    }

    public SpellQueueState SpellQueue { get; }

    public SkillQueueState SkillQueue { get; }

    public FlowerQueueState FlowerQueue { get; }
}
