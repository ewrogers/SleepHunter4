using System.Threading.Channels;
using SleepHunter.Interop.Input;
using SleepHunter.Interop.Snapshots;
using SleepHunter.Runtime.Commands;
using SleepHunter.Runtime.Engine;
using SleepHunter.Runtime.Snapshots;

namespace SleepHunter.Interop.Hosting;

public sealed class ReadOnlyClientRuntimeHost : IClientRuntimeHost
{
    private readonly IClientRuntimeHost host;

    public ReadOnlyClientRuntimeHost(IClientRuntimeHost host)
    {
        ArgumentNullException.ThrowIfNull(host);
        this.host = host;
    }

    public ClientIdentity Client => host.Client;

    public ChannelReader<MacroViewSnapshot> Views => host.Views;

    public SnapshotCaptureResult? LatestCaptureResult =>
        host.LatestCaptureResult;

    public ClientIntentIssueResult? LastIntentIssueResult =>
        host.LastIntentIssueResult;

    public SnapshotCaptureStatistics CaptureStatistics =>
        host.CaptureStatistics;

    public Task Completion => host.Completion;

    public ValueTask SendCommandAsync(
        MacroCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        cancellationToken.ThrowIfCancellationRequested();
        return ValueTask.FromException(
            new InvalidOperationException(
                "A read-only client runtime host cannot accept commands."));
    }

    public bool PublishClientRoster(ClientRosterSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        return false;
    }

    public ValueTask DisposeAsync() => host.DisposeAsync();
}
