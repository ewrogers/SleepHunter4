using SleepHunter.Runtime.Automation.Panels;
using SleepHunter.Runtime.Commands;
using SleepHunter.Runtime.Engine;
using SleepHunter.Runtime.Events;
using SleepHunter.Runtime.Snapshots;
using SleepHunter.Runtime.Time;

namespace SleepHunter.Runtime.Tests.Scenarios;

internal sealed class MacroScenario
{
    private readonly List<MacroDecision> decisions = [];
    private readonly IMacroEngine engine;

    public MacroScenario(
        IMacroEngine? engine = null,
        ClientIdentity? client = null)
    {
        this.engine = engine ?? new MacroEngine();
        Client = client ?? new ClientIdentity("scenario-client", "test");
    }

    public ClientIdentity Client { get; }

    public IReadOnlyList<MacroDecision> Decisions => decisions;

    public MacroTimestamp CurrentTime { get; private set; } = MacroTimestamp.Zero;

    public MacroState State { get; private set; } = MacroState.Initial;

    public void AdvanceBy(TimeSpan duration) =>
        CurrentTime = CurrentTime.Add(duration);

    public MacroDecision Start() => Send(new StartMacroCommand());

    public MacroDecision Pause() => Send(new PauseMacroCommand());

    public MacroDecision Resume() => Send(new ResumeMacroCommand());

    public MacroDecision Stop() => Send(new StopMacroCommand());

    public MacroDecision Send(MacroCommand command) =>
        Apply(new MacroCommandReceived(command));

    public MacroDecision Dispatch(MacroEvent input) => Apply(input);

    public MacroDecision Observe(
        long sequence,
        SnapshotQuality quality = SnapshotQuality.Complete,
        ClientPresence presence = ClientPresence.InWorld,
        ClientIdentity? client = null,
        ClientPanel activePanel = ClientPanel.Unknown,
        MacroTimestamp? captureStartedAt = null,
        MacroTimestamp? captureCompletedAt = null,
        CharacterSnapshot? character = null,
        InventorySnapshot? inventory = null,
        EquipmentSnapshot? equipment = null)
    {
        var startedAt = captureStartedAt ?? CurrentTime;
        var completedAt = captureCompletedAt ?? CurrentTime;
        var snapshot = new ClientSnapshot(
            new SnapshotSequence(sequence),
            startedAt,
            completedAt,
            client ?? Client,
            quality,
            presence,
            activePanel,
            character,
            inventory,
            equipment);

        return Apply(new ClientSnapshotObserved(snapshot));
    }

    private MacroDecision Apply(MacroEvent input)
    {
        var decision = engine.Decide(State, input, CurrentTime);
        State = decision.State;
        decisions.Add(decision);
        return decision;
    }
}
