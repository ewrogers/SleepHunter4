using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

using SleepHunter.IO;
using SleepHunter.IO.Process;
using SleepHunter.Settings;

namespace SleepHunter.Data
{
   public sealed class ClientState : NotifyObject
   {
      static readonly string ActivePanelKey = @"ActivePanel";
      static readonly string InventoryExpandedKey = @"InventoryExpanded";
      static readonly string MinimizedModeKey = @"MinimizedMode";
      static readonly string DialogOpenKey = @"DialogOpen";
      static readonly string UserChattingKey = @"UserChatting";

      Player owner;
      int versionNumber;
      string versionKey;
      InterfacePanel activePanel;
      bool isInventoryExpanded;
      bool isMinimizedMode;
      bool isDialogOpen;
      bool isUserChatting;

      public Player Owner
      {
         get { return owner; }
         set { SetProperty(ref owner, value, "Owner"); }
      }

      public int VersionNumber
      {
         get { return versionNumber; }
         set { SetProperty(ref versionNumber, value, "VersionNumber"); }
      }

      public string VersionKey
      {
         get { return versionKey; }
         set { SetProperty(ref versionKey, value, "VersionKey"); }
      }

      public InterfacePanel ActivePanel
      {
         get { return activePanel; }
         set { SetProperty(ref activePanel, value, "ActivePanel"); }
      }

      public bool IsInventoryExpanded
      {
         get { return isInventoryExpanded; }
         set { SetProperty(ref isInventoryExpanded, value, "IsInventoryExpanded"); }
      }

      public bool IsMinimizedMode
      {
         get { return isMinimizedMode; }
         set { SetProperty(ref isMinimizedMode, value, "IsMinimizedMode"); }
      }

      public bool IsDialogOpen
      {
         get { return isDialogOpen; }
         set { SetProperty(ref isDialogOpen, value, "IsDialogOpen"); }
      }

      public bool IsUserChatting
      {
         get { return isUserChatting; }
         set { SetProperty(ref isUserChatting, value, "IsUserChatting"); }
      }

      public ClientState()
         : this(null) { }

      public ClientState(Player owner)
      {
         this.owner = owner;
      }

      public void Update()
      {
         if (owner == null)
            throw new InvalidOperationException("Player owner is null, cannot update.");

         Update(owner.Accessor);
      }

      public void Update(ProcessMemoryAccessor accessor)
      {
         if (accessor == null)
            throw new ArgumentNullException("accessor");

         var version = this.Owner.Version;

         if (version == null)
         {
            ResetDefaults();
            return;
         }

         var activePanelVariable = version.GetVariable(ActivePanelKey);
         var inventoryExpandedVariable = version.GetVariable(InventoryExpandedKey);
         var minimizedModeVariable = version.GetVariable(MinimizedModeKey);
         var dialogOpenVariable = version.GetVariable(DialogOpenKey) as SearchMemoryVariable;
         var userChattingVariable = version.GetVariable(UserChattingKey);

         byte activePanelByte;
         bool isInventoryExpanded;
         bool isMinimizedMode;
         bool isDialogOpen;
         bool isUserChatting;
         
         using(var stream = accessor.GetStream())
         using (var reader = new BinaryReader(stream, Encoding.ASCII))
         {
            if (activePanelVariable != null && activePanelVariable.TryReadByte(reader, out activePanelByte))
               this.ActivePanel = (InterfacePanel)activePanelByte;
            else
               this.ActivePanel = InterfacePanel.Unknown;

            if (inventoryExpandedVariable != null && inventoryExpandedVariable.TryReadBoolean(reader, out isInventoryExpanded))
               this.IsInventoryExpanded = isInventoryExpanded;
            else
               this.IsInventoryExpanded = false;

            if (minimizedModeVariable != null && minimizedModeVariable.TryReadBoolean(reader, out isMinimizedMode))
               this.IsMinimizedMode = isMinimizedMode;
            else
               this.IsMinimizedMode = false;

            if (dialogOpenVariable != null && dialogOpenVariable.TryReadBoolean(reader, out isDialogOpen))
               this.IsDialogOpen = isDialogOpen;
            else
               this.IsDialogOpen = false;

            if (userChattingVariable != null && userChattingVariable.TryReadBoolean(reader, out isUserChatting))
               this.IsUserChatting = isUserChatting;
            else
               this.IsUserChatting = false;
         }
      }

      public void ResetDefaults()
      {
         this.ActivePanel = InterfacePanel.Unknown;
         this.IsInventoryExpanded = false;
         this.IsMinimizedMode = false;
         this.IsDialogOpen = false;
         this.IsUserChatting = false;
      }

      public static ClientVersion DetectVersion(ProcessMemoryAccessor accessor)
      {
         ClientVersion detectedVersion = null;

         if (accessor == null)
            throw new ArgumentNullException("accessor");

         using (var stream = accessor.GetStream())
         using (var reader = new BinaryReader(stream, Encoding.ASCII))
         {
            foreach (var version in ClientVersionManager.Instance.Versions)
            {
               ushort versionNumber;
               var versionVariable = version.GetVariable("VersionNumber");

               if (versionVariable == null) continue;

               if (!versionVariable.TryReadUInt16(reader, out versionNumber))
                  continue;

               if (versionNumber == version.VersionNumber)
               {
                  detectedVersion = version;
                  break;
               }
            }
         }

         return detectedVersion;
      }
   }
}
