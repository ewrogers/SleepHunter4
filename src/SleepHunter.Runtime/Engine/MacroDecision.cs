using System.Collections.Immutable;

using SleepHunter.Runtime.Effects;
using SleepHunter.Runtime.Events;
using SleepHunter.Runtime.Time;

namespace SleepHunter.Runtime.Engine;

public sealed record MacroDecision
{
    internal MacroDecision(
        MacroState state,
        ImmutableArray<MacroEvent> raisedEvents,
        MacroEffect? effect,
        MacroTimestamp? nextDeadline,
        MacroViewSnapshot? publishedView)
    {
        ArgumentNullException.ThrowIfNull(state);

        if (raisedEvents.IsDefault)
        {
            throw new ArgumentException(
                "Raised events must be an initialized immutable array.",
                nameof(raisedEvents));
        }

        State = state;
        RaisedEvents = raisedEvents;
        Effect = effect;
        NextDeadline = nextDeadline;
        PublishedView = publishedView;
    }

    public MacroState State { get; }

    public ImmutableArray<MacroEvent> RaisedEvents { get; }

    public MacroEffect? Effect { get; }

    public MacroTimestamp? NextDeadline { get; }

    public MacroViewSnapshot? PublishedView { get; }
}
