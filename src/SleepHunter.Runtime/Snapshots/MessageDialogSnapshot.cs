namespace SleepHunter.Runtime.Snapshots;

public sealed record MessageDialogSnapshot
{
    public MessageDialogSnapshot(
        uint treeDepth,
        uint registrationIdentity,
        string text)
    {
        ArgumentNullException.ThrowIfNull(text);

        TreeDepth = treeDepth;
        RegistrationIdentity = registrationIdentity;
        Text = text;
    }

    public uint TreeDepth { get; }

    public uint RegistrationIdentity { get; }

    public string Text { get; }
}
