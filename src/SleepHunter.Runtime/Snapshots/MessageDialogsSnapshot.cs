using System.Collections.Immutable;

namespace SleepHunter.Runtime.Snapshots;

public sealed class MessageDialogsSnapshot :
    IEquatable<MessageDialogsSnapshot>
{
    public static MessageDialogsSnapshot Empty { get; } = new([]);

    public MessageDialogsSnapshot(
        IEnumerable<MessageDialogSnapshot> dialogs)
    {
        ArgumentNullException.ThrowIfNull(dialogs);

        var entries = dialogs.ToImmutableArray();
        if (entries.Any(dialog => dialog is null))
        {
            throw new ArgumentException(
                "Message dialog snapshots cannot contain null entries.",
                nameof(dialogs));
        }

        Dialogs = entries;
    }

    public ImmutableArray<MessageDialogSnapshot> Dialogs { get; }

    public int Count => Dialogs.Length;

    public bool IsOpen => Count > 0;

    public bool Equals(MessageDialogsSnapshot? other) =>
        other is not null &&
        Dialogs.SequenceEqual(other.Dialogs);

    public override bool Equals(object? obj) =>
        obj is MessageDialogsSnapshot other && Equals(other);

    public override int GetHashCode()
    {
        var hash = new HashCode();
        foreach (var dialog in Dialogs)
        {
            hash.Add(dialog);
        }

        return hash.ToHashCode();
    }
}
