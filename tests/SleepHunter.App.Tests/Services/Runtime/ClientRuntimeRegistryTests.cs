using System.Collections.Concurrent;
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
using SleepHunter.Services.Logging;
using SleepHunter.Services.Runtime;
using SleepHunter.ViewModels;

namespace SleepHunter.Tests.Services.Runtime;

public sealed class ClientRuntimeRegistryTests
{
    private static readonly string MappingPath = FindLayoutFile();

    [Test]
    public async Task ShouldAttachFindAndDetachAnActiveRuntime()
    {
        var factory = new RecordingRuntimeFactory();
        var logger = new RecordingLogger();
        await using var registry = CreateRegistry(factory, logger);
        var descriptor = Descriptor(processId: 1234);
        var interval = TimeSpan.FromMilliseconds(250);

        var attached = await registry.AttachAsync(descriptor, interval);
        var duplicate = await registry.AttachAsync(descriptor, interval);
        var wasFound = registry.TryFind(
            descriptor.ProcessId,
            out var viewModel);

        Assert.Multiple(() =>
        {
            Assert.That(attached, Is.True);
            Assert.That(duplicate, Is.False);
            Assert.That(wasFound, Is.True);
            Assert.That(viewModel.Client, Is.EqualTo(descriptor.Client));
            Assert.That(registry.Count, Is.EqualTo(1));
            Assert.That(factory.AttachCount, Is.EqualTo(1));
            Assert.That(factory.LastInterval, Is.EqualTo(interval));
            Assert.That(
                factory.LastSections,
                Is.EqualTo(SnapshotCaptureSections.All));
            Assert.That(
                factory.LastAbilityCatalog,
                Is.SameAs(AbilitySnapshotCatalog.Empty));
            Assert.That(logger.InfoMessages.Length, Is.EqualTo(1));
        });
        await viewModel.SendCommandAsync(new StartMacroCommand());

        var detached = await registry.DetachAsync(descriptor.ProcessId);

        Assert.Multiple(() =>
        {
            Assert.That(detached, Is.True);
            Assert.That(registry.Count, Is.Zero);
            Assert.That(factory.LastHost?.IsDisposed, Is.True);
            Assert.That(logger.InfoMessages.Length, Is.EqualTo(2));
            Assert.That(factory.LastHost?.CommandCount, Is.EqualTo(1));
        });
    }

    [Test]
    public async Task ShouldAttachAClientWithoutVersionRouting()
    {
        var factory = new RecordingRuntimeFactory();
        var logger = new RecordingLogger();
        await using var registry = CreateRegistry(factory, logger);

        var attached = await registry.AttachAsync(
            Descriptor(processId: 1234),
            TimeSpan.FromMilliseconds(200));

        Assert.Multiple(() =>
        {
            Assert.That(attached, Is.True);
            Assert.That(registry.Count, Is.EqualTo(1));
            Assert.That(factory.AttachCount, Is.EqualTo(1));
            Assert.That(logger.InfoMessages.Length, Is.EqualTo(1));
        });
    }

    [Test]
    public async Task ShouldProjectRuntimeSnapshotsIntoThePlayerModel()
    {
        var factory = new RecordingRuntimeFactory();
        await using var registry = CreateRegistry(
            factory,
            new RecordingLogger());
        var descriptor = Descriptor(processId: 1234);
        using var player = new Player(
            new ClientProcess
            {
                ProcessId = descriptor.ProcessId,
                WindowHandle = descriptor.WindowHandle
            });

        await registry.AttachAsync(
            descriptor,
            TimeSpan.FromMilliseconds(200));
        var wasBound = registry.BindPresentation(player);
        factory.LastHost!.PublishCapture(
            CreateCapture(
                descriptor.Client,
                sequenceValue: 1,
                characterName: "Projected"));
        await WaitUntilAsync(() => player.IsLoggedIn);

        Assert.Multiple(() =>
        {
            Assert.That(wasBound, Is.True);
            Assert.That(player.Name, Is.EqualTo("Projected"));
            Assert.That(player.Stats.CurrentHealth, Is.EqualTo(100));
            Assert.That(player.Location.MapName, Is.EqualTo("Test Map"));
            Assert.That(player.LastSnapshotSequence, Is.EqualTo(1));
        });
    }

    [Test]
    public async Task ShouldPublishChangedRostersUsingOneSharedClock()
    {
        var factory = new RecordingRuntimeFactory();
        await using var registry = CreateRegistry(
            factory,
            new RecordingLogger());
        var first = Descriptor(processId: 1234);
        var second = Descriptor(processId: 5678);

        await registry.AttachAsync(
            first,
            TimeSpan.FromMilliseconds(200));
        await registry.AttachAsync(
            second,
            TimeSpan.FromMilliseconds(200));
        var hosts = factory.Hosts;
        hosts[0].PublishCapture(
            CreateCapture(
                first.Client,
                sequenceValue: 1,
                characterName: "First"));
        await WaitUntilAsync(
            () => hosts.All(
                host => host.PublishedRosters
                    .Any(roster => roster.Clients.Length == 1)));
        var publicationCount =
            hosts[0].PublishedRosters.Length;

        hosts[0].PublishCapture(
            CreateCapture(
                first.Client,
                sequenceValue: 2,
                characterName: "First"));
        await WaitUntilAsync(
            () => registry.TryFind(
                      first.ProcessId,
                      out var runtime) &&
                  runtime.CaptureSequence?.Value == 2);

        var roster = hosts[1].PublishedRosters.Last();
        Assert.Multiple(() =>
        {
            Assert.That(
                factory.Clocks.Distinct().Count(),
                Is.EqualTo(1));
            Assert.That(
                roster.Clients.Single().CharacterName,
                Is.EqualTo("First"));
            Assert.That(
                roster.Clients.Single().Client,
                Is.EqualTo(first.Client));
            Assert.That(
                hosts[0].PublishedRosters.Length,
                Is.EqualTo(publicationCount));
        });
    }

    [Test]
    public async Task ShouldReportAttachmentFailuresWithoutAddingAClient()
    {
        var factory = new RecordingRuntimeFactory
        {
            AttachError = new InvalidOperationException(
                "The scripted attachment failed.")
        };
        var logger = new RecordingLogger();
        await using var registry = CreateRegistry(factory, logger);

        var attached = await registry.AttachAsync(
            Descriptor(processId: 1234),
            TimeSpan.FromMilliseconds(200));

        Assert.Multiple(() =>
        {
            Assert.That(attached, Is.False);
            Assert.That(registry.Count, Is.Zero);
            Assert.That(logger.ErrorMessages.Length, Is.EqualTo(1));
            Assert.That(logger.Exceptions.Length, Is.EqualTo(1));
        });
    }

    [Test]
    public async Task ShouldDisposeAllClientsAndRejectNewAttachments()
    {
        var factory = new RecordingRuntimeFactory();
        var logger = new RecordingLogger();
        var registry = CreateRegistry(factory, logger);
        await registry.AttachAsync(
            Descriptor(processId: 1234),
            TimeSpan.FromMilliseconds(200));
        await registry.AttachAsync(
            Descriptor(processId: 5678),
            TimeSpan.FromMilliseconds(200));

        await registry.DisposeAsync();

        Assert.Multiple(() =>
        {
            Assert.That(registry.Count, Is.Zero);
            Assert.That(
                factory.Hosts.All(host => host.IsDisposed),
                Is.True);
            Assert.That(
                async () => await registry.AttachAsync(
                    Descriptor(processId: 9012),
                    TimeSpan.FromMilliseconds(200)),
                Throws.TypeOf<ObjectDisposedException>());
        });
    }

    [Test]
    public void ShouldDisposeAHostWhenAttachmentIsCancelled()
    {
        using var cancellation = new CancellationTokenSource();
        var factory = new RecordingRuntimeFactory
        {
            BeforeHostCreated = cancellation.Cancel
        };
        var registry = CreateRegistry(
            factory,
            new RecordingLogger());

        Assert.That(
            async () => await registry.AttachAsync(
                Descriptor(processId: 1234),
                TimeSpan.FromMilliseconds(200),
                cancellation.Token),
            Throws.TypeOf<OperationCanceledException>());
        Assert.Multiple(() =>
        {
            Assert.That(registry.Count, Is.Zero);
            Assert.That(factory.LastHost?.IsDisposed, Is.True);
        });

        Assert.That(
            async () => await registry.DisposeAsync(),
            Throws.Nothing);
    }

    [Test]
    public async Task ShouldRejectAndDisposeAnAttachmentDuringShutdown()
    {
        using var attachStarted = new ManualResetEventSlim();
        using var continueAttach = new ManualResetEventSlim();
        var factory = new RecordingRuntimeFactory
        {
            BeforeHostCreated = () =>
            {
                attachStarted.Set();
                if (!continueAttach.Wait(TimeSpan.FromSeconds(5)))
                {
                    throw new TimeoutException(
                        "The scripted attachment was not released.");
                }
            }
        };
        var registry = CreateRegistry(
            factory,
            new RecordingLogger());
        var attachTask = Task.Run(
            async () => await registry.AttachAsync(
                Descriptor(processId: 1234),
                TimeSpan.FromMilliseconds(200)));

        try
        {
            Assert.That(
                attachStarted.Wait(TimeSpan.FromSeconds(5)),
                Is.True);
            await registry.DisposeAsync();
        }
        finally
        {
            continueAttach.Set();
        }

        Assert.That(
            async () => await attachTask,
            Throws.TypeOf<ObjectDisposedException>());
        Assert.Multiple(() =>
        {
            Assert.That(registry.Count, Is.Zero);
            Assert.That(factory.LastHost?.IsDisposed, Is.True);
        });
    }

    [Test]
    public void ShouldValidateClientRuntimeDescriptors()
    {
        var client = new ClientIdentity("process:1234");

        Assert.Multiple(() =>
        {
            Assert.That(
                () => _ = new ClientRuntimeDescriptor(
                    client,
                    processId: 0,
                    windowHandle: new nint(1)),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(
                () => _ = new ClientRuntimeDescriptor(
                    client,
                    processId: 1234,
                    windowHandle: nint.Zero),
                Throws.TypeOf<ArgumentOutOfRangeException>());
        });
    }

    private static ClientRuntimeRegistry CreateRegistry(
        IClientRuntimeFactory factory,
        ILogger logger) =>
        new(
            factory,
            logger,
            new InlineUiDispatcher(),
            MappingPath,
            TimeProvider.System,
            () => AbilitySnapshotCatalog.Empty);

    private static SnapshotCaptureObservation CreateCapture(
        ClientIdentity client,
        long sequenceValue,
        string characterName)
    {
        var sequence = new SnapshotSequence(sequenceValue);
        var timestamp = new MacroTimestamp(
            TimeSpan.FromTicks(sequenceValue));
        var reads = new MemoryReadMetrics(
            RequestCount: 1,
            TransportReadCount: 1,
            FailedReadCount: 0,
            RequestedBytes: 4,
            BytesRead: 4);
        var metrics = new SnapshotCaptureMetrics(
            sequence,
            timestamp,
            timestamp,
            ImmutableArray<SnapshotSectionMetrics>.Empty,
            reads);
        var snapshot = new ClientSnapshot(
            sequence,
            timestamp,
            timestamp,
            client,
            SnapshotQuality.Complete,
            ClientPresence.InWorld,
            character: new CharacterSnapshot(
                CharacterClass.Wizard,
                level: 99,
                abilityLevel: 50,
                name: characterName),
            vitals: new VitalsSnapshot(
                currentHealth: 100,
                maximumHealth: 200,
                currentMana: 300,
                maximumMana: 400),
            location: new MapLocationSnapshot(
                mapNumber: 1,
                mapName: "Test Map",
                x: 50,
                y: 60));
        var result = new SnapshotCaptureResult(
            snapshot,
            SnapshotQuality.Complete,
            error: null,
            metrics);
        var statistics = new SnapshotCaptureStatistics(
            windowCapacity: 1,
            succeededCount: 1,
            failedCount: 0,
            new SnapshotDurationStatistics(
                sampleCount: 1,
                minimum: TimeSpan.Zero,
                average: TimeSpan.Zero,
                median: TimeSpan.Zero,
                percentile95: TimeSpan.Zero,
                maximum: TimeSpan.Zero),
            reads,
            ImmutableDictionary<SnapshotCaptureFailure, int>.Empty,
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
                "The expected runtime registry state was not observed.");
        }
    }

    private static string FindLayoutFile()
    {
        var directory = new DirectoryInfo(
            TestContext.CurrentContext.TestDirectory);
        while (directory is not null)
        {
            var candidate = Path.Combine(
                directory.FullName,
                "data",
                "ClientLayout.xml");
            if (File.Exists(candidate))
                return candidate;

            directory = directory.Parent;
        }

        throw new FileNotFoundException(
            "Could not locate data/ClientLayout.xml from the test directory.");
    }

    private static ClientRuntimeDescriptor Descriptor(int processId) =>
        new(
            new ClientIdentity($"process:{processId}"),
            processId,
            new nint(processId));

    private sealed class RecordingRuntimeFactory : IClientRuntimeFactory
    {
        private readonly ConcurrentQueue<MacroClock> clocks = new();
        private readonly ConcurrentQueue<RecordingRuntimeHost> hosts = new();

        public Exception? AttachError { get; init; }

        public Action? BeforeHostCreated { get; init; }

        public int AttachCount { get; private set; }

        public RecordingRuntimeHost? LastHost { get; private set; }

        public TimeSpan LastInterval { get; private set; }

        public SnapshotCaptureSections LastSections { get; private set; }

        public AbilitySnapshotCatalog? LastAbilityCatalog
        {
            get;
            private set;
        }

        public MacroClock? LastClock { get; private set; }

        public MacroClock[] Clocks => clocks.ToArray();

        public RecordingRuntimeHost[] Hosts => hosts.ToArray();

        public IClientRuntimeHost Attach(
            Stream mappingStream,
            ClientIdentity client,
            int processId,
            nint windowHandle,
            SnapshotCaptureSchedule snapshotSchedule,
            MacroClock clock,
            AbilitySnapshotCatalog? abilityCatalog = null)
        {
            Assert.That(mappingStream.CanRead, Is.True);
            AttachCount++;
            LastInterval = snapshotSchedule.Interval;
            LastSections = snapshotSchedule.Sections;
            LastClock = clock;
            LastAbilityCatalog = abilityCatalog;
            clocks.Enqueue(clock);
            if (AttachError is not null)
                throw AttachError;

            BeforeHostCreated?.Invoke();
            var host = new RecordingRuntimeHost(client);
            LastHost = host;
            hosts.Enqueue(host);
            return host;
        }
    }

    private sealed class RecordingRuntimeHost : IClientRuntimeHost
    {
        private readonly Channel<SnapshotCaptureObservation> captures =
            Channel.CreateUnbounded<SnapshotCaptureObservation>();
        private readonly TaskCompletionSource completion =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly ConcurrentQueue<ClientRosterSnapshot>
            publishedRosters = new();
        private readonly Channel<MacroViewSnapshot> views =
            Channel.CreateUnbounded<MacroViewSnapshot>();

        public RecordingRuntimeHost(ClientIdentity client)
        {
            Client = client;
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

        public bool IsDisposed { get; private set; }

        public int CommandCount { get; private set; }

        public ClientRosterSnapshot[] PublishedRosters =>
            publishedRosters.ToArray();

        public ValueTask SendCommandAsync(
            MacroCommand command,
            CancellationToken cancellationToken = default)
        {
            CommandCount++;
            return ValueTask.CompletedTask;
        }

        public bool PublishClientRoster(ClientRosterSnapshot snapshot)
        {
            publishedRosters.Enqueue(snapshot);
            return true;
        }

        public ValueTask DisposeAsync()
        {
            if (!IsDisposed)
            {
                IsDisposed = true;
                captures.Writer.TryComplete();
                views.Writer.TryComplete();
                completion.TrySetResult();
            }

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

    private sealed class RecordingLogger : ILogger
    {
        private readonly ConcurrentQueue<string> errorMessages = new();
        private readonly ConcurrentQueue<Exception> exceptions = new();
        private readonly ConcurrentQueue<string> infoMessages = new();

        public bool AutoFlush { get; set; }

        public string[] ErrorMessages => errorMessages.ToArray();

        public Exception[] Exceptions => exceptions.ToArray();

        public string[] InfoMessages => infoMessages.ToArray();

        public void LogInfo(string message, string category = "") =>
            infoMessages.Enqueue(message);

        public void LogWarn(string message, string category = "")
        {
        }

        public void LogError(string message, string category = "") =>
            errorMessages.Enqueue(message);

        public void LogException(
            Exception exception,
            string category = "",
            string memberName = "",
            string filePath = "",
            int lineNumber = 1) =>
            exceptions.Enqueue(exception);

        public void LogDebug(
            string message,
            string category = "",
            string memberName = "",
            string filePath = "",
            int lineNumber = 1)
        {
        }

        public void AddFileTransport(string filePath)
        {
        }

        public void Dispose()
        {
        }
    }
}
