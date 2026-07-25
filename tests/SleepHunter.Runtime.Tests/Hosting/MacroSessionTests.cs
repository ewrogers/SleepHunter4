using SleepHunter.Runtime.Automation.Panels;
using SleepHunter.Runtime.Automation.Staves;
using SleepHunter.Runtime.Characters;
using SleepHunter.Runtime.Commands;
using SleepHunter.Runtime.Engine;
using SleepHunter.Runtime.Hosting;
using SleepHunter.Runtime.Intents;
using SleepHunter.Runtime.Snapshots;
using SleepHunter.Runtime.Tests.Scenarios;
using SleepHunter.Runtime.Time;

namespace SleepHunter.Runtime.Tests.Hosting;

public sealed class MacroSessionTests
{
    [Test]
    public async Task ShouldRunLifecycleThroughTheSingleOwnerLoop()
    {
        var timeProvider = new ManualTimeProvider();
        await using var session = new MacroSession(
            new MacroEngine(),
            new MacroClock(timeProvider));
        var snapshot = CreateSnapshot(1, MacroTimestamp.Zero);

        session.PublishSnapshot(snapshot);
        await session.Views.ReadUntilAsync(view => view.Revision == 1);
        await session.SendCommandAsync(new StartMacroCommand());
        await session.SendCommandAsync(new PauseMacroCommand());
        await session.SendCommandAsync(new ResumeMacroCommand());
        await session.SendCommandAsync(new StopMacroCommand());

        var finalView = await session.Views.ReadUntilAsync(view => view.Revision == 5);

        Assert.Multiple(() =>
        {
            Assert.That(finalView.Lifecycle, Is.EqualTo(MacroLifecycle.Stopped));
            Assert.That(
                finalView.StopReason,
                Is.EqualTo(MacroStopReason.UserRequested));
            Assert.That(session.Intents.TryRead(out _), Is.False);
        });
    }

    [Test]
    public async Task ShouldDeliverCommandsFromConcurrentProducersWithoutLoss()
    {
        const int commandCount = 100;
        var engine = new CountingMacroEngine();
        var timeProvider = new ManualTimeProvider();
        await using var session = new MacroSession(
            engine,
            new MacroClock(timeProvider));

        var producers = Enumerable
            .Range(1, commandCount)
            .Select(value =>
                session.SendCommandAsync(new TestMacroCommand(value)).AsTask());

        await Task.WhenAll(producers);
        await session.Views.ReadUntilAsync(view => view.Revision == commandCount);

        Assert.Multiple(() =>
        {
            Assert.That(engine.ReceivedCommands, Has.Count.EqualTo(commandCount));
            Assert.That(
                engine.ReceivedCommands,
                Is.EquivalentTo(Enumerable.Range(1, commandCount)));
        });
    }

    [Test]
    public async Task ShouldDispatchScheduledEventsWhenVirtualTimeReachesDeadline()
    {
        var timeProvider = new ManualTimeProvider();
        var engine = new SchedulingMacroEngine(TimeSpan.FromSeconds(1));
        await using var session = new MacroSession(
            engine,
            new MacroClock(timeProvider));

        await session.SendCommandAsync(new TestMacroCommand(1));
        await engine.Scheduled.WaitAsync(TimeSpan.FromSeconds(5));
        timeProvider.Advance(TimeSpan.FromMilliseconds(999));

        var initialView =
            await session.Views.ReadUntilAsync(view => view.Revision == 0);
        Assert.That(initialView.Revision, Is.Zero);

        for (var iteration = 0;
             iteration < 100 && !engine.Processed.IsCompleted;
             iteration++)
        {
            timeProvider.Advance(TimeSpan.FromMilliseconds(10));
            await Task.Delay(TimeSpan.FromMilliseconds(1));
        }

        await engine.Processed.WaitAsync(TimeSpan.FromSeconds(5));
        var deadlineView =
            await session.Views.ReadUntilAsync(view => view.Revision == 1);

        Assert.Multiple(() =>
        {
            Assert.That(deadlineView.Revision, Is.EqualTo(1));
            Assert.That(
                engine.ProcessedAt,
                Is.GreaterThanOrEqualTo(
                    new MacroTimestamp(TimeSpan.FromSeconds(1))));
        });
    }

    [Test]
    public async Task ShouldRetryAndConfirmPanelTransitionThroughSession()
    {
        var timeProvider = new ManualTimeProvider();
        await using var session = new MacroSession(
            new MacroEngine(),
            new MacroClock(timeProvider));
        var policy = new PanelTransitionPolicy(
            TimeSpan.FromMilliseconds(100),
            maximumAttempts: 2);

        session.PublishSnapshot(
            CreateSnapshot(
                sequence: 1,
                MacroTimestamp.Zero,
                ClientPanel.Inventory));
        await session.Views.ReadUntilAsync(view => view.Revision == 1);
        await session.SendCommandAsync(new StartMacroCommand());
        await session.Views.ReadUntilAsync(view => view.Revision == 2);
        await session.SendCommandAsync(
            new RequestPanelTransitionCommand(
                ClientPanel.TemuairSpells,
                policy));

        var firstIntent = (SwitchPanelIntent)await session.Intents.ReadUntilAsync(
            intent => intent is SwitchPanelIntent);
        await session.Views.ReadUntilAsync(view => view.Revision == 3);
        timeProvider.Advance(policy.AttemptTimeout);
        var retryIntent = (SwitchPanelIntent)await session.Intents.ReadUntilAsync(
            intent =>
                intent is SwitchPanelIntent switchPanel &&
                switchPanel.ActionId != firstIntent.ActionId);

        timeProvider.Advance(TimeSpan.FromTicks(1));
        var confirmationTime = new MacroTimestamp(
            policy.AttemptTimeout + TimeSpan.FromTicks(1));
        session.PublishSnapshot(
            CreateSnapshot(
                sequence: 2,
                confirmationTime,
                ClientPanel.TemuairSpells));
        var confirmed = await session.Views.ReadUntilAsync(
            view =>
                view.PanelTransition?.Status ==
                PanelTransitionStatus.Succeeded);

        Assert.Multiple(() =>
        {
            Assert.That(firstIntent.ActionId.Value, Is.EqualTo(1));
            Assert.That(retryIntent.ActionId.Value, Is.EqualTo(2));
            Assert.That(confirmed.PendingActionId, Is.Null);
            Assert.That(confirmed.PanelTransition?.Attempt, Is.EqualTo(2));
        });
    }

    [Test]
    public async Task ShouldSequenceAndConfirmStaffSwitchThroughSession()
    {
        var timeProvider = new ManualTimeProvider();
        await using var session = new MacroSession(
            new MacroEngine(),
            new MacroClock(timeProvider));
        var staff = new StaffCandidate(
            "staff",
            CharacterClass.Wizard,
            requiredLevel: 0,
            requiredAbilityLevel: 0,
            castLines: 1);
        var inventory = new InventorySnapshot(
        [
            new InventoryItemSnapshot(7, staff.Name)
        ]);
        var character = new CharacterSnapshot(
            CharacterClass.Wizard,
            level: 99,
            abilityLevel: 99);

        session.PublishSnapshot(
            CreateSnapshot(
                sequence: 1,
                MacroTimestamp.Zero,
                ClientPanel.Stats,
                character,
                inventory,
                new EquipmentSnapshot(weaponName: null)));
        await session.Views.ReadUntilAsync(view => view.Revision == 1);
        await session.SendCommandAsync(new StartMacroCommand());
        await session.Views.ReadUntilAsync(view => view.Revision == 2);
        await session.SendCommandAsync(
            new RequestStaffSwitchCommand(
                baseCastLines: 4,
                [staff],
                new StaffEquipmentPolicy(
                    TimeSpan.FromMilliseconds(100),
                    maximumAttempts: 2)));

        var panelIntent = (SwitchPanelIntent)await session.Intents.ReadUntilAsync(
            intent => intent is SwitchPanelIntent);
        timeProvider.Advance(TimeSpan.FromTicks(1));
        var panelTime = new MacroTimestamp(TimeSpan.FromTicks(1));
        session.PublishSnapshot(
            CreateSnapshot(
                sequence: 2,
                panelTime,
                ClientPanel.Inventory,
                character,
                inventory,
                new EquipmentSnapshot(weaponName: null)));
        var weaponIntent =
            (EquipWeaponIntent)await session.Intents.ReadUntilAsync(
                intent => intent is EquipWeaponIntent);

        timeProvider.Advance(TimeSpan.FromTicks(1));
        var equippedTime = new MacroTimestamp(TimeSpan.FromTicks(2));
        session.PublishSnapshot(
            CreateSnapshot(
                sequence: 3,
                equippedTime,
                ClientPanel.Inventory,
                character,
                InventorySnapshot.Empty,
                new EquipmentSnapshot(staff.Name)));
        var confirmed = await session.Views.ReadUntilAsync(
            view => view.StaffSwitch?.Status == StaffSwitchStatus.Succeeded);

        Assert.Multiple(() =>
        {
            Assert.That(panelIntent.ActionId.Value, Is.EqualTo(1));
            Assert.That(weaponIntent.ActionId.Value, Is.EqualTo(2));
            Assert.That(weaponIntent.StaffName, Is.EqualTo(staff.Name));
            Assert.That(weaponIntent.InventorySlot, Is.EqualTo(7));
            Assert.That(confirmed.PendingActionId, Is.Null);
        });
    }

    [Test]
    public async Task ShouldAwaitShutdownAndRejectNewInputAfterDisposal()
    {
        var timeProvider = new ManualTimeProvider();
        var session = new MacroSession(
            new MacroEngine(),
            new MacroClock(timeProvider));

        await session.DisposeAsync();
        while (session.Views.TryRead(out _))
        {
        }

        await session.Views.Completion;
        await session.Intents.Completion;

        Assert.Multiple(() =>
        {
            Assert.That(session.Completion.IsCompletedSuccessfully, Is.True);
            Assert.That(
                async () =>
                    await session.SendCommandAsync(new StartMacroCommand()),
                Throws.TypeOf<ObjectDisposedException>());
            Assert.That(
                () => session.PublishSnapshot(
                    CreateSnapshot(1, MacroTimestamp.Zero)),
                Throws.TypeOf<ObjectDisposedException>());
        });
    }

    [Test]
    public async Task ShouldCompleteIntentsWithoutDispatchingAfterShutdown()
    {
        var timeProvider = new ManualTimeProvider();
        var session = new MacroSession(
            new IntentMacroEngine(),
            new MacroClock(timeProvider));

        await session.SendCommandAsync(new TestMacroCommand(1));
        var intent = await session.Intents.ReadUntilAsync(
            value => value is TestClientActionIntent);
        await session.DisposeAsync();

        Assert.Multiple(() =>
        {
            Assert.That(intent, Is.TypeOf<TestClientActionIntent>());
            Assert.That(
                ((ClientActionIntent)intent).ActionId.Value,
                Is.EqualTo(1));
            Assert.That(session.Intents.TryRead(out _), Is.False);
            Assert.That(session.Intents.Completion.IsCompletedSuccessfully, Is.True);
        });
    }

    private static ClientSnapshot CreateSnapshot(
        long sequence,
        MacroTimestamp capturedAt,
        ClientPanel activePanel = ClientPanel.Unknown,
        CharacterSnapshot? character = null,
        InventorySnapshot? inventory = null,
        EquipmentSnapshot? equipment = null) =>
        new(
            new SnapshotSequence(sequence),
            capturedAt,
            capturedAt,
            new ClientIdentity("session-client", "test"),
            SnapshotQuality.Complete,
            ClientPresence.InWorld,
            activePanel,
            character,
            inventory,
            equipment);
}
