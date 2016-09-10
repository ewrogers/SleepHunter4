using System;

namespace SleepHunter.Macro
{
  public delegate void MacroStateEventHandler(object sender, MacroStateEventArgs e);

  public delegate void MacroStatusEventHandler(object sender, MacroStatusEventArgs e);

  public sealed class MacroStateEventArgs : EventArgs
  {
    readonly MacroState state;

    public MacroStateEventArgs(MacroState state)
    {
      this.state = state;
    }
  }

  public sealed class MacroStatusEventArgs : EventArgs
  {
    readonly MacroStatus status;

    public MacroStatus Status { get { return status; } }

    public MacroStatusEventArgs(MacroStatus status)
    {
      this.status = status;
    }
  }
}
