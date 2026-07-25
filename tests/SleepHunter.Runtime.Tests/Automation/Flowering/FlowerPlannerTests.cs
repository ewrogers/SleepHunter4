using SleepHunter.Runtime.Automation.Flowering;
using SleepHunter.Runtime.Automation.Spells;
using SleepHunter.Runtime.Snapshots;
using SleepHunter.Runtime.Time;

namespace SleepHunter.Runtime.Tests.Automation.Flowering;

public sealed class FlowerPlannerTests
{
    private static readonly ClientIdentity SourceClient =
        new("source");

    private static readonly MapLocationSnapshot SourceLocation =
        new(
            mapNumber: 1,
            mapName: "test map",
            x: 50,
            y: 50);

    [Test]
    public void ShouldStartIntervalOnFirstObservationAndResetAfterUse()
    {
        var interval = TimeSpan.FromSeconds(1);
        var entry = new FlowerQueueEntry(
            new FlowerQueueEntryId(1),
            SpellTarget.Self,
            interval);
        var queue = FlowerQueueState.Empty.Add(entry);

        var initial = Plan(
            queue,
            FlowerScheduleState.Empty,
            MacroTimestamp.Zero);
        var readyAt = new MacroTimestamp(interval);
        var ready = Plan(queue, initial.Schedules, readyAt);
        var resetSchedules = ready.Schedules.RecordUse(entry, readyAt);
        var reset = Plan(queue, resetSchedules, readyAt);

        Assert.Multiple(() =>
        {
            Assert.That(initial.Status, Is.EqualTo(FlowerPlanStatus.Waiting));
            Assert.That(
                initial.Readiness.Single().Status,
                Is.EqualTo(FlowerReadinessStatus.WaitingForInterval));
            Assert.That(
                initial.Schedules.GetReadyAt(entry.Id),
                Is.EqualTo(readyAt));
            Assert.That(ready.HasSelection, Is.True);
            Assert.That(
                ready.SelectedEntry,
                Is.EqualTo(entry));
            Assert.That(reset.HasSelection, Is.False);
            Assert.That(
                reset.Schedules.GetReadyAt(entry.Id),
                Is.EqualTo(readyAt.Add(interval)));
        });
    }

    [Test]
    public void ShouldTreatManaAndIntervalConditionsAsAlternatives()
    {
        var entry = new FlowerQueueEntry(
            new FlowerQueueEntryId(1),
            SpellTarget.Character("alt"),
            interval: TimeSpan.FromHours(1),
            manaThreshold: 500);
        var queue = FlowerQueueState.Empty.Add(entry);
        var alt = Client(
            "alt",
            currentMana: 499,
            location: NearbyLocation());

        var ready = Plan(
            queue,
            FlowerScheduleState.Empty,
            MacroTimestamp.Zero,
            [alt]);
        var waiting = Plan(
            queue,
            FlowerScheduleState.Empty,
            MacroTimestamp.Zero,
            [
                Client(
                    "alt",
                    currentMana: 500,
                    location: NearbyLocation())
            ]);

        Assert.Multiple(() =>
        {
            Assert.That(ready.HasSelection, Is.True);
            Assert.That(
                ready.SelectedClient?.CharacterName,
                Is.EqualTo("alt"));
            Assert.That(waiting.HasSelection, Is.False);
            Assert.That(
                waiting.Readiness.Single().Status,
                Is.EqualTo(FlowerReadinessStatus.WaitingForCondition));
        });
    }

    [Test]
    public void ShouldResynchronizeChangedAndRemovedIntervals()
    {
        var original = new FlowerQueueEntry(
            new FlowerQueueEntryId(1),
            SpellTarget.Self,
            interval: TimeSpan.FromSeconds(1));
        var queue = FlowerQueueState.Empty.Add(original);
        var initial = Plan(
            queue,
            FlowerScheduleState.Empty,
            MacroTimestamp.Zero);
        var changedAt =
            new MacroTimestamp(TimeSpan.FromMilliseconds(500));
        var changed = new FlowerQueueEntry(
            original.Id,
            original.Target,
            interval: TimeSpan.FromSeconds(2));
        var updated = Plan(
            queue.Update(changed),
            initial.Schedules,
            changedAt);
        var removed = Plan(
            queue.Remove(original.Id),
            updated.Schedules,
            changedAt);

        Assert.Multiple(() =>
        {
            Assert.That(
                updated.Schedules.GetReadyAt(original.Id),
                Is.EqualTo(
                    changedAt.Add(TimeSpan.FromSeconds(2))));
            Assert.That(removed.Schedules.Schedules, Is.Empty);
        });
    }

    [Test]
    public void ShouldRejectUnavailableAndOutOfRangeCharacterTargets()
    {
        var missing = Entry(1, SpellTarget.Character("missing"));
        var far = Entry(2, SpellTarget.Character("far"));
        var queue = FlowerQueueState.Empty
            .Add(missing)
            .Add(far);
        var farClient = Client(
            "far",
            currentMana: 0,
            location: new MapLocationSnapshot(
                1,
                "test map",
                x: 61,
                y: 50));

        var plan = Plan(
            queue,
            FlowerScheduleState.Empty,
            MacroTimestamp.Zero,
            [farClient]);

        Assert.Multiple(() =>
        {
            Assert.That(plan.HasSelection, Is.False);
            Assert.That(
                plan.Readiness.Single(
                    entry => entry.Entry.Id == missing.Id).Status,
                Is.EqualTo(FlowerReadinessStatus.TargetUnavailable));
            Assert.That(
                plan.Readiness.Single(
                    entry => entry.Entry.Id == far.Id).Status,
                Is.EqualTo(FlowerReadinessStatus.OutOfRange));
        });
    }

    [Test]
    public void ShouldBoundTileTargetsAndAllowScreenTargets()
    {
        var absolute = Entry(1, SpellTarget.AbsoluteTile(61, 50));
        var relative = Entry(2, SpellTarget.RelativeTile(0, 11));
        var screen = Entry(3, SpellTarget.ScreenPoint(400, 300));
        var queue = FlowerQueueState.Empty
            .Add(absolute)
            .Add(relative)
            .Add(screen);

        var plan = Plan(
            queue,
            FlowerScheduleState.Empty,
            MacroTimestamp.Zero);

        Assert.Multiple(() =>
        {
            Assert.That(plan.SelectedEntry, Is.EqualTo(screen));
            Assert.That(
                plan.Readiness.Single(
                    entry => entry.Entry.Id == absolute.Id).Status,
                Is.EqualTo(FlowerReadinessStatus.OutOfRange));
            Assert.That(
                plan.Readiness.Single(
                    entry => entry.Entry.Id == relative.Id).Status,
                Is.EqualTo(FlowerReadinessStatus.OutOfRange));
        });
    }

    [Test]
    public void ShouldRotateFairlyAcrossPrioritizedCharacterEntries()
    {
        var tile = Entry(1, SpellTarget.Self);
        var firstAlt = Entry(2, SpellTarget.Character("first"));
        var secondAlt = Entry(3, SpellTarget.Character("second"));
        var queue = FlowerQueueState.Empty
            .Add(tile)
            .Add(firstAlt)
            .Add(secondAlt);
        var clients = new[]
        {
            Client("first", 0, NearbyLocation()),
            Client("second", 0, NearbyLocation())
        };
        var policy = new FlowerTargetPolicy(
            prioritizeAlternateCharacters: true);

        var first = Plan(
            queue,
            FlowerScheduleState.Empty,
            MacroTimestamp.Zero,
            clients,
            policy);
        var second = Plan(
            first.Queue,
            first.Schedules,
            MacroTimestamp.Zero,
            clients,
            policy);

        Assert.Multiple(() =>
        {
            Assert.That(first.SelectedEntry, Is.EqualTo(firstAlt));
            Assert.That(second.SelectedEntry, Is.EqualTo(secondAlt));
            Assert.That(first.Queue.Cursor, Is.EqualTo(2));
            Assert.That(second.Queue.Cursor, Is.Zero);
        });
    }

    [Test]
    public void ShouldApplyAutomaticCharacterPriorityAroundQueue()
    {
        var queued = Entry(1, SpellTarget.Self);
        var queue = FlowerQueueState.Empty.Add(queued);
        var waitingAlt = Client(
            "waiting",
            currentMana: 0,
            location: NearbyLocation(),
            isWaitingForMana: true);
        var prioritize = new FlowerTargetPolicy(
            autoFlowerWaitingCharacters: true,
            prioritizeAlternateCharacters: true);
        var queueFirst = new FlowerTargetPolicy(
            autoFlowerWaitingCharacters: true,
            prioritizeAlternateCharacters: false);

        var automatic = Plan(
            queue,
            FlowerScheduleState.Empty,
            MacroTimestamp.Zero,
            [waitingAlt],
            prioritize);
        var configured = Plan(
            queue,
            FlowerScheduleState.Empty,
            MacroTimestamp.Zero,
            [waitingAlt],
            queueFirst);

        Assert.Multiple(() =>
        {
            Assert.That(
                automatic.SelectionKind,
                Is.EqualTo(FlowerSelectionKind.WaitingCharacter));
            Assert.That(
                automatic.SelectedTarget,
                Is.EqualTo(SpellTarget.Character("waiting")));
            Assert.That(
                configured.SelectionKind,
                Is.EqualTo(FlowerSelectionKind.QueueEntry));
            Assert.That(configured.SelectedEntry, Is.EqualTo(queued));
        });
    }

    [Test]
    public void ShouldChooseLongestWaitingEligibleCharacterDeterministically()
    {
        var neverFlowered = Client(
            "never",
            currentMana: 0,
            location: NearbyLocation(),
            isWaitingForMana: true);
        var older = Client(
            "older",
            currentMana: 0,
            location: NearbyLocation(),
            isWaitingForMana: true,
            lastFloweredAt: new MacroTimestamp(TimeSpan.FromSeconds(1)));
        var newer = Client(
            "newer",
            currentMana: 0,
            location: NearbyLocation(),
            isWaitingForMana: true,
            lastFloweredAt: new MacroTimestamp(TimeSpan.FromSeconds(2)));
        var stopped = Client(
            "stopped",
            currentMana: 0,
            location: NearbyLocation(),
            isMacroRunning: false,
            isWaitingForMana: true);
        var far = Client(
            "far",
            currentMana: 0,
            location: new MapLocationSnapshot(
                2,
                "other map",
                50,
                50),
            isWaitingForMana: true);
        var source = Client(
            "source character",
            currentMana: 0,
            location: SourceLocation,
            isWaitingForMana: true,
            client: new ClientIdentity(SourceClient.InstanceId));
        var policy = new FlowerTargetPolicy(
            autoFlowerWaitingCharacters: true);

        var plan = Plan(
            FlowerQueueState.Empty,
            FlowerScheduleState.Empty,
            MacroTimestamp.Zero,
            [newer, stopped, far, source, older, neverFlowered],
            policy);

        Assert.Multiple(() =>
        {
            Assert.That(
                plan.SelectedClient,
                Is.EqualTo(neverFlowered));
            Assert.That(
                plan.ClientReadiness.Single(
                    entry => entry.Client == stopped).Status,
                Is.EqualTo(FlowerClientReadinessStatus.MacroStopped));
            Assert.That(
                plan.ClientReadiness.Single(
                    entry => entry.Client == far).Status,
                Is.EqualTo(FlowerClientReadinessStatus.OutOfRange));
            Assert.That(
                plan.ClientReadiness.Single(
                    entry => entry.Client == source).Status,
                Is.EqualTo(FlowerClientReadinessStatus.SourceClient));
        });
    }

    [Test]
    public void ShouldRequireUniqueClientAndCharacterObservations()
    {
        var first = Client(
            "same",
            currentMana: 0,
            location: NearbyLocation());
        var duplicateName = Client(
            "SAME",
            currentMana: 0,
            location: NearbyLocation(),
            client: new ClientIdentity("other"));

        Assert.That(
            () => new FlowerPlanningRequest(
                SourceClient,
                SourceLocation,
                FlowerQueueState.Empty,
                FlowerScheduleState.Empty,
                [first, duplicateName],
                MacroTimestamp.Zero),
            Throws.ArgumentException);
    }

    private static FlowerPlan Plan(
        FlowerQueueState queue,
        FlowerScheduleState schedules,
        MacroTimestamp currentTime,
        IEnumerable<ClientRosterEntry>? clients = null,
        FlowerTargetPolicy? policy = null) =>
        FlowerPlanner.Plan(
            new FlowerPlanningRequest(
                SourceClient,
                SourceLocation,
                queue,
                schedules,
                clients ?? [],
                currentTime,
                policy));

    private static FlowerQueueEntry Entry(
        long id,
        SpellTarget target) =>
        new(
            new FlowerQueueEntryId(id),
            target,
            interval: TimeSpan.Zero);

    private static ClientRosterEntry Client(
        string characterName,
        int currentMana,
        MapLocationSnapshot? location,
        bool isMacroRunning = true,
        bool isWaitingForMana = false,
        MacroTimestamp? lastFloweredAt = null,
        ClientIdentity? client = null) =>
        new(
            client ?? new ClientIdentity(characterName),
            characterName,
            ClientPresence.InWorld,
            isMacroRunning,
            isWaitingForMana,
            location,
            new VitalsSnapshot(
                currentHealth: 100,
                maximumHealth: 100,
                currentMana,
                maximumMana: 1000),
            lastFloweredAt);

    private static MapLocationSnapshot NearbyLocation() =>
        new(
            mapNumber: 1,
            mapName: "test map",
            x: 55,
            y: 55);
}
