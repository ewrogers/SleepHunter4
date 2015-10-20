using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;

using SleepHunter.Data;
using SleepHunter.Settings;
using SleepHunter.Win32;

namespace SleepHunter.IO.Process
{
   public sealed class ClientProcess : NotifyObject
   {
      int processId;
      IntPtr windowHandle;
      string windowClassName = string.Empty;
      string windowTitle = string.Empty;
      int windowWidth = 640;
      int windowHeight = 480;

      public int ProcessId
      {
         get { return processId; }
         set { SetProperty(ref processId, value, "ProcessId"); }
      }

      public IntPtr WindowHandle
      {
         get { return windowHandle; }
         set { SetProperty(ref windowHandle, value, "WindowHandle"); }
      }

      public string WindowClassName
      {
         get { return windowClassName; }
         set { SetProperty(ref windowClassName, value, "WindowClassName"); }
      }

      public string WindowTitle
      {
         get { return windowTitle; }
         set { SetProperty(ref windowTitle, value, "WindowTitle"); }
      }

      public int WindowWidth
      {
         get { return windowWidth; }
         set { SetProperty(ref windowWidth, value, "WindowWidth", onChanged: (p) => { OnPropertyChanged("WindowScaleX"); }); }
      }

      public int WindowHeight
      {
         get { return windowHeight; }
         set { SetProperty(ref windowHeight, value, "WindowHeight", onChanged: (p) => { OnPropertyChanged("WindowScaleY"); }); }
      }

      public double WindowScaleX
      {
         get { return Math.Floor(this.WindowWidth / 640.0); }
      }

      public double WindowScaleY
      {
         get { return Math.Floor(this.WindowHeight / 480.0); }
      }

      public ClientProcess()
      {

      }

      public void Update()
      {
         var windowTextLength = NativeMethods.GetWindowTextLength(windowHandle);
         var windowTextBuffer = new StringBuilder(windowTextLength + 1);
         windowTextLength = NativeMethods.GetWindowText(windowHandle, windowTextBuffer, windowTextBuffer.Capacity);

         this.WindowTitle = windowTextBuffer.ToString(0, windowTextLength);

         Rect windowRect;

         if (NativeMethods.GetWindowRect(windowHandle, out windowRect))
         {
            this.WindowWidth = windowRect.Width;
            this.WindowHeight = windowRect.Height;
         }
      }
   }
}
