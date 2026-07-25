using SleepHunter.Runtime.Actions;
using SleepHunter.Runtime.Automation.Flowering;
using SleepHunter.Runtime.Automation.Panels;
using SleepHunter.Runtime.Automation.Spells;
using SleepHunter.Runtime.Commands;
using SleepHunter.Runtime.Engine;
using SleepHunter.Runtime.Hosting;
using SleepHunter.Runtime.Intents;
using SleepHunter.Runtime.Snapshots;
using SleepHunter.Runtime.Tests.Scenarios;
using SleepHunter.Runtime.Time;

namespace SleepHunter.Runtime.Tests.Hosting;

public sealed class FlowerSessionTests
{
    [Test]
    public async Task ShouldCoordinateFlowerThroughSessionMailboxes()
    {
        var timing = new SpellCastTimingPolicy(
            zeroLineDuration: TimeSpan.Zero,
            singleLineDuration: TimeSpan.FromMilliseconds(10),
            multiLineDurationPerLine: TimeSpan.FromMilliseconds(10),
            completionPadding: TimeSpan.FromMilliseconds(1));
        var policy = new FlowerExecutionPolicy(
            target: new FlowerTargetPolicy(
                autoFlowerWaitingCharacters: true),
            spell: new SpellExecutionPolicy(
                new SpellCastPolicy(requireMana: true, timing),
                PanelTransitionPolicy.Default,
                allowStaffSwitching: false));
        var timeProvider = new ManualTimeProvider();
        await using var session = new MacroSession(
            new MacroEngine(),
            new MacroClock(timeProvider));

        session.PublishSnapshot(SourceSnapshot());
        await session.Views.ReadUntilAsync(view => view.Revision == 1);
        session.PublishClientRoster(
            new ClientRosterSnapshot(
                new ClientRosterSequence(1),
                MacroTimestamp.Zero,
                [SourceClient(), WaitingClient()]));
        var observed = await session.Views.ReadUntilAsync(
            view => view.ClientRosterSequence?.Value == 1);
        await session.SendCommandAsync(new StartMacroCommand());
        await session.SendCommandAsync(new FlowerCommand(policy));

        var intent = (CastSpellIntent)await session.Intents.ReadUntilAsync(
            value => value is CastSpellIntent);
        await session.ReportActionIssueAsync(
            new ClientActionIssue(
                intent.ActionId,
                ClientActionIssueStatus.Issued));
        timeProvider.Advance(timing.CalculateDuration(castLines: 1));
        var completed = await session.Views.ReadUntilAsync(
            view => view.Flower?.Status == FlowerStatus.Succeeded);

        Assert.Multiple(() =>
        {
            Assert.That(
                observed.ClientRosterSequence?.Value,
                Is.EqualTo(1));
            Assert.That(
                intent.SpellName,
                Is.EqualTo(FlowerSpellNames.Plant));
            Assert.That(
                intent.Target,
                Is.EqualTo(SpellTarget.RelativeTile(5, 5)));
            Assert.That(completed.PendingActionId, Is.Null);
            Assert.That(
                completed.Flower?.FloweredAt,
                Is.EqualTo(
                    new MacroTimestamp(
                        timing.CalculateDuration(castLines: 1))));
        });
    }

    [Test]
    public async Task ShouldRejectClientRosterAfterDisposal()
    {
        var timeProvider = new ManualTimeProvider();
        var session = new MacroSession(
            new MacroEngine(),
            new MacroClock(timeProvider));
        var snapshot = new ClientRosterSnapshot(
            new ClientRosterSequence(1),
            MacroTimestamp.Zero,
            []);

        await session.DisposeAsync();

        Assert.That(
            () => session.PublishClientRoster(snapshot),
            Throws.TypeOf<ObjectDisposedException>());
    }

    private static ClientSnapshot SourceSnapshot()
    {
        var spell = new SpellSnapshot(
            FlowerSpellNames.Plant,
            slot: 1,
            currentLevel: 0,
            maximumLevel: 100,
            castLines: 1,
            manaCost: 100,
            cooldown: TimeSpan.Zero);
        return new ClientSnapshot(
            new SnapshotSequence(1),
            MacroTimestamp.Zero,
            MacroTimestamp.Zero,
            new ClientIdentity("source", "test"),
            SnapshotQuality.Complete,
            ClientPresence.InWorld,
            ClientPanel.TemuairSpells,
            character: null,
            inventory: null,
            equipment: null,
            new VitalsSnapshot(100, 100, 500, 1000),
            new SpellbookSnapshot([spell]),
            skillbook: null,
            SourceLocation());
    }

    private static ClientRosterEntry WaitingClient() =>
        new(
            new ClientIdentity("waiting", "test"),
            "waiting",
            ClientPresence.InWorld,
            isMacroRunning: true,
            isWaitingForMana: true,
            new MapLocationSnapshot(
                mapNumber: 1,
                mapName: "test map",
                x: 55,
                y: 55),
            new VitalsSnapshot(100, 100, 0, 1000));

    private static ClientRosterEntry SourceClient() =>
        new(
            new ClientIdentity("source", "test"),
            "source",
            ClientPresence.InWorld,
            isMacroRunning: true,
            isWaitingForMana: false,
            SourceLocation(),
            new VitalsSnapshot(100, 100, 500, 1000));

    private static MapLocationSnapshot SourceLocation() =>
        new(
            mapNumber: 1,
            mapName: "test map",
            x: 50,
            y: 50);
}
