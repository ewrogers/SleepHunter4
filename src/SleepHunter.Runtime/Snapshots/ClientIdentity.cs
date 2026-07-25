namespace SleepHunter.Runtime.Snapshots;

public sealed record ClientIdentity
{
    public ClientIdentity(string instanceId, string version)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(instanceId);
        ArgumentException.ThrowIfNullOrWhiteSpace(version);

        InstanceId = instanceId;
        Version = version;
    }

    public string InstanceId { get; }

    public string Version { get; }
}
