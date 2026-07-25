using System.Collections.Immutable;

namespace SleepHunter.Interop.Input;

public sealed record WindowInputPlan
{
    public const int MaximumMessageCount = 32;
    public const int MaximumCleanupMessageCount = 8;

    public WindowInputPlan(
        IEnumerable<WindowInputMessage> messages,
        IEnumerable<WindowInputMessage>? cleanupMessages = null)
    {
        ArgumentNullException.ThrowIfNull(messages);

        var plannedMessages = messages.ToImmutableArray();
        if (plannedMessages.IsEmpty ||
            plannedMessages.Length > MaximumMessageCount)
        {
            throw new ArgumentException(
                $"Input plans must contain between 1 and {MaximumMessageCount} messages.",
                nameof(messages));
        }

        var plannedCleanup = cleanupMessages?.ToImmutableArray() ??
            ImmutableArray<WindowInputMessage>.Empty;
        if (plannedCleanup.Length > MaximumCleanupMessageCount)
        {
            throw new ArgumentException(
                $"Input plans cannot contain more than {MaximumCleanupMessageCount} cleanup messages.",
                nameof(cleanupMessages));
        }

        Messages = plannedMessages;
        CleanupMessages = plannedCleanup;
    }

    public ImmutableArray<WindowInputMessage> Messages { get; }

    public ImmutableArray<WindowInputMessage> CleanupMessages { get; }
}
