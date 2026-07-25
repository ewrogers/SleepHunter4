using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Threading.Channels;
using SleepHunter.Interop.Hosting;
using SleepHunter.Interop.Input;
using SleepHunter.Interop.Snapshots;
using SleepHunter.Persistence.Configuration;
using SleepHunter.Persistence.Serialization;
using SleepHunter.Runtime.Automation.Spells;
using SleepHunter.Runtime.Commands;
using SleepHunter.Runtime.Engine;
using SleepHunter.Runtime.Snapshots;
using SleepHunter.Services.Configuration;
using SleepHunter.Services.Logging;
using SleepHunter.Services.Runtime;
using SleepHunter.ViewModels;

namespace SleepHunter.Tests.Services.Runtime;

public sealed class ClientRuntimeRegistryTests
{
    private static readonly string MappingPath = FindVersionsFile();

    [Test]
    public async Task ShouldAttachFindAndDetachAShadowRuntime()
    {
        var factory = new RecordingRuntimeFactory();
        var logger = new RecordingLogger();
        await using var registry = CreateRegistry(factory, logger);
        var descriptor = Descriptor(
            processId: 1234,
            Usda741SnapshotCapture.SupportedVersion);
        var interval = TimeSpan.FromMilliseconds(250);

        var attached = await registry.AttachAsync(descriptor, interval);
        var duplicate = await registry.AttachAsync(descriptor, interval);
        var wasFound = registry.TryFind(
            descriptor.ProcessId,
            out var viewModel);
        var configurationWasFound = registry.TryFindConfiguration(
            descriptor.ProcessId,
            out var configuration);

        Assert.Multiple(() =>
        {
            Assert.That(attached, Is.True);
            Assert.That(duplicate, Is.False);
            Assert.That(wasFound, Is.True);
            Assert.That(configurationWasFound, Is.True);
            Assert.That(configuration, Is.Not.Null);
            Assert.That(viewModel.Client, Is.EqualTo(descriptor.Client));
            Assert.That(registry.Count, Is.EqualTo(1));
            Assert.That(factory.AttachCount, Is.EqualTo(1));
            Assert.That(factory.LastInterval, Is.EqualTo(interval));
            Assert.That(
                factory.LastSections,
                Is.EqualTo(SnapshotCaptureSections.All));
            Assert.That(logger.InfoMessages.Length, Is.EqualTo(1));
        });
        Assert.That(
            async () => await viewModel.SendCommandAsync(
                new StartMacroCommand()),
            Throws.TypeOf<InvalidOperationException>());

        var detached = await registry.DetachAsync(descriptor.ProcessId);

        Assert.Multiple(() =>
        {
            Assert.That(detached, Is.True);
            Assert.That(registry.Count, Is.Zero);
            Assert.That(factory.LastHost?.IsDisposed, Is.True);
            Assert.That(logger.InfoMessages.Length, Is.EqualTo(2));
            Assert.That(factory.LastHost?.CommandCount, Is.Zero);
        });
    }

    [Test]
    public async Task ShouldSkipUnsupportedClientVersions()
    {
        var factory = new RecordingRuntimeFactory();
        var logger = new RecordingLogger();
        await using var registry = CreateRegistry(factory, logger);

        var attached = await registry.AttachAsync(
            Descriptor(
                processId: 1234,
                version: "Zolian 9.1.1"),
            TimeSpan.FromMilliseconds(200));

        Assert.Multiple(() =>
        {
            Assert.That(attached, Is.False);
            Assert.That(registry.Count, Is.Zero);
            Assert.That(factory.AttachCount, Is.Zero);
            Assert.That(logger.InfoMessages.Length, Is.EqualTo(1));
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
            Descriptor(
                processId: 1234,
                Usda741SnapshotCapture.SupportedVersion),
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
            Descriptor(
                processId: 1234,
                Usda741SnapshotCapture.SupportedVersion),
            TimeSpan.FromMilliseconds(200));
        await registry.AttachAsync(
            Descriptor(
                processId: 5678,
                Usda741SnapshotCapture.SupportedVersion),
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
                    Descriptor(
                        processId: 9012,
                        Usda741SnapshotCapture.SupportedVersion),
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
                Descriptor(
                    processId: 1234,
                    Usda741SnapshotCapture.SupportedVersion),
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
                Descriptor(
                    processId: 1234,
                    Usda741SnapshotCapture.SupportedVersion),
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
        var client = new ClientIdentity(
            "process:1234",
            Usda741SnapshotCapture.SupportedVersion);

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
            new EmptyMacroConfigurationReader(),
            () => SpellQueueRotation.Priority);

    private static string FindVersionsFile()
    {
        var directory = new DirectoryInfo(
            TestContext.CurrentContext.TestDirectory);
        while (directory is not null)
        {
            var candidate = Path.Combine(
                directory.FullName,
                "data",
                "Versions.xml");
            if (File.Exists(candidate))
                return candidate;

            directory = directory.Parent;
        }

        throw new FileNotFoundException(
            "Could not locate data/Versions.xml from the test directory.");
    }

    private static ClientRuntimeDescriptor Descriptor(
        int processId,
        string version) =>
        new(
            new ClientIdentity(
                $"process:{processId}",
                version),
            processId,
            new nint(processId));

    private sealed class RecordingRuntimeFactory : IClientRuntimeFactory
    {
        private readonly ConcurrentQueue<RecordingRuntimeHost> hosts = new();

        public Exception? AttachError { get; init; }

        public Action? BeforeHostCreated { get; init; }

        public int AttachCount { get; private set; }

        public RecordingRuntimeHost? LastHost { get; private set; }

        public TimeSpan LastInterval { get; private set; }

        public SnapshotCaptureSections LastSections { get; private set; }

        public RecordingRuntimeHost[] Hosts => hosts.ToArray();

        public IClientRuntimeHost Attach(
            Stream mappingStream,
            ClientIdentity client,
            int processId,
            nint windowHandle,
            SnapshotCaptureSchedule snapshotSchedule,
            TimeProvider timeProvider,
            AbilitySnapshotCatalog? abilityCatalog = null)
        {
            Assert.That(mappingStream.CanRead, Is.True);
            AttachCount++;
            LastInterval = snapshotSchedule.Interval;
            LastSections = snapshotSchedule.Sections;
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

        public ValueTask SendCommandAsync(
            MacroCommand command,
            CancellationToken cancellationToken = default)
        {
            CommandCount++;
            return ValueTask.CompletedTask;
        }

        public bool PublishClientRoster(ClientRosterSnapshot snapshot) => true;

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

    private sealed class EmptyMacroConfigurationReader :
        IMacroConfigurationReader
    {
        private static readonly MacroConfigurationLoadResult Result = new(
            MacroConfiguration.Empty,
            MacroConfigurationFormat.Current,
            MacroConfigurationSerializer.CurrentVersion,
            ImmutableArray<MacroConfigurationWarning>.Empty);

        public Task<MacroConfigurationLoadResult> LoadAsync(
            string filePath,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(Result);
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
