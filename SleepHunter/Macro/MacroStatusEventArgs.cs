using System;

namespace SleepHunter.Macro
{
    public delegate void MacroStatusEventHandler(object sender, MacroStatusEventArgs e);

    public sealed class MacroStatusEventArgs : EventArgs
    {
        public MacroStatus Status { get; }

        public MacroStatusEventArgs(MacroStatus status)
            => Status = status;
    }
}
