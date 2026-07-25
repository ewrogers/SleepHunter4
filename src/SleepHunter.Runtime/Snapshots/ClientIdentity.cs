namespace SleepHunter.Runtime.Snapshots;

public sealed record ClientIdentity
{
    public ClientIdentity(string instanceId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(instanceId);

        InstanceId = instanceId;
    }

    public string InstanceId { get; }
}
