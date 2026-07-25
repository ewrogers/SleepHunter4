using System.Collections.Immutable;
using SleepHunter.Runtime.Events;
using SleepHunter.Runtime.Intents;
using SleepHunter.Runtime.Time;

namespace SleepHunter.Runtime.Engine;

public sealed record MacroDecision
{
    internal MacroDecision(
        MacroState state,
        ImmutableArray<MacroEvent> raisedEvents,
        ImmutableArray<ScheduledMacroEvent> scheduledEvents,
        MacroIntent? intent,
        MacroViewSnapshot? publishedView)
    {
        ArgumentNullException.ThrowIfNull(state);

        if (raisedEvents.IsDefault)
        {
            throw new ArgumentException(
                "Raised events must be an initialized immutable array.",
                nameof(raisedEvents));
        }

        if (scheduledEvents.IsDefault)
        {
            throw new ArgumentException(
                "Scheduled events must be an initialized immutable array.",
                nameof(scheduledEvents));
        }

        State = state;
        RaisedEvents = raisedEvents;
        ScheduledEvents = scheduledEvents;
        Intent = intent;
        PublishedView = publishedView;
    }

    public MacroState State { get; }

    public ImmutableArray<MacroEvent> RaisedEvents { get; }

    public ImmutableArray<ScheduledMacroEvent> ScheduledEvents { get; }

    public MacroIntent? Intent { get; }

    public MacroTimestamp? NextDeadline =>
        ScheduledEvents.IsEmpty
            ? null
            : ScheduledEvents.Min(scheduledEvent => scheduledEvent.DueAt);

    public MacroViewSnapshot? PublishedView { get; }
}
