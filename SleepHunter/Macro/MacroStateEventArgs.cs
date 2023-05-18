using System;

namespace SleepHunter.Macro
{
    public delegate void MacroStateEventHandler(object sender, MacroStateEventArgs e);

    public delegate void MacroStatusEventHandler(object sender, MacroStatusEventArgs e);

    public sealed class MacroStateEventArgs : EventArgs
    {
        public MacroState State { get; }

        public MacroStateEventArgs(MacroState state)
        {
            State = state;
        }
    }

    public sealed class MacroStatusEventArgs : EventArgs
    {
        public MacroStatus Status { get; }

        public MacroStatusEventArgs(MacroStatus status)
        {
            Status = status;
        }
    }
}
