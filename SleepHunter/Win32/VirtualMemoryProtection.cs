using System;

namespace SleepHunter.Win32
{
  [Flags]
   internal enum VirtualMemoryProtection : uint
   {
      None = 0,
      Execute = 0x10,
      ExecuteRead = 0x20,
      ExecuteReadWrite = 0x40,
      ExecuteWriteCopy = 0x80,
      NoAccess = 0x01,
      ReadOnly = 0x02,
      ReadWrite = 0x4,
      WriteCopy = 0x8,
      Guard = 0x100,
      NoCache = 0x200,
      WriteCombine = 0x400
   }
}
