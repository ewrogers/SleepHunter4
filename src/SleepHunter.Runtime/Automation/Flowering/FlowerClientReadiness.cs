namespace SleepHunter.Runtime.Automation.Flowering;

public sealed record FlowerClientReadiness(
    FlowerClientObservation Client,
    FlowerClientReadinessStatus Status);
