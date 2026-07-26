using System.Collections.Immutable;
using System.Threading.Channels;
using SleepHunter.Interop.Hosting;
using SleepHunter.Interop.Input;
using SleepHunter.Interop.Snapshots;
using SleepHunter.Runtime.Automation;
using SleepHunter.Runtime.Automation.Flowering;
using SleepHunter.Runtime.Automation.Skills;
using SleepHunter.Runtime.Automation.Spells;
using SleepHunter.Runtime.Commands;
using SleepHunter.Runtime.Engine;
using SleepHunter.Runtime.Snapshots;
using SleepHunter.Runtime.Time;

namespace SleepHunter.Interop.Tests.Hosting;

public sealed class ShadowClientRuntimeHostTests
{
    [Test]
    public async Task ShouldForwardOnlyAtomicQueueReplacementCommands()
    {
        var innerHost = new RecordingClientRuntimeHost();
        await using var host = new ShadowClientRuntimeHost(innerHost);
        var command = new ReplaceQueuesCommand(
            Array.Empty<SpellQueueEntry>(),
            SpellQueueRotation.Priority,
            Array.Empty<SkillQueueEntry>(),
            Array.Empty<FlowerQueueEntry>());

        await host.SendCommandAsync(command);

        Assert.That(innerHost.Commands, Is.EqualTo(new[] { command }));
    }

    [Test]
    public void ShouldRejectLifecycleAndIncrementalQueueCommands()
    {
        var innerHost = new RecordingClientRuntimeHost();
        var host = new ShadowClientRuntimeHost(innerHost);
        MacroCommand[] commands =
        [
            new StartMacroCommand(),
            new ConfigureAutomationCommand(
                new AutomationConfiguration(
                    spellsEnabled: true)),
            new ReplaceSpellQueueCommand(
                Array.Empty<SpellQueueEntry>(),
                SpellQueueRotation.Priority),
            new ReplaceSkillQueueCommand(
                Array.Empty<SkillQueueEntry>()),
            new ReplaceFlowerQueueCommand(
                Array.Empty<FlowerQueueEntry>()),
            new ClearSpellQueueCommand(),
            new ClearSkillQueueCommand(),
            new ClearFlowerQueueCommand(),
        ];

        Assert.Multiple(() =>
        {
            foreach (var command in commands)
            {
                Assert.That(
                    async () => await host.SendCommandAsync(command),
                    Throws.InvalidOperationException);
            }
        });
        Assert.That(innerHost.Commands, Is.Empty);
    }

    [Test]
    public void ShouldRejectClientRosterPublication()
    {
        var innerHost = new RecordingClientRuntimeHost();
        var host = new ShadowClientRuntimeHost(innerHost);
        var roster = new ClientRosterSnapshot(
            new ClientRosterSequence(1),
            MacroTimestamp.Zero,
            ImmutableArray<ClientRosterEntry>.Empty);

        var published = host.PublishClientRoster(roster);

        Assert.Multiple(() =>
        {
            Assert.That(published, Is.False);
            Assert.That(innerHost.PublishedRosters, Is.Empty);
        });
    }

    private sealed class RecordingClientRuntimeHost : IClientRuntimeHost
    {
        private readonly Channel<SnapshotCaptureObservation> captures =
            Channel.CreateUnbounded<SnapshotCaptureObservation>();
        private readonly List<MacroCommand> commands = [];
        private readonly List<ClientRosterSnapshot> publishedRosters = [];
        private readonly Channel<MacroViewSnapshot> views =
            Channel.CreateUnbounded<MacroViewSnapshot>();

        public ClientIdentity Client { get; } =
            new("process:1234");

        public ChannelReader<SnapshotCaptureObservation> Captures =>
            captures.Reader;

        public ChannelReader<MacroViewSnapshot> Views => views.Reader;

        public SnapshotCaptureResult? LatestCaptureResult => null;

        public ClientIntentIssueResult? LastIntentIssueResult => null;

        public SnapshotCaptureStatistics CaptureStatistics { get; } =
            SnapshotCaptureStatistics.Empty(windowCapacity: 1);

        public Task Completion => Task.CompletedTask;

        public IReadOnlyList<MacroCommand> Commands => commands;

        public IReadOnlyList<ClientRosterSnapshot> PublishedRosters =>
            publishedRosters;

        public ValueTask SendCommandAsync(
            MacroCommand command,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            commands.Add(command);
            return ValueTask.CompletedTask;
        }

        public bool PublishClientRoster(ClientRosterSnapshot snapshot)
        {
            publishedRosters.Add(snapshot);
            return true;
        }

        public ValueTask DisposeAsync()
        {
            captures.Writer.TryComplete();
            views.Writer.TryComplete();
            return ValueTask.CompletedTask;
        }
    }
}
