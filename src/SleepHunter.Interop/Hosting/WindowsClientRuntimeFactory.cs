using System.ComponentModel;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;
using SleepHunter.Interop.Input;
using SleepHunter.Interop.Mappings;
using SleepHunter.Interop.Memory;
using SleepHunter.Interop.Snapshots;
using SleepHunter.Runtime.Commands;
using SleepHunter.Runtime.Engine;
using SleepHunter.Runtime.Snapshots;
using SleepHunter.Runtime.Time;

namespace SleepHunter.Interop.Hosting;

public sealed partial class WindowsClientRuntimeFactory : IClientRuntimeFactory
{
    public IClientRuntimeHost Attach(
        Stream mappingStream,
        ClientIdentity client,
        int processId,
        nint windowHandle,
        SnapshotCaptureSchedule snapshotSchedule,
        TimeProvider timeProvider,
        AbilitySnapshotCatalog? abilityCatalog = null)
    {
        ArgumentNullException.ThrowIfNull(mappingStream);
        ArgumentNullException.ThrowIfNull(client);
        ArgumentNullException.ThrowIfNull(snapshotSchedule);
        ArgumentNullException.ThrowIfNull(timeProvider);

        if (processId <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(processId),
                processId,
                "The client process identifier must be positive.");
        }

        if (windowHandle == nint.Zero)
        {
            throw new ArgumentOutOfRangeException(
                nameof(windowHandle),
                windowHandle,
                "The client window handle cannot be zero.");
        }

        if (!string.Equals(
                client.Version,
                Usda741SnapshotCapture.SupportedVersion,
                StringComparison.OrdinalIgnoreCase))
        {
            throw new NotSupportedException(
                $"Client runtime attachment does not support version '{client.Version}'.");
        }

        var map = ClientMemoryMapLoader.Load(
            mappingStream,
            client.Version);
        if (map.PointerWidth != PointerWidth.Bit32)
        {
            throw new NotSupportedException(
                $"Client runtime attachment does not support {map.PointerWidth} process mappings.");
        }

        var processHandle = OpenReadProcess(processId);
        try
        {
            var clock = new MacroClock(timeProvider);
            var capture = new Usda741SnapshotCapture(
                client,
                map,
                new WindowsProcessMemorySource(processHandle),
                MemoryReadLimits.Client32Bit,
                clock,
                abilityCatalog);
            var executor = new ClientIntentExecutor(
                new Usda741ClientIntentPlanner(
                    new WindowsVirtualKeyMapper()),
                new WindowInputDispatcher(
                    new WindowsClientWindowGuard(),
                    new WindowsWindowMessageSink()));
            var host = new ClientRuntimeHost(
                capture,
                snapshotSchedule,
                executor,
                new WindowsClientWindowTargetProvider(
                    client,
                    processId,
                    windowHandle),
                timeProvider);
            return new OwnedClientRuntimeHost(host, processHandle);
        }
        catch
        {
            processHandle.Dispose();
            throw;
        }
    }

    private static SafeProcessHandle OpenReadProcess(int processId)
    {
        var processHandle = NativeProcessMethods.OpenProcess(
            ProcessAccessRights.QueryLimitedInformation |
            ProcessAccessRights.VirtualMemoryRead,
            inheritHandle: false,
            processId);
        if (!processHandle.IsInvalid)
        {
            return processHandle;
        }

        var errorCode = Marshal.GetLastPInvokeError();
        processHandle.Dispose();
        throw new Win32Exception(
            errorCode,
            $"Unable to open process {processId} for bounded read-only capture.");
    }

    [Flags]
    private enum ProcessAccessRights : uint
    {
        VirtualMemoryRead = 0x0010,
        QueryLimitedInformation = 0x1000
    }

    private static partial class NativeProcessMethods
    {
        [LibraryImport(
            "kernel32.dll",
            EntryPoint = "OpenProcess",
            SetLastError = true)]
        internal static partial SafeProcessHandle OpenProcess(
            ProcessAccessRights desiredAccess,
            [MarshalAs(UnmanagedType.Bool)] bool inheritHandle,
            int processId);
    }

    private sealed class OwnedClientRuntimeHost : IClientRuntimeHost
    {
        private readonly IClientRuntimeHost host;
        private readonly SafeProcessHandle processHandle;
        private int disposeState;

        public OwnedClientRuntimeHost(
            IClientRuntimeHost host,
            SafeProcessHandle processHandle)
        {
            this.host = host;
            this.processHandle = processHandle;
        }

        public ClientIdentity Client => host.Client;

        public System.Threading.Channels.ChannelReader<MacroViewSnapshot>
            Views => host.Views;

        public SnapshotCaptureResult? LatestCaptureResult =>
            host.LatestCaptureResult;

        public ClientIntentIssueResult? LastIntentIssueResult =>
            host.LastIntentIssueResult;

        public SnapshotCaptureStatistics CaptureStatistics =>
            host.CaptureStatistics;

        public Task Completion => host.Completion;

        public ValueTask SendCommandAsync(
            MacroCommand command,
            CancellationToken cancellationToken = default) =>
            host.SendCommandAsync(command, cancellationToken);

        public bool PublishClientRoster(ClientRosterSnapshot snapshot) =>
            host.PublishClientRoster(snapshot);

        public async ValueTask DisposeAsync()
        {
            var isFirstDispose =
                Interlocked.Exchange(ref disposeState, 1) == 0;
            try
            {
                await host.DisposeAsync().ConfigureAwait(false);
            }
            finally
            {
                if (isFirstDispose)
                {
                    processHandle.Dispose();
                }
            }
        }
    }
}
