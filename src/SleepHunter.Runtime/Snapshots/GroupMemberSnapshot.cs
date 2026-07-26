namespace SleepHunter.Runtime.Snapshots;

public sealed record GroupMemberSnapshot
{
    public GroupMemberSnapshot(string name, bool isStarred)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        Name = name.Trim();
        IsStarred = isStarred;
    }

    public string Name { get; }

    public bool IsStarred { get; }
}
