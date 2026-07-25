using SleepHunter.Runtime.Actions;
using SleepHunter.Runtime.Time;

namespace SleepHunter.Runtime.Events;

public sealed record ClientActionIssueObserved : MacroEvent
{
    public ClientActionIssueObserved(
        ClientActionIssue issue,
        MacroTimestamp? observedAt = null)
    {
        ArgumentNullException.ThrowIfNull(issue);
        Issue = issue;
        ObservedAt = observedAt;
    }

    public ClientActionIssue Issue { get; }

    public MacroTimestamp? ObservedAt { get; }
}
