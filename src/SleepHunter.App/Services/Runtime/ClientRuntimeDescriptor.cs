using System;
using SleepHunter.Runtime.Snapshots;

namespace SleepHunter.Services.Runtime
{
    public sealed record ClientRuntimeDescriptor
    {
        public ClientRuntimeDescriptor(
            ClientIdentity client,
            int processId,
            nint windowHandle)
        {
            Client = client ??
                throw new ArgumentNullException(nameof(client));

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

            ProcessId = processId;
            WindowHandle = windowHandle;
        }

        public ClientIdentity Client { get; }

        public int ProcessId { get; }

        public nint WindowHandle { get; }
    }
}
