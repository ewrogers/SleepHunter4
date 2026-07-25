using System.Threading.Channels;
using SleepHunter.Interop.Input;
using SleepHunter.Interop.Snapshots;
using SleepHunter.Runtime.Commands;
using SleepHunter.Runtime.Engine;
using SleepHunter.Runtime.Snapshots;

namespace SleepHunter.Interop.Hosting;

public interface IClientRuntimeHost : IAsyncDisposable
{
    ClientIdentity Client { get; }

    ChannelReader<MacroViewSnapshot> Views { get; }

    SnapshotCaptureResult? LatestCaptureResult { get; }

    ClientIntentIssueResult? LastIntentIssueResult { get; }

    SnapshotCaptureStatistics CaptureStatistics { get; }

    Task Completion { get; }

    ValueTask SendCommandAsync(
        MacroCommand command,
        CancellationToken cancellationToken = default);

    bool PublishClientRoster(ClientRosterSnapshot snapshot);
}
