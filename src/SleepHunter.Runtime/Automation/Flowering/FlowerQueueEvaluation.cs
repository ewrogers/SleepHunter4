namespace SleepHunter.Runtime.Automation.Flowering;

internal sealed record FlowerQueueEvaluation(
    FlowerQueueEntry? SelectedEntry,
    FlowerQueueState State);
