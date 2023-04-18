using System;

namespace SleepHunter.Macro
{
    internal delegate void MacroStateEventHandler(object sender, MacroStateEventArgs e);

    internal delegate void MacroStatusEventHandler(object sender, MacroStatusEventArgs e);

    internal sealed class MacroStateEventArgs : EventArgs
    {
        private readonly MacroState state;

        public MacroState State => state;

        public MacroStateEventArgs(MacroState state)
        {
            this.state = state;
        }
    }

    internal sealed class MacroStatusEventArgs : EventArgs
    {
        private readonly MacroStatus status;

        public MacroStatus Status => status;

        public MacroStatusEventArgs(MacroStatus status)
        {
            this.status = status;
        }
    }
}
