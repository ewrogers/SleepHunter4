using System;

namespace SleepHunter.Macro
{
    public delegate void MacroStateEventHandler(object sender, MacroStateEventArgs e);

    public sealed class MacroStateEventArgs : EventArgs
    {
        public MacroState State { get; }

        public MacroStateEventArgs(MacroState state)
            => State = state ?? throw new ArgumentNullException(nameof(state));
    }
}
