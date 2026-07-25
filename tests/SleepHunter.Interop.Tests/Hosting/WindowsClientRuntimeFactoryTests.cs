using System.ComponentModel;
using SleepHunter.Interop.Hosting;
using SleepHunter.Interop.Input;
using SleepHunter.Interop.Snapshots;
using SleepHunter.Runtime.Snapshots;

namespace SleepHunter.Interop.Tests.Hosting;

public sealed class WindowsClientRuntimeFactoryTests
{
    private static readonly string MappingPath = Path.Combine(
        TestContext.CurrentContext.TestDirectory,
        "Data",
        "Versions.xml");

    [Test]
    public async Task ShouldAttachToAProcessForBoundedReadOnlyCapture()
    {
        var factory = new WindowsClientRuntimeFactory();
        var client = new ClientIdentity(
            $"process:{Environment.ProcessId}",
            Usda741SnapshotCapture.SupportedVersion);
        await using var mappingStream = File.OpenRead(MappingPath);
        await using var host = factory.Attach(
            mappingStream,
            client,
            Environment.ProcessId,
            new nint(1),
            new SnapshotCaptureSchedule(
                TimeSpan.FromSeconds(1)),
            TimeProvider.System);

        await WaitUntilAsync(() => host.LatestCaptureResult is not null);

        Assert.Multiple(() =>
        {
            Assert.That(host.Client, Is.EqualTo(client));
            Assert.That(host.LatestCaptureResult?.Succeeded, Is.False);
            Assert.That(host.LastIntentIssueResult, Is.Null);
            Assert.That(host.CaptureStatistics.SampleCount, Is.EqualTo(1));
        });
    }

    [Test]
    public void ShouldFailWhenTheProcessCannotBeOpened()
    {
        var factory = new WindowsClientRuntimeFactory();
        var client = new ClientIdentity(
            "process:missing",
            Usda741SnapshotCapture.SupportedVersion);
        using var mappingStream = File.OpenRead(MappingPath);

        Assert.That(
            () => factory.Attach(
                mappingStream,
                client,
                int.MaxValue,
                new nint(1),
                new SnapshotCaptureSchedule(
                    TimeSpan.FromSeconds(1)),
                TimeProvider.System),
            Throws.TypeOf<Win32Exception>());
    }

    [Test]
    public void ShouldRejectUnsupportedClientsBeforeReadingMappings()
    {
        var factory = new WindowsClientRuntimeFactory();
        var client = new ClientIdentity(
            "process:unsupported",
            "Zolian 9.1.1");

        Assert.That(
            () => factory.Attach(
                Stream.Null,
                client,
                Environment.ProcessId,
                new nint(1),
                new SnapshotCaptureSchedule(
                    TimeSpan.FromSeconds(1)),
                TimeProvider.System),
            Throws.TypeOf<NotSupportedException>());
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
                "The expected attachment state was not observed.");
        }
    }
}
