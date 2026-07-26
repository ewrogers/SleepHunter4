using System.Collections.Immutable;
using System.Threading.Channels;
using SleepHunter.Interop.Hosting;
using SleepHunter.Interop.Input;
using SleepHunter.Interop.Mappings;
using SleepHunter.Interop.Memory;
using SleepHunter.Interop.Snapshots;
using SleepHunter.Macro;
using SleepHunter.Models;
using SleepHunter.Persistence.Configuration;
using SleepHunter.Runtime.Automation;
using SleepHunter.Runtime.Automation.Flowering;
using SleepHunter.Runtime.Automation.Skills;
using SleepHunter.Runtime.Automation.Spells;
using SleepHunter.Runtime.Automation.Staves;
using SleepHunter.Runtime.Characters;
using SleepHunter.Runtime.Commands;
using SleepHunter.Runtime.Engine;
using SleepHunter.Runtime.Snapshots;
using SleepHunter.Runtime.Time;
using SleepHunter.Services.Configuration;
using SleepHunter.Services.Runtime;
using SleepHunter.Settings;
using SleepHunter.ViewModels;

namespace SleepHunter.Tests.ViewModels;

public sealed class ClientListViewModelTests
{
    [Test]
    public void ShouldOwnSelectionAndClearItWithTheRemovedClient()
    {
        using var player = CreatePlayer();
        using var clients = new ClientListViewModel();

        clients.Refresh([player], _ => null);
        var item = clients.Clients.Single();
        clients.SelectedClient = item;
        clients.Refresh([player], _ => null);

        Assert.Multiple(() =>
        {
            Assert.That(clients.SelectedClient, Is.SameAs(item));
            Assert.That(
                typeof(ClientListItemViewModel)
                    .GetProperty(nameof(ClientListItemViewModel.Runtime))
                    ?.SetMethod
                    ?.IsPrivate,
                Is.True);
        });

        clients.Refresh(Array.Empty<Player>(), _ => null);

        Assert.That(clients.SelectedClient, Is.Null);
    }

    [Test]
    public async Task ShouldUseRuntimeObservationsAndFallBackAfterFailure()
    {
        using var player = CreatePlayer();
        var host = new RecordingRuntimeHost(player.Process.ProcessId);
        await using var runtime = new ClientRuntimeViewModel(
            host,
            new InlineUiDispatcher());
        using var item = new ClientListItemViewModel(
            player,
            runtime);

        Assert.Multiple(() =>
        {
            Assert.That(item.HasRuntime, Is.True);
            Assert.That(item.UsesRuntimeSnapshot, Is.False);
            Assert.That(item.Name, Is.EqualTo("Legacy"));
            Assert.That(item.CurrentHealth, Is.EqualTo(100));
            Assert.That(item.MapName, Is.EqualTo("Legacy Map"));
        });

        host.PublishCapture(CreateCapture(
            host.Client,
            sequenceValue: 1,
            succeeded: true));
        await WaitUntilAsync(() => item.UsesRuntimeSnapshot);

        Assert.Multiple(() =>
        {
            Assert.That(item.IsLoggedIn, Is.True);
            Assert.That(item.Name, Is.EqualTo("Runtime"));
            Assert.That(item.CurrentHealth, Is.EqualTo(300));
            Assert.That(item.MaximumHealth, Is.EqualTo(400));
            Assert.That(item.CurrentMana, Is.EqualTo(500));
            Assert.That(item.MaximumMana, Is.EqualTo(600));
            Assert.That(item.MapName, Is.EqualTo("Runtime Map"));
            Assert.That(item.MapX, Is.EqualTo(70));
            Assert.That(item.MapY, Is.EqualTo(80));
            Assert.That(
                item.RuntimeStatus,
                Does.StartWith(
                    "Runtime healthy, snapshot read avg 0.00 ms"));
            Assert.That(item.RuntimeStatus, Does.Contain("min 0.00 ms"));
            Assert.That(item.RuntimeStatus, Does.Contain("max 0.00 ms"));
            Assert.That(item.RuntimeStatus, Does.Not.Contain("snapshot 1"));
        });

        host.PublishCapture(CreateCapture(
            host.Client,
            sequenceValue: 2,
            succeeded: true,
            presence: ClientPresence.LoggedOut));
        await WaitUntilAsync(
            () => runtime.CaptureSequence?.Value == 2);

        Assert.Multiple(() =>
        {
            Assert.That(item.UsesRuntimeSnapshot, Is.True);
            Assert.That(item.IsLoggedIn, Is.False);
            Assert.That(item.Name, Is.EqualTo("Legacy"));
            Assert.That(item.CurrentHealth, Is.EqualTo(100));
        });

        host.PublishCapture(CreateCapture(
            host.Client,
            sequenceValue: 3,
            succeeded: false,
            failureSection: SnapshotSection.Location,
            variableKey: "MapName",
            readError: new MappedMemoryReadError(
                MappedMemoryReadFailure.ValueReadFailed,
                "MapName",
                ActualKind: MemoryValueKind.Text,
                MemoryError: new MemoryReadError(
                    MemoryReadFailure.InvalidEncoding,
                    new MemoryAddress(0x2FF6925C),
                    RequestedBytes: 32,
                    BytesRead: 32))));
        await WaitUntilAsync(
            () => !item.UsesRuntimeSnapshot &&
                  item.HasLastErrorStatus);
        item.IsRuntimeDetailsOpen = true;
        var frozenDetails = item.RuntimeDetailsSnapshot;

        Assert.Multiple(() =>
        {
            Assert.That(item.Name, Is.EqualTo("Legacy"));
            Assert.That(item.CurrentHealth, Is.EqualTo(100));
            Assert.That(item.MapName, Is.EqualTo("Legacy Map"));
            Assert.That(item.HasLastErrorStatus, Is.True);
            Assert.That(
                item.LastErrorStatus,
                Is.EqualTo(
                    "Capture MappingReadFailed: " +
                    "The scripted capture failed."));
            Assert.That(
                item.RuntimeStatus,
                Does.StartWith("Runtime capture failed"));
            Assert.That(
                frozenDetails,
                Does.Contain("Variable: MapName"));
            Assert.That(
                frozenDetails,
                Does.Contain("Mapped read failure: ValueReadFailed"));
            Assert.That(
                frozenDetails,
                Does.Contain("Memory failure: InvalidEncoding"));
            Assert.That(
                frozenDetails,
                Does.Contain("Address: 0x2FF6925C"));
            Assert.That(
                frozenDetails,
                Does.Contain("Requested bytes: 32"));
            Assert.That(
                frozenDetails,
                Does.Contain("Bytes read: 32"));
        });

        host.PublishCapture(CreateCapture(
            host.Client,
            sequenceValue: 4,
            succeeded: true));
        await WaitUntilAsync(
            () => runtime.CaptureSequence?.Value == 4);

        Assert.That(
            item.LastErrorStatus,
            Is.EqualTo(
                "Capture MappingReadFailed: " +
                "The scripted capture failed."));
        Assert.That(item.RuntimeDetailsSnapshot, Is.EqualTo(frozenDetails));
        item.IsRuntimeDetailsOpen = false;
        item.IsRuntimeDetailsOpen = true;
        Assert.That(
            item.RuntimeDetailsSnapshot,
            Does.Contain("Last retained capture error"));
    }

    [Test]
    public async Task ShouldKeepLastCoherentLocationDuringMapTransition()
    {
        using var player = CreatePlayer();
        var host = new RecordingRuntimeHost(player.Process.ProcessId);
        await using var runtime = new ClientRuntimeViewModel(
            host,
            new InlineUiDispatcher());
        using var item = new ClientListItemViewModel(
            player,
            runtime);

        host.PublishCapture(CreateCapture(
            host.Client,
            sequenceValue: 1,
            succeeded: true));
        await WaitUntilAsync(() => item.UsesRuntimeSnapshot);

        host.PublishCapture(CreateCapture(
            host.Client,
            sequenceValue: 2,
            succeeded: false,
            failure: SnapshotCaptureFailure.LocationTransition));
        await WaitUntilAsync(
            () => runtime.CaptureSequence?.Value == 2);

        Assert.Multiple(() =>
        {
            Assert.That(runtime.IsCaptureHealthy, Is.False);
            Assert.That(runtime.LatestSnapshot, Is.Null);
            Assert.That(item.UsesRuntimeSnapshot, Is.True);
            Assert.That(item.HasLastErrorStatus, Is.False);
            Assert.That(item.LastErrorStatus, Is.Null);
            Assert.That(item.Name, Is.EqualTo("Runtime"));
            Assert.That(item.MapName, Is.EqualTo("Runtime Map"));
            Assert.That(item.MapX, Is.EqualTo(70));
            Assert.That(item.MapY, Is.EqualTo(80));
            Assert.That(
                item.RuntimeStatus,
                Is.EqualTo(
                    "Runtime is waiting for a coherent map location"));
        });
    }

    [Test]
    public async Task ShouldReuseRefreshAndDisposeClientListItems()
    {
        using var player = CreatePlayer();
        var host = new RecordingRuntimeHost(player.Process.ProcessId);
        await using var runtime = new ClientRuntimeViewModel(
            host,
            new InlineUiDispatcher());
        using var clients = new ClientListViewModel();

        clients.Refresh(
            [player],
            _ => null);
        var original = clients.Clients.Single();
        clients.Refresh(
            [player],
            _ => runtime);

        Assert.Multiple(() =>
        {
            Assert.That(clients.Clients.Single(), Is.SameAs(original));
            Assert.That(original.Runtime, Is.SameAs(runtime));
            Assert.That(
                () => clients.Refresh(
                    [player, player],
                    _ => runtime),
                Throws.TypeOf<ArgumentException>());
        });

        var notificationCount = 0;
        original.PropertyChanged += (_, _) => notificationCount++;
        clients.Refresh(
            Array.Empty<Player>(),
            _ => null);
        var countAfterRemoval = notificationCount;
        player.Name = "Changed after removal";

        Assert.Multiple(() =>
        {
            Assert.That(clients.Clients, Is.Empty);
            Assert.That(notificationCount, Is.EqualTo(countAfterRemoval));
        });
    }

    [Test]
    public async Task ShouldConfigureRuntimeBeforeStartingOrResuming()
    {
        using var player = CreatePlayer();
        var macroConfiguration = new PlayerMacroConfiguration(player)
        {
            SpellQueueRotation = SpellRotationMode.RoundRobin
        };
        macroConfiguration.AddToSpellQueue(
            new SpellQueueItem
            {
                Name = "test spell",
                Target = new SleepHunter.Models.SpellTarget
                {
                    Mode = SpellTargetMode.Self
                }
            });
        var host = new RecordingRuntimeHost(
            player.Process.ProcessId);
        await using var runtime = new ClientRuntimeViewModel(
            host,
            new InlineUiDispatcher());
        using var item = new ClientListItemViewModel(
            player,
            macroConfiguration,
            runtime,
            new PlayerMacroConfigurationMapper(),
            new RuntimeAutomationSetupFactory(
                new EmptyStaffCandidateProvider()),
            () => new UserSettings
            {
                AllowStaffSwitching = false
            });

        host.PublishView(CreateView(
            revision: 0,
            MacroLifecycle.Stopped));
        host.PublishCapture(CreateCapture(
            host.Client,
            sequenceValue: 1,
            succeeded: true,
            spellbook: new SpellbookSnapshot(
            [
                new SpellSnapshot(
                    "test spell",
                    slot: 1,
                    currentLevel: 5,
                    maximumLevel: 10,
                    castLines: 1,
                    manaCost: 25,
                    cooldown: TimeSpan.FromSeconds(1),
                    isActionDelayed: true)
            ])));
        await WaitUntilAsync(
            () => item.ToggleMacroCommand.CanExecute(null));

        Assert.Multiple(() =>
        {
            Assert.That(item.MacroToggleLabel, Is.EqualTo("Start Macro"));
            Assert.That(item.MacroEditor, Is.Not.Null);
            Assert.That(
                item.MacroEditor.ClearSpellsCommand.CanExecute(null),
                Is.True);
            Assert.That(
                macroConfiguration.QueuedSpells.Single().CurrentLevel,
                Is.EqualTo(5));
            Assert.That(
                macroConfiguration.QueuedSpells.Single().MaximumLevel,
                Is.EqualTo(10));
            Assert.That(
                macroConfiguration.QueuedSpells.Single().IsOnCooldown,
                Is.True);
        });
        await item.ToggleMacroCommand.ExecuteAsync(null);

        var replace = await host.ReadCommandAsync();
        var configure = await host.ReadCommandAsync();
        var start = await host.ReadCommandAsync();
        Assert.Multiple(() =>
        {
            Assert.That(
                replace,
                Is.TypeOf<ReplaceQueuesCommand>());
            Assert.That(
                ((ReplaceQueuesCommand)replace)
                    .SpellQueue.Entries.Single().Name,
                Is.EqualTo("test spell"));
            Assert.That(
                configure,
                Is.TypeOf<ConfigureAutomationCommand>());
            Assert.That(
                ((ConfigureAutomationCommand)configure)
                    .Configuration.SpellsEnabled,
                Is.True);
            Assert.That(start, Is.TypeOf<StartMacroCommand>());
            Assert.That(item.LastAutomationError, Is.Null);
        });

        host.PublishView(CreateView(
            revision: 1,
            MacroLifecycle.Running));
        await WaitUntilAsync(() => item.IsMacroRunning);
        Assert.Multiple(() =>
        {
            Assert.That(item.MacroToggleLabel, Is.EqualTo("Pause Macro"));
            Assert.That(item.IsMacroEditingEnabled, Is.False);
            Assert.That(
                item.ToggleMacroCommand.CanExecute(null),
                Is.True);
            Assert.That(
                item.MacroEditor.ClearSpellsCommand.CanExecute(null),
                Is.False);
        });

        await item.ToggleMacroCommand.ExecuteAsync(null);
        Assert.That(
            await host.ReadCommandAsync(),
            Is.TypeOf<PauseMacroCommand>());

        host.PublishView(CreateView(
            revision: 2,
            MacroLifecycle.Paused));
        await WaitUntilAsync(() => item.IsMacroPaused);
        Assert.Multiple(() =>
        {
            Assert.That(
                item.MacroToggleLabel,
                Is.EqualTo("Resume Macro"));
            Assert.That(
                item.MacroEditor.ClearSpellsCommand.CanExecute(null),
                Is.True);
        });

        await item.ToggleMacroCommand.ExecuteAsync(null);

        var resumeReplace = await host.ReadCommandAsync();
        var resumeConfigure = await host.ReadCommandAsync();
        var resume = await host.ReadCommandAsync();
        Assert.Multiple(() =>
        {
            Assert.That(
                resumeReplace,
                Is.TypeOf<ReplaceQueuesCommand>());
            Assert.That(
                resumeConfigure,
                Is.TypeOf<ConfigureAutomationCommand>());
            Assert.That(
                resume,
                Is.TypeOf<ResumeMacroCommand>());
        });
    }

    [Test]
    public async Task ShouldRetainAutomationErrorsForTheStatusBar()
    {
        using var player = CreatePlayer();
        var macroConfiguration =
            new PlayerMacroConfiguration(player);
        var host = new RecordingRuntimeHost(
            player.Process.ProcessId);
        await using var runtime = new ClientRuntimeViewModel(
            host,
            new InlineUiDispatcher());
        using var item = new ClientListItemViewModel(
            player,
            macroConfiguration,
            runtime,
            new PlayerMacroConfigurationMapper(),
            new ThrowingAutomationSetupFactory(),
            () => new UserSettings());

        host.PublishView(CreateView(
            revision: 0,
            MacroLifecycle.Stopped));
        host.PublishCapture(CreateCapture(
            host.Client,
            sequenceValue: 1,
            succeeded: true));
        await WaitUntilAsync(
            () => item.ToggleMacroCommand.CanExecute(null));

        await item.ToggleMacroCommand.ExecuteAsync(null);
        item.IsRuntimeDetailsOpen = true;

        Assert.Multiple(() =>
        {
            Assert.That(
                item.LastAutomationError,
                Is.TypeOf<InvalidOperationException>());
            Assert.That(item.HasLastErrorStatus, Is.True);
            Assert.That(
                item.LastErrorStatus,
                Is.EqualTo(
                    "Automation: The scripted setup failed."));
            Assert.That(
                item.RuntimeDetailsSnapshot,
                Does.Contain(
                    "Exception: System.InvalidOperationException"));
            Assert.That(
                item.RuntimeDetailsSnapshot,
                Does.Contain("Message: The scripted setup failed."));
        });
    }

    [Test]
    public async Task ShouldStopAllActiveRuntimesThroughToolkitCommand()
    {
        using var player = CreatePlayer();
        var host = new RecordingRuntimeHost(
            player.Process.ProcessId);
        await using var runtime = new ClientRuntimeViewModel(
            host,
            new InlineUiDispatcher());
        using var clients = new ClientListViewModel();
        clients.Refresh([player], _ => runtime);
        host.PublishView(CreateView(
            revision: 0,
            MacroLifecycle.Running));
        await WaitUntilAsync(
            () => clients.StopAllMacrosCommand.CanExecute(null));

        await clients.StopAllMacrosCommand.ExecuteAsync(null);

        Assert.That(
            await host.ReadCommandAsync(),
            Is.TypeOf<StopMacroCommand>());
    }

    private static Player CreatePlayer()
    {
        var process = new ClientProcess
        {
            ProcessId = Environment.ProcessId,
            WindowHandle = new nint(1),
            WindowTitle = "Legacy Window"
        };
        var player = new Player(process)
        {
            Name = "Legacy",
            IsLoggedIn = true,
            Status = "Legacy status"
        };
        player.Stats.CurrentHealth = 100;
        player.Stats.MaximumHealth = 200;
        player.Stats.CurrentMana = 150;
        player.Stats.MaximumMana = 250;
        player.Location.MapName = "Legacy Map";
        player.Location.X = 10;
        player.Location.Y = 20;
        return player;
    }

    private static MacroViewSnapshot CreateView(
        long revision,
        MacroLifecycle lifecycle) =>
        new(
            revision,
            lifecycle,
            MacroStopReason.None,
            LatestSnapshotSequence: null,
            ClientPresence.Unknown,
            LastTransitionAt: null,
            PendingActionId: null,
            AutomationConfiguration.Disabled,
            SpellQueueState.Empty,
            PanelTransition: null,
            PanelPreservation: null,
            StaffSwitch: null,
            SpellCooldownState.Empty,
            SpellCast: null,
            SkillQueueState.Empty,
            SkillCooldownState.Empty,
            SkillUse: null,
            Disarm: null,
            Dialog: null,
            FlowerQueueState.Empty,
            FlowerScheduleState.Empty,
            ClientRosterSequence: null,
            Flower: null,
            TargetRotationState.Empty,
            TargetRotationState.Empty,
            LastActionIssue: null);

    private static SnapshotCaptureObservation CreateCapture(
        ClientIdentity client,
        long sequenceValue,
        bool succeeded,
        ClientPresence presence = ClientPresence.InWorld,
        SpellbookSnapshot? spellbook = null,
        SnapshotCaptureFailure failure =
            SnapshotCaptureFailure.MappingReadFailed,
        SnapshotSection failureSection = SnapshotSection.Presence,
        string? variableKey = null,
        MappedMemoryReadError? readError = null)
    {
        var sequence = new SnapshotSequence(sequenceValue);
        var timestamp = new MacroTimestamp(
            TimeSpan.FromTicks(sequenceValue));
        var reads = new MemoryReadMetrics(
            RequestCount: 1,
            TransportReadCount: 1,
            FailedReadCount: succeeded ? 0 : 1,
            RequestedBytes: 4,
            BytesRead: succeeded ? 4 : 0);
        var metrics = new SnapshotCaptureMetrics(
            sequence,
            timestamp,
            timestamp,
            ImmutableArray<SnapshotSectionMetrics>.Empty,
            reads);
        var quality = failure ==
            SnapshotCaptureFailure.LocationTransition
                ? SnapshotQuality.Incoherent
                : SnapshotQuality.Partial;
        var result = succeeded
            ? new SnapshotCaptureResult(
                new ClientSnapshot(
                    sequence,
                    timestamp,
                    timestamp,
                    client,
                    SnapshotQuality.Complete,
                    presence,
                    character: presence == ClientPresence.InWorld
                        ? new CharacterSnapshot(
                            CharacterClass.Wizard,
                            level: 99,
                            abilityLevel: 50,
                            name: "Runtime")
                        : null,
                    vitals: presence == ClientPresence.InWorld
                        ? new VitalsSnapshot(
                            currentHealth: 300,
                            maximumHealth: 400,
                            currentMana: 500,
                            maximumMana: 600)
                        : null,
                    spellbook: spellbook,
                    location: presence == ClientPresence.InWorld
                        ? new MapLocationSnapshot(
                            mapNumber: 1,
                            mapName: "Runtime Map",
                            x: 70,
                            y: 80)
                        : null),
                SnapshotQuality.Complete,
                error: null,
                metrics)
            : new SnapshotCaptureResult(
                snapshot: null,
                quality,
                new SnapshotCaptureError(
                    failure ==
                        SnapshotCaptureFailure.LocationTransition
                            ? SnapshotSection.Coherence
                            : failureSection,
                    failure,
                    "The scripted capture failed.",
                    variableKey,
                    readError),
                metrics);
        var statistics = new SnapshotCaptureStatistics(
            windowCapacity: 1,
            succeededCount: succeeded ? 1 : 0,
            failedCount: succeeded ? 0 : 1,
            new SnapshotDurationStatistics(
                sampleCount: 1,
                minimum: TimeSpan.Zero,
                average: TimeSpan.Zero,
                median: TimeSpan.Zero,
                percentile95: TimeSpan.Zero,
                maximum: TimeSpan.Zero),
            reads,
            succeeded
                ? ImmutableDictionary<SnapshotCaptureFailure, int>.Empty
                : ImmutableDictionary<SnapshotCaptureFailure, int>
                    .Empty
                    .Add(failure, 1),
            ImmutableArray<SnapshotSectionStatistics>.Empty);
        return new SnapshotCaptureObservation(
            result,
            statistics);
    }

    private static async Task WaitUntilAsync(Func<bool> predicate)
    {
        using var timeout = new CancellationTokenSource(
            TimeSpan.FromSeconds(5));
        try
        {
            while (!predicate())
            {
                await Task.Delay(
                    TimeSpan.FromMilliseconds(1),
                    timeout.Token);
            }
        }
        catch (OperationCanceledException)
            when (timeout.IsCancellationRequested)
        {
            throw new TimeoutException(
                "The expected client-list state was not observed.");
        }
    }

    private sealed class RecordingRuntimeHost : IClientRuntimeHost
    {
        private readonly Channel<SnapshotCaptureObservation> captures =
            Channel.CreateUnbounded<SnapshotCaptureObservation>();
        private readonly Channel<MacroCommand> commands =
            Channel.CreateUnbounded<MacroCommand>();
        private readonly TaskCompletionSource completion =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly Channel<MacroViewSnapshot> views =
            Channel.CreateUnbounded<MacroViewSnapshot>();

        public RecordingRuntimeHost(int processId)
        {
            Client = new ClientIdentity($"process:{processId}");
        }

        public ClientIdentity Client { get; }

        public ChannelReader<SnapshotCaptureObservation> Captures =>
            captures.Reader;

        public ChannelReader<MacroViewSnapshot> Views => views.Reader;

        public SnapshotCaptureResult? LatestCaptureResult => null;

        public ClientIntentIssueResult? LastIntentIssueResult => null;

        public SnapshotCaptureStatistics CaptureStatistics =>
            SnapshotCaptureStatistics.Empty(1);

        public Task Completion => completion.Task;

        public ValueTask SendCommandAsync(
            MacroCommand command,
            CancellationToken cancellationToken = default) =>
            commands.Writer.WriteAsync(command, cancellationToken);

        public bool PublishClientRoster(ClientRosterSnapshot snapshot) =>
            false;

        public ValueTask DisposeAsync()
        {
            captures.Writer.TryComplete();
            commands.Writer.TryComplete();
            views.Writer.TryComplete();
            completion.TrySetResult();
            return ValueTask.CompletedTask;
        }

        public void PublishCapture(SnapshotCaptureObservation capture)
        {
            if (!captures.Writer.TryWrite(capture))
            {
                throw new InvalidOperationException(
                    "The test capture channel is unavailable.");
            }
        }

        public void PublishView(MacroViewSnapshot view)
        {
            if (!views.Writer.TryWrite(view))
            {
                throw new InvalidOperationException(
                    "The test view channel is unavailable.");
            }
        }

        public async Task<MacroCommand> ReadCommandAsync()
        {
            using var timeout = new CancellationTokenSource(
                TimeSpan.FromSeconds(5));
            try
            {
                return await commands.Reader.ReadAsync(timeout.Token);
            }
            catch (OperationCanceledException)
                when (timeout.IsCancellationRequested)
            {
                throw new TimeoutException(
                    "The expected runtime command was not received.");
            }
        }
    }

    private sealed class EmptyStaffCandidateProvider :
        IRuntimeStaffCandidateProvider
    {
        public ImmutableArray<StaffCandidate> GetCandidates(
            string spellName,
            CharacterClass characterClass) =>
            ImmutableArray<StaffCandidate>.Empty;
    }

    private sealed class ThrowingAutomationSetupFactory :
        IRuntimeAutomationSetupFactory
    {
        public RuntimeAutomationSetup Create(
            MacroConfiguration configuration,
            UserSettings settings,
            CharacterClass characterClass) =>
            throw new InvalidOperationException(
                "The scripted setup failed.");
    }

    private sealed class InlineUiDispatcher : IUiDispatcher
    {
        public ValueTask InvokeAsync(
            Action action,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            action();
            return ValueTask.CompletedTask;
        }
    }
}
