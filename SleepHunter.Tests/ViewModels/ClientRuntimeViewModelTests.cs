using System.Collections.Immutable;
using System.Threading.Channels;
using SleepHunter.Interop.Hosting;
using SleepHunter.Interop.Input;
using SleepHunter.Interop.Snapshots;
using SleepHunter.Runtime.Automation.Flowering;
using SleepHunter.Runtime.Automation.Panels;
using SleepHunter.Runtime.Automation.Skills;
using SleepHunter.Runtime.Automation.Spells;
using SleepHunter.Runtime.Commands;
using SleepHunter.Runtime.Engine;
using SleepHunter.Runtime.Snapshots;
using SleepHunter.ViewModels;

namespace SleepHunter.Tests.ViewModels;

public sealed class ClientRuntimeViewModelTests
{
    [Test]
    public async Task ShouldProjectViewsAndRouteLifecycleCommands()
    {
        var host = new RecordingRuntimeHost();
        var dispatcher = new RecordingUiDispatcher();
        await using var viewModel = new ClientRuntimeViewModel(
            host,
            dispatcher);

        host.PublishView(CreateView(0, MacroLifecycle.Stopped));
        await WaitUntilAsync(() => viewModel.Current?.Revision == 0);
        Assert.Multiple(() =>
        {
            Assert.That(dispatcher.InvocationCount, Is.EqualTo(1));
            Assert.That(viewModel.StartCommand.CanExecute(null), Is.True);
            Assert.That(viewModel.PauseCommand.CanExecute(null), Is.False);
            Assert.That(viewModel.ResumeCommand.CanExecute(null), Is.False);
            Assert.That(viewModel.StopCommand.CanExecute(null), Is.False);
        });

        await viewModel.StartCommand.ExecuteAsync(null);
        Assert.That(
            await host.ReadCommandAsync(),
            Is.TypeOf<StartMacroCommand>());

        host.PublishView(CreateView(1, MacroLifecycle.Running));
        await WaitUntilAsync(() => viewModel.Current?.Revision == 1);
        Assert.Multiple(() =>
        {
            Assert.That(viewModel.StartCommand.CanExecute(null), Is.False);
            Assert.That(viewModel.PauseCommand.CanExecute(null), Is.True);
            Assert.That(viewModel.ResumeCommand.CanExecute(null), Is.False);
            Assert.That(viewModel.StopCommand.CanExecute(null), Is.True);
        });

        await viewModel.PauseCommand.ExecuteAsync(null);
        Assert.That(
            await host.ReadCommandAsync(),
            Is.TypeOf<PauseMacroCommand>());

        host.PublishView(CreateView(2, MacroLifecycle.Paused));
        await WaitUntilAsync(() => viewModel.Current?.Revision == 2);
        Assert.Multiple(() =>
        {
            Assert.That(viewModel.StartCommand.CanExecute(null), Is.False);
            Assert.That(viewModel.PauseCommand.CanExecute(null), Is.False);
            Assert.That(viewModel.ResumeCommand.CanExecute(null), Is.True);
            Assert.That(viewModel.StopCommand.CanExecute(null), Is.True);
        });

        await viewModel.ResumeCommand.ExecuteAsync(null);
        Assert.That(
            await host.ReadCommandAsync(),
            Is.TypeOf<ResumeMacroCommand>());
        await viewModel.StopCommand.ExecuteAsync(null);
        Assert.That(
            await host.ReadCommandAsync(),
            Is.TypeOf<StopMacroCommand>());
    }

    [Test]
    public async Task ShouldForwardFeatureCommandsWithoutUiLogic()
    {
        var host = new RecordingRuntimeHost();
        await using var viewModel = new ClientRuntimeViewModel(
            host,
            new RecordingUiDispatcher());
        var command = new RequestPanelTransitionCommand(
            ClientPanel.Inventory);

        await viewModel.SendCommandAsync(command);

        Assert.That(await host.ReadCommandAsync(), Is.SameAs(command));
    }

    [Test]
    public async Task ShouldDisposeTheOwnedHostAndDisableCommands()
    {
        var host = new RecordingRuntimeHost();
        var viewModel = new ClientRuntimeViewModel(
            host,
            new RecordingUiDispatcher());
        host.PublishView(CreateView(0, MacroLifecycle.Stopped));
        await WaitUntilAsync(() => viewModel.Current is not null);

        await viewModel.DisposeAsync();

        Assert.Multiple(() =>
        {
            Assert.That(host.IsDisposed, Is.True);
            Assert.That(host.Completion.IsCompletedSuccessfully, Is.True);
            Assert.That(viewModel.StartCommand.CanExecute(null), Is.False);
            Assert.That(viewModel.PauseCommand.CanExecute(null), Is.False);
            Assert.That(viewModel.ResumeCommand.CanExecute(null), Is.False);
            Assert.That(viewModel.StopCommand.CanExecute(null), Is.False);
            Assert.That(
                async () => await viewModel.SendCommandAsync(
                    new StartMacroCommand()),
                Throws.TypeOf<ObjectDisposedException>());
        });
    }

    [Test]
    public async Task ShouldDisableCommandsWhenTheHostStopsUnexpectedly()
    {
        var host = new RecordingRuntimeHost();
        await using var viewModel = new ClientRuntimeViewModel(
            host,
            new RecordingUiDispatcher());
        host.PublishView(CreateView(0, MacroLifecycle.Stopped));
        await WaitUntilAsync(
            () => viewModel.StartCommand.CanExecute(null));

        host.CompleteViews();
        await WaitUntilAsync(
            () => !viewModel.StartCommand.CanExecute(null));

        Assert.That(viewModel.StartCommand.CanExecute(null), Is.False);
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
            SpellQueueState.Empty,
            PanelTransition: null,
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
                "The expected view-model state was not observed.");
        }
    }

    private sealed class RecordingRuntimeHost : IClientRuntimeHost
    {
        private readonly Channel<MacroCommand> commands =
            Channel.CreateUnbounded<MacroCommand>();
        private readonly TaskCompletionSource completion =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly Channel<MacroViewSnapshot> views =
            Channel.CreateUnbounded<MacroViewSnapshot>();

        public ClientIdentity Client { get; } = new(
            "process:1234",
            "USDA 7.41");

        public ChannelReader<MacroViewSnapshot> Views => views.Reader;

        public SnapshotCaptureResult? LatestCaptureResult => null;

        public ClientIntentIssueResult? LastIntentIssueResult => null;

        public SnapshotCaptureStatistics CaptureStatistics =>
            SnapshotCaptureStatistics.Empty(1);

        public Task Completion => completion.Task;

        public bool IsDisposed { get; private set; }

        public ValueTask SendCommandAsync(
            MacroCommand command,
            CancellationToken cancellationToken = default)
        {
            ObjectDisposedException.ThrowIf(IsDisposed, this);
            return commands.Writer.WriteAsync(command, cancellationToken);
        }

        public bool PublishClientRoster(ClientRosterSnapshot snapshot)
        {
            ObjectDisposedException.ThrowIf(IsDisposed, this);
            return true;
        }

        public ValueTask DisposeAsync()
        {
            if (!IsDisposed)
            {
                IsDisposed = true;
                views.Writer.TryComplete();
                commands.Writer.TryComplete();
                completion.TrySetResult();
            }

            return ValueTask.CompletedTask;
        }

        public void PublishView(MacroViewSnapshot view)
        {
            if (!views.Writer.TryWrite(view))
            {
                throw new InvalidOperationException(
                    "The test view channel is unavailable.");
            }
        }

        public void CompleteViews() => views.Writer.TryComplete();

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
                    "The expected command was not received.");
            }
        }
    }

    private sealed class RecordingUiDispatcher : IUiDispatcher
    {
        private int invocationCount;

        public int InvocationCount => Volatile.Read(ref invocationCount);

        public ValueTask InvokeAsync(
            Action action,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Interlocked.Increment(ref invocationCount);
            action();
            return ValueTask.CompletedTask;
        }
    }
}
