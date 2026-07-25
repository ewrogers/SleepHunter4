using System.Collections.Immutable;
using System.Threading.Channels;

using SleepHunter.Runtime.Commands;
using SleepHunter.Runtime.Engine;
using SleepHunter.Runtime.Events;
using SleepHunter.Runtime.Intents;
using SleepHunter.Runtime.Snapshots;
using SleepHunter.Runtime.Time;

namespace SleepHunter.Runtime.Hosting;

public sealed class MacroSession : IAsyncDisposable
{
    private const int MaximumCommandsPerIteration = 256;

    private readonly MacroClock clock;
    private readonly Channel<MacroCommand> commands;
    private readonly CancellationTokenSource disposeCancellation = new();
    private readonly IMacroEngine engine;
    private readonly Channel<MacroIntent> intents;
    private readonly Queue<MacroEvent> immediateEvents = new();
    private readonly LatestValueMailbox<ClientSnapshot> snapshots = new();
    private readonly PriorityQueue<
        ScheduledMacroEvent,
        (long DueTicks, long EnqueueOrder)> scheduledEvents = new();
    private readonly Channel<MacroViewSnapshot> views;
    private readonly Channel<byte> wakeSignals;
    private readonly Task worker;

    private int disposeState;
    private long scheduledEventSequence;
    private MacroState state = MacroState.Initial;

    public MacroSession(IMacroEngine engine, MacroClock clock)
    {
        ArgumentNullException.ThrowIfNull(engine);
        ArgumentNullException.ThrowIfNull(clock);

        this.engine = engine;
        this.clock = clock;
        commands = Channel.CreateUnbounded<MacroCommand>(
            new UnboundedChannelOptions
            {
                AllowSynchronousContinuations = false,
                SingleReader = true,
                SingleWriter = false
            });
        intents = Channel.CreateUnbounded<MacroIntent>(
            new UnboundedChannelOptions
            {
                AllowSynchronousContinuations = false,
                SingleReader = true,
                SingleWriter = true
            });
        views = Channel.CreateUnbounded<MacroViewSnapshot>(
            new UnboundedChannelOptions
            {
                AllowSynchronousContinuations = false,
                SingleReader = true,
                SingleWriter = true
            });
        wakeSignals = Channel.CreateBounded<byte>(
            new BoundedChannelOptions(1)
            {
                AllowSynchronousContinuations = false,
                FullMode = BoundedChannelFullMode.DropWrite,
                SingleReader = true,
                SingleWriter = false
            });

        views.Writer.TryWrite(MacroViewSnapshot.FromState(state));
        worker = RunAsync(disposeCancellation.Token);
    }

    public ChannelReader<MacroIntent> Intents => intents.Reader;

    public ChannelReader<MacroViewSnapshot> Views => views.Reader;

    public Task Completion => worker;

    public async ValueTask SendCommandAsync(
        MacroCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        ThrowIfDisposing();

        try
        {
            await commands.Writer
                .WriteAsync(command, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (ChannelClosedException) when (Volatile.Read(ref disposeState) != 0)
        {
            throw new ObjectDisposedException(nameof(MacroSession));
        }

        SignalWorker();
    }

    public bool PublishSnapshot(ClientSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        ThrowIfDisposing();

        ObjectDisposedException.ThrowIf(!snapshots.TryWrite(snapshot), this);

        SignalWorker();
        return true;
    }

    public async ValueTask DisposeAsync()
    {
        var isFirstDispose = Interlocked.Exchange(ref disposeState, 1) == 0;
        if (isFirstDispose)
        {
            commands.Writer.TryComplete();
            snapshots.Complete();
            disposeCancellation.Cancel();
            SignalWorker();
        }

        try
        {
            await worker.ConfigureAwait(false);
        }
        finally
        {
            if (isFirstDispose)
            {
                disposeCancellation.Dispose();
            }
        }
    }

    private async Task RunAsync(CancellationToken cancellationToken)
    {
        Exception? completionError = null;

        try
        {
            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();
                DrainWakeSignals();

                var didWork = ProcessCommands();
                didWork |= ProcessLatestSnapshot();
                didWork |= ProcessDueEvents();

                if (didWork)
                {
                    continue;
                }

                await WaitForInputAsync(cancellationToken).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (Exception exception)
        {
            completionError = exception;
            throw;
        }
        finally
        {
            wakeSignals.Writer.TryComplete();
            intents.Writer.TryComplete(completionError);
            views.Writer.TryComplete(completionError);
        }
    }

    private bool ProcessCommands()
    {
        var processed = false;

        for (var index = 0;
             index < MaximumCommandsPerIteration &&
             commands.Reader.TryRead(out var command);
             index++)
        {
            ProcessInput(new MacroCommandReceived(command));
            processed = true;
        }

        return processed;
    }

    private bool ProcessLatestSnapshot()
    {
        if (!snapshots.TryReadLatest(out var snapshot))
        {
            return false;
        }

        ProcessInput(new ClientSnapshotObserved(snapshot));
        return true;
    }

    private bool ProcessDueEvents()
    {
        var processed = false;
        var currentTime = clock.GetCurrentTimestamp();

        while (scheduledEvents.TryPeek(out _, out var priority) &&
               priority.DueTicks <= currentTime.Elapsed.Ticks)
        {
            var scheduledEvent = scheduledEvents.Dequeue();
            ProcessInput(scheduledEvent.Input);
            processed = true;
            currentTime = clock.GetCurrentTimestamp();
        }

        return processed;
    }

    private void ProcessInput(MacroEvent input)
    {
        immediateEvents.Enqueue(input);

        while (immediateEvents.TryDequeue(out var currentInput))
        {
            var currentTime = clock.GetCurrentTimestamp();
            var decision = engine.Decide(state, currentInput, currentTime);
            MacroDecisionInvariants.EnsureValid(state, decision, currentTime);
            state = decision.State;

            foreach (var raisedEvent in decision.RaisedEvents)
            {
                immediateEvents.Enqueue(raisedEvent);
            }

            foreach (var scheduledEvent in decision.ScheduledEvents)
            {
                scheduledEvents.Enqueue(
                    scheduledEvent,
                    (
                        scheduledEvent.DueAt.Elapsed.Ticks,
                        checked(scheduledEventSequence++)));
            }

            if (decision.Intent is not null)
            {
                intents.Writer.TryWrite(decision.Intent);
            }

            if (decision.PublishedView is not null)
            {
                views.Writer.TryWrite(decision.PublishedView);
            }
        }
    }

    private async Task WaitForInputAsync(CancellationToken cancellationToken)
    {
        if (!scheduledEvents.TryPeek(out _, out var priority))
        {
            await wakeSignals.Reader
                .WaitToReadAsync(cancellationToken)
                .ConfigureAwait(false);
            return;
        }

        var currentTime = clock.GetCurrentTimestamp();
        var delay = TimeSpan.FromTicks(priority.DueTicks) - currentTime.Elapsed;
        if (delay <= TimeSpan.Zero)
        {
            return;
        }

        using var waitCancellation =
            CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var wakeTask = wakeSignals.Reader
            .WaitToReadAsync(waitCancellation.Token)
            .AsTask();
        var delayTask = Task.Delay(
            delay,
            clock.TimeProvider,
            waitCancellation.Token);

        if (priority.DueTicks <=
            clock.GetCurrentTimestamp().Elapsed.Ticks)
        {
            await CancelWaitAsync(
                    waitCancellation,
                    wakeTask,
                    delayTask,
                    cancellationToken)
                .ConfigureAwait(false);
            return;
        }

        await Task.WhenAny(wakeTask, delayTask).ConfigureAwait(false);
        await CancelWaitAsync(
                waitCancellation,
                wakeTask,
                delayTask,
                cancellationToken)
            .ConfigureAwait(false);
    }

    private static async Task CancelWaitAsync(
        CancellationTokenSource waitCancellation,
        Task wakeTask,
        Task delayTask,
        CancellationToken cancellationToken)
    {
        await waitCancellation.CancelAsync().ConfigureAwait(false);
        try
        {
            await Task.WhenAll(wakeTask, delayTask).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (waitCancellation.IsCancellationRequested)
        {
        }

        cancellationToken.ThrowIfCancellationRequested();
    }

    private void DrainWakeSignals()
    {
        while (wakeSignals.Reader.TryRead(out _))
        {
        }
    }

    private void SignalWorker() => wakeSignals.Writer.TryWrite(0);

    private void ThrowIfDisposing()
    {
        ObjectDisposedException.ThrowIf(
            Volatile.Read(ref disposeState) != 0,
            this);
    }
}
