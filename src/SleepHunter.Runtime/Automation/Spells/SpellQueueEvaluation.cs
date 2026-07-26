namespace SleepHunter.Runtime.Automation.Spells;

public sealed record SpellQueueEvaluation(
    SpellQueueEntry? SelectedEntry,
    SpellQueueState State)
{
    public bool HasSelection => SelectedEntry is not null;
}
