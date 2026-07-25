using System.Collections.Immutable;
using System.Threading.Channels;
using SleepHunter.Interop.Hosting;
using SleepHunter.Interop.Input;
using SleepHunter.Interop.Memory;
using SleepHunter.Interop.Snapshots;
using SleepHunter.Models;
using SleepHunter.Runtime.Characters;
using SleepHunter.Runtime.Commands;
using SleepHunter.Runtime.Engine;
using SleepHunter.Runtime.Snapshots;
using SleepHunter.Runtime.Time;
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
                Does.StartWith("Runtime snapshot 1"));
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
            succeeded: false));
        await WaitUntilAsync(() => !item.UsesRuntimeSnapshot);

        Assert.Multiple(() =>
        {
            Assert.That(item.Name, Is.EqualTo("Legacy"));
            Assert.That(item.CurrentHealth, Is.EqualTo(100));
            Assert.That(item.MapName, Is.EqualTo("Legacy Map"));
            Assert.That(
                item.RuntimeStatus,
                Does.StartWith("Runtime capture failed"));
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

    private static SnapshotCaptureObservation CreateCapture(
        ClientIdentity client,
        long sequenceValue,
        bool succeeded,
        ClientPresence presence = ClientPresence.InWorld)
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
        var failure = SnapshotCaptureFailure.MappingReadFailed;
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
                SnapshotQuality.Partial,
                new SnapshotCaptureError(
                    SnapshotSection.Presence,
                    failure,
                    "The scripted capture failed."),
                metrics);
        var statistics = new SnapshotCaptureStatistics(
            windowCapacity: 1,
            succeededCount: succeeded ? 1 : 0,
            failedCount: succeeded ? 0 : 1,
            new SnapshotDurationStatistics(
                sampleCount: 1,
                TimeSpan.Zero,
                TimeSpan.Zero,
                TimeSpan.Zero),
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
        private readonly TaskCompletionSource completion =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly Channel<MacroViewSnapshot> views =
            Channel.CreateUnbounded<MacroViewSnapshot>();

        public RecordingRuntimeHost(int processId)
        {
            Client = new ClientIdentity(
                $"process:{processId}",
                "USDA 7.41");
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
            ValueTask.CompletedTask;

        public bool PublishClientRoster(ClientRosterSnapshot snapshot) =>
            false;

        public ValueTask DisposeAsync()
        {
            captures.Writer.TryComplete();
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
