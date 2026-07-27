using SleepHunter.Services.Clients;
using SleepHunter.Tests.Support;
using SleepHunter.ViewModels;

namespace SleepHunter.Tests.Services.Clients;

public sealed class ClientDiscoveryCoordinatorTests
{
    [Test]
    public async Task ShouldPollImmediatelyAndUseConfiguredCadence()
    {
        var processPolls = 0;
        var reconciliations = 0;
        var timeProvider = new ManualTimeProvider();
        var dispatcher = new InlineDispatcher();
        await using var coordinator =
            new ClientDiscoveryCoordinator(
                () => Interlocked.Increment(
                    ref processPolls),
                () => Interlocked.Increment(
                    ref reconciliations),
                () => TimeSpan.FromSeconds(1),
                dispatcher,
                timeProvider,
                new TestLogger());

        coordinator.Start();
        await WaitUntilAsync(
            () => Volatile.Read(ref processPolls) == 1 &&
                  Volatile.Read(ref reconciliations) == 1 &&
                  timeProvider.ActiveTimerCount == 1);

        timeProvider.Advance(
            TimeSpan.FromMilliseconds(999));
        await Task.Yield();

        Assert.Multiple(() =>
        {
            Assert.That(
                Volatile.Read(ref processPolls),
                Is.EqualTo(1));
            Assert.That(
                Volatile.Read(ref reconciliations),
                Is.EqualTo(1));
            Assert.That(dispatcher.InvocationCount, Is.EqualTo(1));
            Assert.That(coordinator.IsRunning, Is.True);
        });

        timeProvider.Advance(
            TimeSpan.FromMilliseconds(1));
        await WaitUntilAsync(
            () => Volatile.Read(ref processPolls) == 2 &&
                  Volatile.Read(ref reconciliations) == 2);

        Assert.Multiple(() =>
        {
            Assert.That(
                Volatile.Read(ref reconciliations),
                Is.EqualTo(2));
            Assert.That(dispatcher.InvocationCount, Is.EqualTo(2));
        });
    }

    [Test]
    public async Task ShouldReadChangedIntervalsAfterEachPoll()
    {
        var processPolls = 0;
        var processInterval =
            TimeSpan.FromMilliseconds(100);
        var timeProvider = new ManualTimeProvider();
        await using var coordinator =
            new ClientDiscoveryCoordinator(
                () => Interlocked.Increment(
                    ref processPolls),
                () => { },
                () => processInterval,
                new InlineDispatcher(),
                timeProvider,
                new TestLogger());

        coordinator.Start();
        await WaitUntilAsync(
            () => Volatile.Read(ref processPolls) == 1 &&
                  timeProvider.ActiveTimerCount == 1);

        processInterval = TimeSpan.FromMilliseconds(500);
        timeProvider.Advance(
            TimeSpan.FromMilliseconds(100));
        await WaitUntilAsync(
            () => Volatile.Read(ref processPolls) == 2 &&
                  timeProvider.ActiveTimerCount == 1);

        timeProvider.Advance(
            TimeSpan.FromMilliseconds(499));
        await Task.Yield();
        Assert.That(
            Volatile.Read(ref processPolls),
            Is.EqualTo(2));

        timeProvider.Advance(
            TimeSpan.FromMilliseconds(1));
        await WaitUntilAsync(
            () => Volatile.Read(ref processPolls) == 3);
    }

    [Test]
    public async Task ShouldContinueAfterAnIterationFails()
    {
        var processPolls = 0;
        var reconciliations = 0;
        var timeProvider = new ManualTimeProvider();
        var logger = new TestLogger();
        await using var coordinator =
            new ClientDiscoveryCoordinator(
                () =>
                {
                    if (Interlocked.Increment(
                            ref processPolls) == 1)
                    {
                        throw new InvalidOperationException(
                            "Scan failed");
                    }
                },
                () => Interlocked.Increment(
                    ref reconciliations),
                () => TimeSpan.FromMilliseconds(100),
                new InlineDispatcher(),
                timeProvider,
                logger);

        coordinator.Start();
        await WaitUntilAsync(
            () => Volatile.Read(ref processPolls) == 1 &&
                  timeProvider.ActiveTimerCount == 1);

        timeProvider.Advance(
            TimeSpan.FromMilliseconds(100));
        await WaitUntilAsync(
            () => Volatile.Read(ref processPolls) == 2 &&
                  Volatile.Read(ref reconciliations) == 1);

        Assert.Multiple(() =>
        {
            Assert.That(
                Volatile.Read(ref reconciliations),
                Is.EqualTo(1));
            Assert.That(logger.Exceptions, Has.Count.EqualTo(1));
            Assert.That(logger.Errors, Has.Count.EqualTo(1));
            Assert.That(coordinator.IsRunning, Is.True);
        });
    }

    [Test]
    public async Task ShouldClampInvalidIntervalsAndStopPromptly()
    {
        var processPolls = 0;
        var timeProvider = new ManualTimeProvider();
        var coordinator =
            new ClientDiscoveryCoordinator(
                () => Interlocked.Increment(
                    ref processPolls),
                () => { },
                () => TimeSpan.Zero,
                new InlineDispatcher(),
                timeProvider,
                new TestLogger());

        coordinator.Start();
        await WaitUntilAsync(
            () => Volatile.Read(ref processPolls) == 1 &&
                  timeProvider.ActiveTimerCount == 1);

        timeProvider.Advance(
            TimeSpan.FromMilliseconds(9));
        await Task.Yield();
        Assert.That(
            Volatile.Read(ref processPolls),
            Is.EqualTo(1));

        timeProvider.Advance(
            TimeSpan.FromMilliseconds(1));
        await WaitUntilAsync(
            () => Volatile.Read(ref processPolls) == 2);

        await coordinator.DisposeAsync();
        var pollsAfterDispose =
            Volatile.Read(ref processPolls);
        timeProvider.Advance(TimeSpan.FromDays(1));
        await Task.Yield();

        Assert.Multiple(() =>
        {
            Assert.That(coordinator.IsRunning, Is.False);
            Assert.That(
                coordinator.Completion.IsCompleted,
                Is.True);
            Assert.That(
                Volatile.Read(ref processPolls),
                Is.EqualTo(pollsAfterDispose));
            Assert.That(
                timeProvider.ActiveTimerCount,
                Is.EqualTo(0));
        });
    }

    private static async Task WaitUntilAsync(
        Func<bool> predicate)
    {
        var timeout = DateTime.UtcNow +
                      TimeSpan.FromSeconds(2);
        while (!predicate())
        {
            if (DateTime.UtcNow >= timeout)
            {
                Assert.Fail(
                    "The polling condition was not reached.");
            }

            await Task.Delay(
                TimeSpan.FromMilliseconds(1));
        }
    }

    private sealed class InlineDispatcher : IUiDispatcher
    {
        private int invocationCount;

        public int InvocationCount =>
            Volatile.Read(ref invocationCount);

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
