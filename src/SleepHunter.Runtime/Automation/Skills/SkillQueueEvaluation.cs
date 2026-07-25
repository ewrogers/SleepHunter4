namespace SleepHunter.Runtime.Automation.Skills;

public sealed record SkillQueueEvaluation(
    SkillQueueEntry? SelectedEntry,
    SkillQueueState State)
{
    public bool HasSelection => SelectedEntry is not null;
}
