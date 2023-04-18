using System;

namespace SleepHunter.Macro
{
    internal delegate void MacroStateEventHandler(object sender, MacroStateEventArgs e);

    internal delegate void MacroStatusEventHandler(object sender, MacroStatusEventArgs e);

    internal sealed class MacroStateEventArgs : EventArgs
    {
        public MacroState State { get; }

        public MacroStateEventArgs(MacroState state)
            => State = state ?? throw new ArgumentNullException(nameof(state));
    }

    internal sealed class MacroStatusEventArgs : EventArgs
    {
        public MacroStatus Status { get; }

        public MacroStatusEventArgs(MacroStatus status)
            => Status = status;
    }
}
