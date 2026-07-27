using System;

namespace SleepHunter.Models
{
    public sealed class ClientProcess
    {
        public int ProcessId { get; init; }

        public nint WindowHandle { get; init; }

        public string WindowTitle { get; init; } =
            string.Empty;

        public DateTime CreationTime { get; init; }
    }
}
