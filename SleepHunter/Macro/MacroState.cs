using System;
using System.Threading;
using System.Threading.Tasks;

using SleepHunter.Common;
using SleepHunter.Models;
using SleepHunter.Settings;

namespace SleepHunter.Macro
{
    public enum MacroStatus
   {
      Idle,
      Running,
      Paused,
      Stopped,
      Error = -1
   }

   public abstract class MacroState : ObservableObject, IDisposable
   {
      bool isDisposed;
      protected string name;
      protected Player client;
      protected MacroStatus status;
      protected CancellationTokenSource cancelSource;
      protected Task task;
      protected int lastKnownMapNumber;
      protected string lastKnownMapName;
      protected int lastKnownXCoordinate;
      protected int lastKnownYCoordinate;

      public event MacroStatusEventHandler StatusChanged;

      public string Name
      {
         get { return name; }
         set { SetProperty(ref name, value, "Name"); }
      }

      public Player Client
      {
         get { return client; }
         set { SetProperty(ref client, value, "Client"); }
      }

      public MacroStatus Status
      {
         get { return status; }
         set { SetProperty(ref status, value, "Status"); }
      }

      public MacroState(Player client)
      {
         if (client == null)
            throw new ArgumentNullException("client");

         this.client = client;
      }

      #region IDisposable Methods
      public void Dispose()
      {
         Dispose(true);
         GC.SuppressFinalize(this);
      }

      protected virtual void Dispose(bool isDisposing)
      {
         if (isDisposed) return;

         if (isDisposing)
         {
            CancelTask();
         }

         isDisposed = true;
      }
      #endregion

      public void Start()
      {
         if (this.Status == MacroStatus.Running)
            return;

         SaveKnownState();

         if (this.Status == MacroStatus.Paused)
            ResumeMacro();
         else
            StartMacro();

         this.Status = MacroStatus.Running;
         OnStatusChanged(this.Status);
      }

      public void Stop()
      {
         if (this.Status == MacroStatus.Stopped)
            return;

         StopMacro();

         this.Status = MacroStatus.Stopped;
         OnStatusChanged(this.Status);
      }

      public void Pause()
      {
         if (this.Status == MacroStatus.Paused)
            return;

         PauseMacro();

         this.Status = MacroStatus.Paused;
         OnStatusChanged(this.Status);
      }

      protected virtual void StartMacro(object state = null)
      {
         cancelSource = new CancellationTokenSource();
         task = Task.Factory.StartNew((arg) =>
         {
            while (true)
            {
               try
               {
                  if (!CheckMap())
                     cancelSource.Cancel();

                  if (cancelSource.Token.IsCancellationRequested)
                     break;

                  if (this.Status == MacroStatus.Running)
                  {
                     CheckKnownState();
                     MacroLoop(arg);
                  }
                  else Thread.Sleep(100);
               }
               finally
               {
                  if (!cancelSource.IsCancellationRequested)
                     Thread.Sleep(50);

                  if (cancelSource.IsCancellationRequested)
                     this.Stop();

                  if (!client.IsLoggedIn)
                     this.Stop();
               }
            }

         }, state, cancelSource.Token);
      }

      protected virtual void ResumeMacro()
      {

      }

      protected virtual void StopMacro()
      {
         CancelTask();
      }

      protected virtual void PauseMacro()
      {

      }

      protected abstract void MacroLoop(object argument);

      protected virtual bool CheckMap()
      {
         if (client == null) 
            return false;

            client.Update(PlayerFieldFlags.Location);
            return true;
      }

      protected virtual bool CancelTask(bool waitForTask = false)
      {
         if (cancelSource != null)
            cancelSource.Cancel();
         else
            return false;

         if (task != null && waitForTask && !task.IsCompleted)
            task.Wait();

         return true;
      }

      protected virtual void OnStatusChanged(MacroStatus status)
      {
         if (status == MacroStatus.Idle || status == MacroStatus.Stopped)
            client.Status = null;
         else
            client.Status = status.ToString();

         var handler = this.StatusChanged;

         if (handler != null)
            handler(this, new MacroStatusEventArgs(status));
      }

      protected virtual void OnMapChanged()
      {
         var action = UserSettingsManager.Instance.Settings.MapChangeAction;
         TakeAction(action);
      }

      protected virtual void OnXYChanged()
      {
         var action = UserSettingsManager.Instance.Settings.CoordsChangeAction;
         TakeAction(action);
      }

      protected virtual void TakeAction(MacroAction action)
      {
         switch (action)
         {
            case MacroAction.Start: Start(); break;
            case MacroAction.Resume: Start(); break;
            case MacroAction.Restart: Stop(); Start(); break;
            case MacroAction.Pause: Pause(); break;
            case MacroAction.Stop: Stop(); break;
            case MacroAction.ForceQuit: Stop(); client.Terminate(); break;
         }
      }

      protected virtual void SaveKnownState()
      {
         client.Update(PlayerFieldFlags.Location);

         lastKnownMapName = client.Location.MapName;
         lastKnownMapNumber = client.Location.MapNumber;
         lastKnownXCoordinate = client.Location.X;
         lastKnownYCoordinate = client.Location.Y;
      }

      protected virtual void CheckKnownState(bool saveStateAfterCheck = true)
      {
         client.Update(PlayerFieldFlags.Location);
         
         if (!string.Equals(client.Location.MapName, lastKnownMapName) ||
            client.Location.MapNumber != lastKnownMapNumber)
         {
            OnMapChanged();
         }

         if (client.Location.X != lastKnownXCoordinate ||
            client.Location.Y != lastKnownYCoordinate)
         {
            OnXYChanged();
         }

         if (saveStateAfterCheck)
         {
            lastKnownMapName = client.Location.MapName;
            lastKnownMapNumber = client.Location.MapNumber;
            lastKnownXCoordinate = client.Location.X;
            lastKnownYCoordinate = client.Location.Y;
         }
      }
   }
}
