using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Threading.Channels;
using System.Windows.Input;
using SleepHunter.Interop.Hosting;
using SleepHunter.Interop.Input;
using SleepHunter.Interop.Mappings;
using SleepHunter.Interop.Memory;
using SleepHunter.Interop.Snapshots;
using SleepHunter.Models;
using SleepHunter.Services.Hotkeys;
using SleepHunter.ViewModels.Editing;
using SleepHunter.Persistence.Configuration;
using SleepHunter.Runtime.Automation;
using SleepHunter.Runtime.Automation.Flowering;
using SleepHunter.Runtime.Automation.Panels;
using SleepHunter.Runtime.Automation.Skills;
using SleepHunter.Runtime.Automation.Spells;
using SleepHunter.Runtime.Automation.Staves;
using SleepHunter.Runtime.Characters;
using SleepHunter.Runtime.Commands;
using SleepHunter.Runtime.Engine;
using SleepHunter.Runtime.Events;
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
    public async Task ShouldFilterClientsAndPublishRuntimeLoginTransitions()
    {
        var player = CreatePlayer();
        var host = new RecordingRuntimeHost(
            player.Process.ProcessId);
        await using var runtime = new ClientRuntimeViewModel(
            host,
            new InlineUiDispatcher());
        using var clients = new ClientListViewModel();
        var transitions = new List<bool>();
        clients.ClientLoginStateChanged +=
            (_, args) => transitions.Add(args.IsLoggedIn);

        clients.Refresh(
            [player],
            _ => runtime,
            ClientSortOrder.LaunchOrder,
            showAllClients: false);

        Assert.Multiple(() =>
        {
            Assert.That(clients.Clients, Is.Empty);
            Assert.That(clients.AllClients, Has.Count.EqualTo(1));
            Assert.That(clients.HasLoggedInClients, Is.False);
        });

        host.PublishCapture(
            CreateCapture(
                host.Client,
                sequenceValue: 1,
                succeeded: true));
        await WaitUntilAsync(
            () => clients.Clients.Count == 1);

        Assert.Multiple(() =>
        {
            Assert.That(
                clients.Clients.Single().Name,
                Is.EqualTo("Runtime"));
            Assert.That(clients.HasLoggedInClients, Is.True);
            Assert.That(transitions, Is.EqualTo(new[] { true }));
        });

        host.PublishCapture(
            CreateCapture(
                host.Client,
                sequenceValue: 2,
                succeeded: true,
                presence: ClientPresence.LoggedOut));
        await WaitUntilAsync(
            () => clients.Clients.Count == 0);

        Assert.Multiple(() =>
        {
            Assert.That(clients.AllClients, Has.Count.EqualTo(1));
            Assert.That(clients.HasLoggedInClients, Is.False);
            Assert.That(
                transitions,
                Is.EqualTo(new[] { true, false }));
        });
    }

    [Test]
    public async Task ShouldDeriveWindowTitleFromTheSelectedRuntimeClient()
    {
        var session = CreatePlayer();
        var host = new RecordingRuntimeHost(
            session.Process.ProcessId);
        await using var runtime = new ClientRuntimeViewModel(
            host,
            new InlineUiDispatcher());
        using var clients = new ClientListViewModel();

        clients.Refresh([session], _ => runtime);
        clients.SelectedClient = clients.Clients.Single();
        Assert.That(
            clients.WindowTitle,
            Is.EqualTo("SleepHunter"));

        host.PublishCapture(
            CreateCapture(
                host.Client,
                sequenceValue: 1,
                succeeded: true));
        await WaitUntilAsync(
            () => clients.WindowTitle ==
                  "SleepHunter - Runtime");

        host.PublishCapture(
            CreateCapture(
                host.Client,
                sequenceValue: 2,
                succeeded: true,
                presence: ClientPresence.LoggedOut));
        await WaitUntilAsync(
            () => clients.WindowTitle == "SleepHunter");
    }

    [Test]
    public async Task ShouldSortClientsUsingRuntimeSnapshotVitals()
    {
        var firstPlayer = CreatePlayer(processId: 1001);
        var secondPlayer = CreatePlayer(processId: 1002);
        var firstHost = new RecordingRuntimeHost(
            firstPlayer.Process.ProcessId);
        var secondHost = new RecordingRuntimeHost(
            secondPlayer.Process.ProcessId);
        await using var firstRuntime =
            new ClientRuntimeViewModel(
                firstHost,
                new InlineUiDispatcher());
        await using var secondRuntime =
            new ClientRuntimeViewModel(
                secondHost,
                new InlineUiDispatcher());
        using var clients = new ClientListViewModel();

        firstHost.PublishCapture(
            CreateCapture(
                firstHost.Client,
                sequenceValue: 1,
                succeeded: true,
                maximumHealth: 400));
        secondHost.PublishCapture(
            CreateCapture(
                secondHost.Client,
                sequenceValue: 1,
                succeeded: true,
                maximumHealth: 800));
        await WaitUntilAsync(
            () => firstRuntime.CaptureSequence?.Value == 1 &&
                  secondRuntime.CaptureSequence?.Value == 1);

        clients.Refresh(
            [firstPlayer, secondPlayer],
            processId => processId ==
                    firstPlayer.Process.ProcessId
                ? firstRuntime
                : secondRuntime,
            ClientSortOrder.HighestHealth,
            showAllClients: false);

        Assert.That(
            clients.Clients.Select(
                client => client.Process.ProcessId),
            Is.EqualTo(new[] { 1002, 1001 }));
    }

    [Test]
    public async Task ShouldMarshalPlayerPresentationObservationsToTheUiDispatcher()
    {
        var player = CreatePlayer();
        var dispatcher = new QueuedUiDispatcher();
        using var item = new ClientListItemViewModel(
            player,
            macroConfiguration: null,
            runtime: null,
            configurationMapper: null,
            setupFactory: null,
            getSettings: null,
            uiDispatcher: dispatcher);
        var observedThread = new TaskCompletionSource<int>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        item.PropertyChanged +=
            (_, args) =>
            {
                if (args.PropertyName ==
                    nameof(ClientListItemViewModel.Name))
                {
                    observedThread.TrySetResult(
                        Environment.CurrentManagedThreadId);
                }
            };

        await Task.Run(() => player.Name = "Worker Update");

        Assert.Multiple(() =>
        {
            Assert.That(dispatcher.PendingCount, Is.EqualTo(1));
            Assert.That(observedThread.Task.IsCompleted, Is.False);
        });

        var uiThread = Environment.CurrentManagedThreadId;
        dispatcher.ExecuteNext();

        Assert.That(
            await observedThread.Task.WaitAsync(
                TimeSpan.FromSeconds(1)),
            Is.EqualTo(uiThread));
    }

    [Test]
    public void ShouldOwnSelectionAndClearItWithTheRemovedClient()
    {
        var player = CreatePlayer();
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

        clients.Refresh(Array.Empty<ClientSession>(), _ => null);

        Assert.That(clients.SelectedClient, Is.Null);
    }

    [Test]
    public void ShouldFindTheHotkeyOwnerWithoutChangingSelection()
    {
        var player = CreatePlayer();
        using var clients = new ClientListViewModel();
        player.Hotkey = new Hotkey(
            ModifierKeys.Control | ModifierKeys.Shift,
            Key.F8);
        clients.Refresh([player], _ => null);

        var owner = clients.FindByHotkey(
            new Hotkey(
                ModifierKeys.Control | ModifierKeys.Shift,
                Key.F8));

        Assert.Multiple(() =>
        {
            Assert.That(owner, Is.SameAs(clients.Clients.Single()));
            Assert.That(owner?.Session, Is.SameAs(player));
            Assert.That(clients.SelectedClient, Is.Null);
            Assert.That(
                clients.FindByHotkey(
                    new Hotkey(ModifierKeys.Control, Key.F8)),
                Is.Null);
        });
    }

    [Test]
    public void ShouldProjectTheAssignedHotkeyGlyph()
    {
        var player = CreatePlayer();
        using var item =
            new ClientListItemViewModel(
                player,
                runtime: null);
        var changedProperties = new List<string?>();
        item.PropertyChanged +=
            (_, args) =>
                changedProperties.Add(
                    args.PropertyName);

        player.Hotkey = new Hotkey(
            ModifierKeys.Control,
            Key.D1);

        Assert.Multiple(() =>
        {
            Assert.That(item.HasHotkey, Is.True);
            Assert.That(
                item.HotkeyString,
                Is.EqualTo("Control+1"));
            Assert.That(
                changedProperties,
                Does.Contain(
                    nameof(
                        ClientListItemViewModel
                            .HasHotkey)));
            Assert.That(
                changedProperties,
                Does.Contain(
                    nameof(
                        ClientListItemViewModel
                            .HotkeyString)));
        });
    }

    [Test]
    public async Task ShouldUseRuntimeObservationsAndRetainProjectedStateAfterFailure()
    {
        var player = CreatePlayer();
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
            Assert.That(item.Name, Is.EqualTo("Presentation"));
            Assert.That(item.CurrentHealth, Is.Zero);
            Assert.That(item.MapName, Is.Null);
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
            Assert.That(item.RuntimeStatus, Is.EqualTo("Healthy"));
            Assert.That(item.IsRuntimeStatusError, Is.False);
            Assert.That(
                item.RuntimeDetailsText,
                Does.Contain("Timing average: 0 ms"));
            Assert.That(
                item.RuntimeDetailsText,
                Does.Contain("Timing minimum: 0 ms"));
            Assert.That(
                item.RuntimeDetailsText,
                Does.Contain("Timing maximum: 0 ms"));
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
            Assert.That(item.Name, Is.EqualTo("Runtime"));
            Assert.That(item.CurrentHealth, Is.Zero);
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
            Assert.That(item.Name, Is.EqualTo("Runtime"));
            Assert.That(item.CurrentHealth, Is.Zero);
            Assert.That(item.MapName, Is.Null);
            Assert.That(item.HasLastErrorStatus, Is.True);
            Assert.That(
                item.LastErrorStatus,
                Is.EqualTo(
                    "Capture MappingReadFailed: " +
                    "The scripted capture failed."));
            Assert.That(
                item.RuntimeStatus,
                Does.StartWith("MappingReadFailed:"));
            Assert.That(item.IsRuntimeStatusError, Is.True);
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
    public async Task ShouldRefreshManaAfterAZeroManaObservation()
    {
        var player = CreatePlayer();
        var host = new RecordingRuntimeHost(player.Process.ProcessId);
        await using var runtime = new ClientRuntimeViewModel(
            host,
            new InlineUiDispatcher());
        using var item = new ClientListItemViewModel(player, runtime);
        var manaNotifications = 0;
        item.PropertyChanged +=
            (_, args) =>
            {
                if (args.PropertyName ==
                    nameof(ClientListItemViewModel.CurrentMana))
                {
                    manaNotifications++;
                }
            };

        host.PublishCapture(
            CreateCapture(
                host.Client,
                sequenceValue: 1,
                succeeded: true,
                currentMana: 0));
        await WaitUntilAsync(
            () => runtime.CaptureSequence?.Value == 1);
        host.PublishCapture(
            CreateCapture(
                host.Client,
                sequenceValue: 2,
                succeeded: true,
                currentMana: 550));
        await WaitUntilAsync(
            () => item.CurrentMana == 550);

        Assert.Multiple(() =>
        {
            Assert.That(item.CurrentMana, Is.EqualTo(550));
            Assert.That(item.MaximumMana, Is.EqualTo(600));
            Assert.That(manaNotifications, Is.GreaterThanOrEqualTo(2));
            Assert.That(
                item.RuntimeDetailsText,
                Does.Contain("MP 550/600"));
        });
    }

    [Test]
    public async Task ShouldRefreshHealthAfterAZeroHealthObservation()
    {
        var player = CreatePlayer();
        var host = new RecordingRuntimeHost(player.Process.ProcessId);
        await using var runtime = new ClientRuntimeViewModel(
            host,
            new InlineUiDispatcher());
        using var item = new ClientListItemViewModel(player, runtime);
        var healthNotifications = 0;
        item.PropertyChanged +=
            (_, args) =>
            {
                if (args.PropertyName ==
                    nameof(ClientListItemViewModel.CurrentHealth))
                {
                    healthNotifications++;
                }
            };

        host.PublishCapture(
            CreateCapture(
                host.Client,
                sequenceValue: 1,
                succeeded: true,
                currentHealth: 0));
        await WaitUntilAsync(
            () => runtime.CaptureSequence?.Value == 1);
        host.PublishCapture(
            CreateCapture(
                host.Client,
                sequenceValue: 2,
                succeeded: true,
                currentHealth: 350));
        await WaitUntilAsync(
            () => item.CurrentHealth == 350);

        Assert.Multiple(() =>
        {
            Assert.That(item.CurrentHealth, Is.EqualTo(350));
            Assert.That(item.MaximumHealth, Is.EqualTo(400));
            Assert.That(
                healthNotifications,
                Is.GreaterThanOrEqualTo(2));
            Assert.That(
                item.RuntimeDetailsText,
                Does.Contain("HP 350/400"));
        });
    }

    [Test]
    public async Task ShouldKeepObservingVitalsAfterSpellProjectionFails()
    {
        var player = CreatePlayer();
        var configuration = new ClientMacroConfiguration(player);
        var queuedSpell = new SpellQueueItemViewModel
        {
            Name = "projected spell"
        };
        configuration.AddToSpellQueue(queuedSpell);
        var throwOnce = true;
        queuedSpell.PropertyChanged +=
            (_, _) =>
            {
                if (!throwOnce)
                    return;

                throwOnce = false;
                throw new InvalidOperationException(
                    "Scripted projection failure.");
            };
        var host = new RecordingRuntimeHost(player.Process.ProcessId);
        await using var runtime = new ClientRuntimeViewModel(
            host,
            new InlineUiDispatcher());
        using var item = new ClientListItemViewModel(
            player,
            configuration,
            runtime,
            configurationMapper: null,
            setupFactory: null,
            getSettings: null,
            uiDispatcher: new InlineUiDispatcher());
        var spellbook = new SpellbookSnapshot(
            [
                new SpellSnapshot(
                    queuedSpell.Name,
                    slot: 1,
                    currentLevel: 20,
                    maximumLevel: 100,
                    castLines: 1,
                    manaCost: 0,
                    cooldown: TimeSpan.Zero)
            ]);

        host.PublishCapture(
            CreateCapture(
                host.Client,
                sequenceValue: 1,
                succeeded: true,
                spellbook: spellbook,
                currentHealth: 0,
                currentMana: 0));
        await WaitUntilAsync(
            () => item.LastObservationError is not null);
        host.PublishCapture(
            CreateCapture(
                host.Client,
                sequenceValue: 2,
                succeeded: true,
                spellbook: spellbook,
                currentHealth: 350,
                currentMana: 550));
        await WaitUntilAsync(
            () => runtime.CaptureSequence?.Value == 2 &&
                  item.LastObservationError is null);

        Assert.Multiple(() =>
        {
            Assert.That(item.CurrentHealth, Is.EqualTo(350));
            Assert.That(item.CurrentMana, Is.EqualTo(550));
            Assert.That(queuedSpell.CurrentLevel, Is.EqualTo(20));
            Assert.That(queuedSpell.MaximumLevel, Is.EqualTo(100));
            Assert.That(item.RuntimeStatus, Is.EqualTo("Healthy"));
        });
    }

    [Test]
    public async Task ShouldProjectConfiguredSkillStateIntoTheRuntimeSkillbook()
    {
        var player = CreatePlayer();
        var configuration = new ClientMacroConfiguration(player);
        var host = new RecordingRuntimeHost(
            player.Process.ProcessId);
        await using var runtime = new ClientRuntimeViewModel(
            host,
            new InlineUiDispatcher());
        using var item = new ClientListItemViewModel(
            player,
            configuration,
            runtime,
            configurationMapper: null,
            setupFactory: null,
            getSettings: null,
            uiDispatcher: new InlineUiDispatcher());
        var skillbook = new SkillbookSnapshot(
        [
            new SkillSnapshot(
                "Assail",
                slot: 1,
                currentLevel: 10,
                maximumLevel: 100,
                manaCost: 0,
                cooldown: TimeSpan.Zero,
                isAssail: true,
                icon: 12)
        ]);

        host.PublishCapture(CreateCapture(
            host.Client,
            sequenceValue: 1,
            succeeded: true,
            skillbook: skillbook));
        await WaitUntilAsync(
            () => item.Skillbook.GetSkill("Assail") is not null);

        Assert.That(
            item.Skillbook.GetSkill("Assail")!.IsActive,
            Is.False);

        configuration.ToggleSkill("Assail");
        await WaitUntilAsync(
            () =>
                item.Skillbook.GetSkill("Assail")?.IsActive ==
                true);

        Assert.That(
            configuration.Skills.Single().Name,
            Is.EqualTo("Assail"));
    }

    [Test]
    public async Task ShouldTickAndResetFlowerDueTimeFromRuntimeSchedule()
    {
        var player = CreatePlayer();
        var configuration = new ClientMacroConfiguration(player);
        var flower = new FlowerQueueItemViewModel
        {
            Interval = TimeSpan.FromSeconds(10)
        };
        configuration.AddToFlowerQueue(flower);
        var host = new RecordingRuntimeHost(player.Process.ProcessId);
        await using var runtime = new ClientRuntimeViewModel(
            host,
            new InlineUiDispatcher());
        using var item = new ClientListItemViewModel(
            player,
            configuration,
            runtime,
            configurationMapper: null,
            setupFactory: null,
            getSettings: null,
            uiDispatcher: new InlineUiDispatcher());
        var entry = new FlowerQueueEntry(
            new FlowerQueueEntryId(flower.Id),
            SleepHunter.Runtime.Automation.Spells.SpellTarget.Self,
            flower.Interval);
        var queue = FlowerQueueState.Empty.Add(entry);
        var schedules = FlowerScheduleState.Empty.RecordUse(
            entry,
            MacroTimestamp.Zero);

        host.PublishView(CreateView(
            revision: 1,
            MacroLifecycle.Running,
            queue,
            schedules));
        host.PublishCapture(CreateCapture(
            host.Client,
            sequenceValue: 1,
            succeeded: true,
            capturedAt: TimeSpan.FromSeconds(3)));
        await WaitUntilAsync(
            () => flower.RemainingTime == TimeSpan.FromSeconds(7));

        host.PublishCapture(CreateCapture(
            host.Client,
            sequenceValue: 2,
            succeeded: true,
            capturedAt: TimeSpan.FromSeconds(7)));
        await WaitUntilAsync(
            () => flower.RemainingTime == TimeSpan.FromSeconds(3));

        host.PublishCapture(CreateCapture(
            host.Client,
            sequenceValue: 3,
            succeeded: true,
            capturedAt: TimeSpan.FromSeconds(12)));
        await WaitUntilAsync(() => flower.IsReady);

        host.PublishView(CreateView(
            revision: 2,
            MacroLifecycle.Stopped,
            queue,
            schedules));
        await WaitUntilAsync(
            () => flower.RemainingTime == TimeSpan.FromSeconds(10));

        Assert.Multiple(() =>
        {
            Assert.That(
                flower.RemainingTime,
                Is.EqualTo(TimeSpan.FromSeconds(10)));
            Assert.That(flower.IsReady, Is.False);
            Assert.That(item.LastObservationError, Is.Null);
        });
    }

    [Test]
    public async Task ShouldKeepLastCoherentLocationDuringMapTransition()
    {
        var player = CreatePlayer();
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
                Is.EqualTo("Waiting for coherent map location"));
            Assert.That(item.IsRuntimeStatusError, Is.False);
        });
    }

    [Test]
    public async Task ShouldReuseRefreshAndDisposeClientListItems()
    {
        var player = CreatePlayer();
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
            Array.Empty<ClientSession>(),
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
        var player = CreatePlayer();
        var macroConfiguration = new ClientMacroConfiguration(player)
        {
            SpellQueueRotation = SpellRotationMode.RoundRobin
        };
        macroConfiguration.AddToSpellQueue(
            new SpellQueueItemViewModel
            {
                Name = "test spell",
                Target = new SleepHunter.ViewModels.Editing.SpellTargetViewModel
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
            new ClientMacroConfigurationMapper(),
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
            () => item.ToggleMacroCommand.CanExecute(null) &&
                  macroConfiguration.QueuedSpells.Single().CurrentLevel == 5);

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

        var apply = await host.ReadCommandAsync();
        var start = await host.ReadCommandAsync();
        Assert.Multiple(() =>
        {
            Assert.That(
                apply,
                Is.TypeOf<ApplyAutomationSetupCommand>());
            var setup = (ApplyAutomationSetupCommand)apply;
            Assert.That(
                setup.Queues
                    .SpellQueue.Entries.Single().Name,
                Is.EqualTo("test spell"));
            Assert.That(
                setup.Configuration.SpellsEnabled,
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
            Assert.That(item.IsMacroEditingEnabled, Is.True);
            Assert.That(item.CanReplaceMacroConfiguration, Is.False);
            Assert.That(
                item.ToggleMacroCommand.CanExecute(null),
                Is.True);
            Assert.That(
                item.MacroEditor.ClearSpellsCommand.CanExecute(null),
                Is.True);
        });

        macroConfiguration.ToggleSkill("Assail");
        var skillUpdate = await host.ReadCommandAsync();
        Assert.Multiple(() =>
        {
            Assert.That(
                skillUpdate,
                Is.TypeOf<ApplyAutomationSetupCommand>());
            var setup = (ApplyAutomationSetupCommand)skillUpdate;
            Assert.That(
                setup.Queues.SkillQueue.Entries.Single().Name,
                Is.EqualTo("Assail"));
            Assert.That(setup.Configuration.SkillsEnabled, Is.True);
        });

        var secondSpell = new SpellQueueItemViewModel
        {
            Name = "second spell",
            Target = new SleepHunter.ViewModels.Editing.SpellTargetViewModel
            {
                Mode = SpellTargetMode.Self
            }
        };
        macroConfiguration.AddToSpellQueue(secondSpell);
        var added = (ApplyAutomationSetupCommand)
            await host.ReadCommandAsync();
        Assert.That(
            added.Queues.SpellQueue.Entries.Select(entry => entry.Name),
            Is.EqualTo(new[] { "test spell", "second spell" }));

        macroConfiguration.MoveSpell(
            macroConfiguration.QueuedSpells[0],
            secondSpell);
        var moved = (ApplyAutomationSetupCommand)
            await host.ReadCommandAsync();
        Assert.That(
            moved.Queues.SpellQueue.Entries.Select(entry => entry.Name),
            Is.EqualTo(new[] { "second spell", "test spell" }));

        macroConfiguration.SpellQueueRotation =
            SpellRotationMode.None;
        var rotationChanged = (ApplyAutomationSetupCommand)
            await host.ReadCommandAsync();
        Assert.Multiple(() =>
        {
            Assert.That(
                rotationChanged.Queues.SpellQueue.Rotation,
                Is.EqualTo(SpellQueueRotation.Priority));
            Assert.That(
                rotationChanged.Queues.SpellQueue.Entries.Select(
                    entry => entry.Name),
                Is.EqualTo(
                    new[] { "second spell", "test spell" }));
        });

        macroConfiguration.RemoveFromSpellQueue(
            macroConfiguration.QueuedSpells[1]);
        var removed = (ApplyAutomationSetupCommand)
            await host.ReadCommandAsync();
        Assert.That(
            removed.Queues.SpellQueue.Entries.Single().Name,
            Is.EqualTo("second spell"));

        macroConfiguration.UpdateSpell(
            secondSpell,
            new SpellQueueItemViewModel
            {
                Id = secondSpell.Id,
                Name = "updated spell",
                Target = new SleepHunter.ViewModels.Editing.SpellTargetViewModel
                {
                    Mode = SpellTargetMode.Self
                }
            });
        var updated = (ApplyAutomationSetupCommand)
            await host.ReadCommandAsync();
        Assert.That(
            updated.Queues.SpellQueue.Entries.Single().Name,
            Is.EqualTo("updated spell"));

        macroConfiguration.FlowerAlternateCharacters = true;
        var automaticFlowering = (ApplyAutomationSetupCommand)
            await host.ReadCommandAsync();
        Assert.Multiple(() =>
        {
            Assert.That(
                automaticFlowering.Configuration.FloweringEnabled,
                Is.True);
            Assert.That(
                automaticFlowering.Queues.FlowerQueue.Entries,
                Is.Empty);
        });

        var liveFlower = new FlowerQueueItemViewModel
        {
            Target = new SleepHunter.ViewModels.Editing.SpellTargetViewModel
            {
                Mode = SpellTargetMode.Self
            },
            Interval = TimeSpan.FromSeconds(10)
        };
        macroConfiguration.AddToFlowerQueue(liveFlower);
        var flowerAdded = (ApplyAutomationSetupCommand)
            await host.ReadCommandAsync();
        Assert.That(
            flowerAdded.Queues.FlowerQueue.Entries.Single().Id.Value,
            Is.EqualTo(liveFlower.Id));

        item.MacroEditor.SelectedFlower = liveFlower;
        item.MacroEditor.RemoveSelectedFlowerCommand.Execute(null);
        var flowerRemoved = (ApplyAutomationSetupCommand)
            await host.ReadCommandAsync();
        Assert.Multiple(() =>
        {
            Assert.That(
                flowerRemoved.Configuration.FloweringEnabled,
                Is.True);
            Assert.That(
                flowerRemoved.Queues.FlowerQueue.Entries,
                Is.Empty);
            Assert.That(item.IsMacroRunning, Is.True);
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

        var resumeApply = await host.ReadCommandAsync();
        var resume = await host.ReadCommandAsync();
        Assert.Multiple(() =>
        {
            Assert.That(
                resumeApply,
                Is.TypeOf<ApplyAutomationSetupCommand>());
            Assert.That(
                resume,
                Is.TypeOf<ResumeMacroCommand>());
        });
    }

    [Test]
    public async Task ShouldProjectTheActiveRuntimeCastByQueueIdentifier()
    {
        var player = CreatePlayer();
        var configuration = new ClientMacroConfiguration(player);
        var first = new SpellQueueItemViewModel
        {
            Id = 41,
            Name = "duplicate spell"
        };
        var second = new SpellQueueItemViewModel
        {
            Id = 42,
            Name = first.Name
        };
        configuration.AddToSpellQueue(first);
        configuration.AddToSpellQueue(second);
        var host = new RecordingRuntimeHost(player.Process.ProcessId);
        await using var runtime = new ClientRuntimeViewModel(
            host,
            new InlineUiDispatcher());
        using var item = new ClientListItemViewModel(
            player,
            configuration,
            runtime,
            configurationMapper: null,
            setupFactory: null,
            getSettings: null,
            uiDispatcher: new InlineUiDispatcher());
        var spellbook = new SpellbookSnapshot(
        [
            new SpellSnapshot(
                first.Name,
                slot: 1,
                currentLevel: 1,
                maximumLevel: 100,
                castLines: 4,
                manaCost: 0,
                cooldown: TimeSpan.Zero)
        ]);
        var capture = CreateCapture(
            host.Client,
            sequenceValue: 1,
            succeeded: true,
            activePanel: ClientPanel.TemuairSpells,
            spellbook: spellbook);
        host.PublishCapture(capture);
        await WaitUntilAsync(
            () => runtime.CaptureSequence?.Value == 1);

        var snapshot = capture.Result.Snapshot ??
            throw new InvalidOperationException(
                "The scripted successful capture has no snapshot.");
        var castingView = CreateCastingView(
            snapshot,
            first,
            second);
        host.PublishView(castingView);
        await WaitUntilAsync(() => first.IsActive);

        Assert.Multiple(() =>
        {
            Assert.That(first.IsActive, Is.True);
            Assert.That(second.IsActive, Is.False);
        });

        host.PublishView(CreateView(
            castingView.Revision + 1,
            MacroLifecycle.Running));
        await WaitUntilAsync(() => !first.IsActive);

        Assert.Multiple(() =>
        {
            Assert.That(first.IsActive, Is.False);
            Assert.That(second.IsActive, Is.False);
        });
    }

    [Test]
    public async Task ShouldRetainAutomationErrorsForRuntimeDetails()
    {
        var player = CreatePlayer();
        var macroConfiguration =
            new ClientMacroConfiguration(player);
        var host = new RecordingRuntimeHost(
            player.Process.ProcessId);
        await using var runtime = new ClientRuntimeViewModel(
            host,
            new InlineUiDispatcher());
        using var item = new ClientListItemViewModel(
            player,
            macroConfiguration,
            runtime,
            new ClientMacroConfigurationMapper(),
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
                item.RuntimeStatus,
                Is.EqualTo(
                    "Automation error: The scripted setup failed."));
            Assert.That(item.IsRuntimeStatusError, Is.True);
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
    public async Task ShouldReportRuntimeHostFailuresInStatusDetails()
    {
        var player = CreatePlayer();
        var host = new RecordingRuntimeHost(
            player.Process.ProcessId);
        await using var runtime = new ClientRuntimeViewModel(
            host,
            new InlineUiDispatcher());
        using var item = new ClientListItemViewModel(
            player,
            runtime);
        host.PublishView(CreateView(
            revision: 0,
            MacroLifecycle.Running));
        host.PublishCapture(CreateCapture(
            host.Client,
            sequenceValue: 1,
            succeeded: true));
        await WaitUntilAsync(() => item.RuntimeStatus == "Healthy");
        var failure = new InvalidOperationException(
            "Flower spell casting requires flower action state.");

        host.Fail(failure);
        await WaitUntilAsync(
            () => item.RuntimeStatus.StartsWith(
                "Runtime stopped:",
                StringComparison.Ordinal));
        item.IsRuntimeDetailsOpen = true;

        Assert.Multiple(() =>
        {
            Assert.That(item.IsRuntimeStatusError, Is.True);
            Assert.That(item.IsMacroRunning, Is.False);
            Assert.That(item.MacroToggleLabel, Is.EqualTo("Start Macro"));
            Assert.That(
                item.RuntimeStatus,
                Is.EqualTo(
                    "Runtime stopped: " +
                    "Flower spell casting requires flower action state."));
            Assert.That(
                item.RuntimeDetailsSnapshot,
                Does.Contain("Runtime available: No"));
            Assert.That(
                item.RuntimeDetailsSnapshot,
                Does.Contain("Macro lifecycle: Stopped"));
            Assert.That(
                item.RuntimeDetailsSnapshot,
                Does.Contain("Macro stop reason: RuntimeFailure"));
            Assert.That(
                item.RuntimeDetailsSnapshot,
                Does.Contain("Runtime failure"));
            Assert.That(
                item.RuntimeDetailsSnapshot,
                Does.Contain(
                    "Exception: System.InvalidOperationException"));
            Assert.That(
                item.RuntimeDetailsSnapshot,
                Does.Contain(
                    "Message: Flower spell casting requires " +
                    "flower action state."));
        });
    }

    [Test]
    public async Task ShouldStopAllActiveRuntimesThroughToolkitCommand()
    {
        var player = CreatePlayer();
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

    private static ClientSession CreatePlayer(
        int? processId = null)
    {
        var process = new ClientProcess
        {
            ProcessId = processId ?? Environment.ProcessId,
            WindowHandle = new nint(1),
            WindowTitle = "Presentation Window"
        };
        var player = new ClientSession(process)
        {
            Name = "Presentation"
        };
        return player;
    }

    private static MacroViewSnapshot CreateView(
        long revision,
        MacroLifecycle lifecycle,
        FlowerQueueState? flowerQueue = null,
        FlowerScheduleState? flowerSchedules = null) =>
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
            flowerQueue ?? FlowerQueueState.Empty,
            flowerSchedules ?? FlowerScheduleState.Empty,
            ClientRosterSequence: null,
            Flower: null,
            TargetRotationState.Empty,
            TargetRotationState.Empty,
            LastActionIssue: null);

    private static MacroViewSnapshot CreateCastingView(
        ClientSnapshot snapshot,
        params SpellQueueItemViewModel[] queuedSpells)
    {
        var engine = new MacroEngine();
        var currentTime = snapshot.CaptureCompletedAt;
        var state = engine.Decide(
            MacroState.Initial,
            new ClientSnapshotObserved(snapshot),
            currentTime).State;
        foreach (var queuedSpell in queuedSpells)
        {
            state = engine.Decide(
                state,
                new MacroCommandReceived(
                    new AddSpellQueueEntryCommand(
                        new SpellQueueEntry(
                            new SpellQueueEntryId(queuedSpell.Id),
                            queuedSpell.Name))),
                currentTime).State;
        }

        state = engine.Decide(
            state,
            new MacroCommandReceived(new StartMacroCommand()),
            currentTime).State;
        var cast = engine.Decide(
            state,
            new MacroCommandReceived(
                new CastNextSpellCommand(
                    new SpellExecutionPolicy(
                        allowStaffSwitching: false))),
            currentTime);
        return cast.PublishedView ??
            throw new InvalidOperationException(
                "The scripted spell cast did not publish a runtime view.");
    }

    private static SnapshotCaptureObservation CreateCapture(
        ClientIdentity client,
        long sequenceValue,
        bool succeeded,
        ClientPresence presence = ClientPresence.InWorld,
        ClientPanel activePanel = ClientPanel.Unknown,
        SpellbookSnapshot? spellbook = null,
        SkillbookSnapshot? skillbook = null,
        int currentHealth = 300,
        int currentMana = 500,
        int maximumHealth = 400,
        int maximumMana = 600,
        SnapshotCaptureFailure failure =
            SnapshotCaptureFailure.MappingReadFailed,
        SnapshotSection failureSection = SnapshotSection.Presence,
        string? variableKey = null,
        MappedMemoryReadError? readError = null,
        TimeSpan? capturedAt = null)
    {
        var sequence = new SnapshotSequence(sequenceValue);
        var timestamp = new MacroTimestamp(
            capturedAt ?? TimeSpan.FromTicks(sequenceValue));
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
                    activePanel,
                    character: presence == ClientPresence.InWorld
                        ? new CharacterSnapshot(
                            CharacterClass.Wizard,
                            level: 99,
                            abilityLevel: 50,
                            name: "Runtime")
                        : null,
                    vitals: presence == ClientPresence.InWorld
                        ? new VitalsSnapshot(
                            currentHealth,
                            maximumHealth,
                            currentMana,
                            maximumMana)
                        : null,
                    spellbook: spellbook,
                    skillbook: skillbook,
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

        public void Fail(Exception exception)
        {
            captures.Writer.TryComplete(exception);
            commands.Writer.TryComplete(exception);
            views.Writer.TryComplete(exception);
            completion.TrySetException(exception);
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

    private sealed class QueuedUiDispatcher : IUiDispatcher
    {
        private readonly ConcurrentQueue<Invocation> invocations =
            new();

        public int PendingCount => invocations.Count;

        public ValueTask InvokeAsync(
            Action action,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var completion = new TaskCompletionSource<bool>(
                TaskCreationOptions.RunContinuationsAsynchronously);
            invocations.Enqueue(
                new Invocation(action, completion));
            return new ValueTask(completion.Task);
        }

        public void ExecuteNext()
        {
            if (!invocations.TryDequeue(out var invocation))
            {
                throw new InvalidOperationException(
                    "No UI invocation is pending.");
            }

            try
            {
                invocation.Action();
                invocation.Completion.SetResult(true);
            }
            catch (Exception exception)
            {
                invocation.Completion.SetException(exception);
                throw;
            }
        }

        private sealed record Invocation(
            Action Action,
            TaskCompletionSource<bool> Completion);
    }
}
