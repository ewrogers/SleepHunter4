using System.ComponentModel;
using System.Text;
using SleepHunter.Interop.Hosting;
using SleepHunter.Interop.Input;
using SleepHunter.Interop.Snapshots;
using SleepHunter.Runtime.Snapshots;
using SleepHunter.Runtime.Time;

namespace SleepHunter.Interop.Tests.Hosting;

public sealed class WindowsClientRuntimeFactoryTests
{
    private static readonly string MappingPath = Path.Combine(
        TestContext.CurrentContext.TestDirectory,
        "Data",
        "Versions.xml");

    [Test]
    public async Task ShouldAttachAnyClientIdentityForBoundedReadOnlyCapture()
    {
        var factory = new WindowsClientRuntimeFactory();
        var client = new ClientIdentity(
            $"custom-client:{Environment.ProcessId}");
        await using var mappingStream = File.OpenRead(MappingPath);
        await using var host = factory.Attach(
            mappingStream,
            client,
            Environment.ProcessId,
            new nint(1),
            new SnapshotCaptureSchedule(
                TimeSpan.FromSeconds(1)),
            new MacroClock(TimeProvider.System));

        var capture = await host.Captures.ReadUntilAsync(
            current => current.Result.Metrics.Sequence.Value == 1);

        Assert.Multiple(() =>
        {
            Assert.That(host.Client, Is.EqualTo(client));
            Assert.That(capture.Result.Succeeded, Is.False);
            Assert.That(host.LatestCaptureResult?.Succeeded, Is.False);
            Assert.That(host.LastIntentIssueResult, Is.Null);
            Assert.That(host.CaptureStatistics.SampleCount, Is.EqualTo(1));
        });
    }

    [Test]
    public void ShouldFailWhenTheProcessCannotBeOpened()
    {
        var factory = new WindowsClientRuntimeFactory();
        var client = new ClientIdentity("process:missing");
        using var mappingStream = File.OpenRead(MappingPath);

        Assert.That(
            () => factory.Attach(
                mappingStream,
                client,
                int.MaxValue,
                new nint(1),
                new SnapshotCaptureSchedule(
                    TimeSpan.FromSeconds(1)),
                new MacroClock(TimeProvider.System)),
            Throws.TypeOf<Win32Exception>());
    }

    [Test]
    public void ShouldRejectMultipleClientMappingsBeforeOpeningAProcess()
    {
        const string xml = """
            <ClientVersions>
              <Clients>
                <Client PointerWidth="Bit32"><Variables /></Client>
                <Client PointerWidth="Bit32"><Variables /></Client>
              </Clients>
            </ClientVersions>
            """;
        var factory = new WindowsClientRuntimeFactory();
        var client = new ClientIdentity("custom-client:1234");
        using var mappingStream = new MemoryStream(
            Encoding.UTF8.GetBytes(xml));

        Assert.That(
            () => factory.Attach(
                mappingStream,
                client,
                Environment.ProcessId,
                new nint(1),
                new SnapshotCaptureSchedule(
                    TimeSpan.FromSeconds(1)),
                new MacroClock(TimeProvider.System)),
            Throws.TypeOf<InvalidDataException>());
    }
}
