using System;
using System.Runtime.InteropServices;

namespace SleepHunter.Win32
{
  [StructLayout(LayoutKind.Sequential)]
   internal struct StartupInfo
   {
      int size;
      string reserved;
      string desktop;
      string title;
      int x;
      int y;
      int width;
      int height;
      int consoleWidth;
      int consoleHeight;
      int fillAttribute;
      int flags;
      short showWindow;
      short reserved2;
      IntPtr reserved3;
      IntPtr standardInput;
      IntPtr standardOutput;
      IntPtr standardError;

      public int Size
      {
         get { return size; }
         set { size = value; }
      }

      public string Reserved { get { return reserved; } }

      public string Desktop
      {
         get { return desktop; }
         set { desktop = value; }
      }

      public string Title
      {
         get { return title; }
         set { title = value; }
      }

      public int X
      {
         get { return x; }
         set { x = value; }
      }

      public int Y
      {
         get { return y; }
         set { y = value; }
      }

      public int Width
      {
         get { return width; }
         set { width = value; }
      }

      public int Height
      {
         get { return height; }
         set { height = value; }
      }

      public int ConsoleWidth
      {
         get { return consoleWidth; }
         set { consoleWidth = value; }
      }

      public int ConsoleHeight
      {
         get { return consoleHeight; }
         set { consoleHeight = value; }
      }

      public int FillAttribute
      {
         get { return fillAttribute; }
         set { fillAttribute = value; }
      }

      public int Flags
      {
         get { return flags; }
         set { flags = value; }
      }

      public short ShowWindow
      {
         get { return showWindow; }
         set { showWindow = value; }
      }

      public short Reserved2 { get { return reserved2; } }

      public IntPtr Reserved3 { get { return reserved3; } }

      public IntPtr StandardInput
      {
         get { return standardInput; }
         set { standardInput = value; }
      }

      public IntPtr StandardOutput
      {
         get { return standardOutput; }
         set { standardOutput = value; }
      }

      public IntPtr StandardError
      {
         get { return standardError; }
         set { standardError = value; }
      }
   }
}
