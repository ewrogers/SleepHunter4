using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace SleepHunter.Win32
{
   [StructLayout(LayoutKind.Explicit)]
   internal struct SystemInfo
   {
      [FieldOffset(0)]
      uint oemId;
      [FieldOffset(0)]
      ProcessorArchitecture processorArchitecture;
      [FieldOffset(2)]
      ushort reserved;

      [FieldOffset(4)]
      ushort pageSize;

      [FieldOffset(6)]
      IntPtr minimumApplicationAddress;

      [FieldOffset(10)]
      IntPtr maximumApplicationAddress;

      [FieldOffset(14)]
      uint activeProcessorMask;

      [FieldOffset(18)]
      uint numberOfProcessors;

      [FieldOffset(22)]
      ProcessorType processorType;

      [FieldOffset(26)]
      uint allocationGranularity;

      [FieldOffset(30)]
      ushort processorLevel;

      [FieldOffset(32)]
      ushort processorRevision;

      public uint OemId
      {
         get { return oemId; }
         set { oemId = value; }
      }

      public ProcessorArchitecture ProcessorArchitecture
      {
         get { return processorArchitecture; }
         set { processorArchitecture = value; }
      }

      public ushort Reserved
      {
         get { return reserved; }
         set { reserved = value; }
      }

      public ushort PageSize
      {
         get { return pageSize; }
         set { pageSize = value; }
      }

      public IntPtr MinimumApplicationAddress
      {
         get { return minimumApplicationAddress; }
         set { minimumApplicationAddress = value; }
      }

      public IntPtr MaximumApplicationAddress
      {
         get { return maximumApplicationAddress; }
         set { maximumApplicationAddress = value; }
      }

      public uint ActiveProcessorMask
      {
         get { return activeProcessorMask; }
         set { activeProcessorMask = value; }
      }

      public uint NumberOfProcessors
      {
         get { return numberOfProcessors; }
         set { numberOfProcessors = value; }
      }

      public ProcessorType ProcessorType
      {
         get { return processorType; }
         set { processorType = value; }
      }

      public uint AllocationGranularity
      {
         get { return allocationGranularity; }
         set { allocationGranularity = value; }
      }

      public ushort ProcessorLevel
      {
         get { return processorLevel; }
         set { processorLevel = value; }
      }

      public ushort ProcessorRevision
      {
         get { return processorRevision; }
         set { processorRevision = value; }
      }
   }
}
