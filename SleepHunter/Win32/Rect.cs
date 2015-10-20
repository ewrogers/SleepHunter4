using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace SleepHunter.Win32
{
   [StructLayout(LayoutKind.Sequential)]
   internal struct Rect
   {
      int left;
      int top;
      int right;
      int bottom;

      public int Left
      {
         get { return left; }
         set { left = value; }
      }

      public int Top
      {
         get { return top; }
         set { top = value; }
      }

      public int Right
      {
         get { return right; }
         set { right = value; }
      }

      public int Bottom
      {
         get { return bottom; }
         set { bottom = value; }
      }

      public int Width
      {
         get { return Math.Abs(right - left); }
      }

      public int Height
      {
         get { return Math.Abs(bottom - top); }
      }
   }
}
