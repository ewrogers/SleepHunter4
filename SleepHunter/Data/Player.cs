using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

using SleepHunter.IO;
using SleepHunter.IO.Process;
using SleepHunter.Media;
using SleepHunter.Macro;
using SleepHunter.Metadata;
using SleepHunter.Settings;

namespace SleepHunter.Data
{
   [Flags]
   public enum PlayerFieldFlags : uint
   {
      None = 0x0,
      Name = 0x1,
      Guild = 0x2,
      GuildRank = 0x4,
      Title = 0x8,
      Inventory = 0x10,
      Equipment = 0x20,
      Skillbook = 0x40,
      Spellbook = 0x80,
      Stats = 0x100,
      Modifiers = 0x200,
      Location = 0x400,
      GameClient = 0x800,
      Status = 0x1000,
      Window = 0x2000,

      All = 0xFFFFFFFF
   }

   public sealed class Player : NotifyObject, IDisposable
   {
      static readonly string CharacterNameKey = @"CharacterName";

      bool isDisposed;
      ClientProcess process;
      ClientVersion version;
      ProcessMemoryAccessor accessor;
      string name;
      string guild;
      string guildRank;
      string title;
      PlayerClass playerClass;
      Inventory inventory;
      EquipmentSet equipment;
      Skillbook skillbook;
      Spellbook spellbook;
      PlayerStats stats;
      PlayerModifiers modifiers;
      MapLocation location;
      ClientState gameClient;
      bool isLoggedIn;
      string status;
      Hotkey hotkey;
      int selectedTabIndex;
      double? skillbookScrollPosition;
      double? spellbookScrollPosition;
      double? spellQueueScrollPosition;
      double? flowerScrollPosition;
      bool hasLyliacPlant;
      bool hasLyliacVineyard;
      bool hasFasSpiorad;
      DateTime lastFlowerTimestamp;

      public ClientProcess Process
      {
         get { return process; }
         private set { SetProperty(ref process, value, "Process"); }
      }

      public ClientVersion Version
      {
         get { return version; }
         set { SetProperty(ref version, value, "Version"); }
      }

      public IntPtr ProcessHandle
      {
         get { return accessor.ProcessHandle; }
      }

      public ProcessMemoryAccessor Accessor
      {
         get { return accessor; }
      }

      public string Name
      {
         get { return name; }
         set { SetProperty(ref name, value, "Name"); }
      }

      public string Guild
      {
         get { return guild; }
         set { SetProperty(ref guild, value, "Guild"); }
      }

      public string GuildRank
      {
         get { return guildRank; }
         set { SetProperty(ref guildRank, value, "GuildRank"); }
      }

      public string Title
      {
         get { return title; }
         set { SetProperty(ref title, value, "Title"); }
      }

      public PlayerClass Class
      {
         get { return playerClass; }
         set { SetProperty(ref playerClass, value, "Class"); }
      }

      public Inventory Inventory
      {
         get { return inventory; }
         private set { SetProperty(ref inventory, value, "Inventory"); }
      }

      public EquipmentSet Equipment
      {
         get { return equipment; }
         set { SetProperty(ref equipment, value, "Equipment"); }
      }

      public Skillbook Skillbook
      {
         get { return skillbook; }
         private set { SetProperty(ref skillbook, value, "Skillbook"); }
      }

      public Spellbook Spellbook
      {
         get { return spellbook; }
         private set { SetProperty(ref spellbook, value, "Spellbook"); }
      }

      public PlayerStats Stats
      {
         get { return stats; }
         private set { SetProperty(ref stats, value, "Stats"); }
      }

      public PlayerModifiers Modifiers
      {
         get { return modifiers; }
         private set { SetProperty(ref modifiers, value, "Modifiers"); }
      }

      public MapLocation Location
      {
         get { return location; }
         private set { SetProperty(ref location, value, "Location"); }
      }

      public ClientState GameClient
      {
         get { return gameClient; }
         private set { SetProperty(ref gameClient, value, "GameClient"); }
      }

      public bool IsLoggedIn
      {
         get { return isLoggedIn; }
         set { SetProperty(ref isLoggedIn, value, "IsLoggedIn"); }
      }

      public string Status
      {
         get { return status; }
         set { SetProperty(ref status, value, "Status"); }
      }

      public string HotkeyString
      {
         get { return hotkey != null ? hotkey.ToString() : null; }
      }

      public Hotkey Hotkey
      {
         get { return hotkey; }
         set { SetProperty(ref hotkey, value, "Hotkey", onChanged: (playerClass) => { OnPropertyChanged("HotkeyStrike"); OnPropertyChanged("HasHotkey"); }); }
      }

      public bool HasHotkey
      {
         get { return !string.IsNullOrWhiteSpace(this.HotkeyString); }
      }

      public int SelectedTabIndex
      {
         get { return selectedTabIndex; }
         set { SetProperty(ref selectedTabIndex, value, "SelectedTabIndex"); }
      }

      public double? SkillbookScrollPosition
      {
         get { return skillbookScrollPosition; }
         set { SetProperty(ref skillbookScrollPosition, value, "SkillbookScrollPosition"); }
      }

      public double? SpellbookScrollPosition
      {
         get { return spellbookScrollPosition; }
         set { SetProperty(ref spellbookScrollPosition, value, "SpellbookScrollPosition"); }
      }

      public double? SpellQueueScrollPosition
      {
         get { return spellQueueScrollPosition; }
         set { SetProperty(ref spellQueueScrollPosition, value, "SpellQueueScrollPosition"); }
      }

      public double? FlowerScrollPosition
      {
         get { return flowerScrollPosition; }
         set { SetProperty(ref flowerScrollPosition, value, "FlowerScrollPosition"); }
      }

      public bool HasLyliacPlant
      {
         get { return hasLyliacPlant; }
         set { SetProperty(ref hasLyliacPlant, value, "HasLyliacPlant"); }
      }

      public bool HasLyliacVineyard
      {
         get { return hasLyliacVineyard; }
         set { SetProperty(ref hasLyliacVineyard, value, "HasLyliacVineyard"); }
      }

      public bool HasFasSpiorad
      {
         get { return hasFasSpiorad; }
         set { SetProperty(ref hasFasSpiorad, value, "HasFasSpiorad"); }
      }

      public DateTime LastFlowerTimestamp
      {
         get { return lastFlowerTimestamp; }
         set { SetProperty(ref lastFlowerTimestamp, value, "LastFlowerTimestamp", onChanged: (p) => { OnPropertyChanged("TimeSinceFlower"); }); }
      }

      public TimeSpan TimeSinceFlower
      {
         get { return DateTime.Now - lastFlowerTimestamp; }
      }

      public Player()
         : this(null) { }

      public Player(ClientProcess process)
      {
         this.process = process;
         accessor = new ProcessMemoryAccessor(process.ProcessId, ProcessAccess.Read);
         inventory = new Inventory(this);
         equipment = new EquipmentSet(this);
         skillbook = new Skillbook(this);
         spellbook = new Spellbook(this);
         stats = new PlayerStats(this);
         modifiers = new PlayerModifiers(this);
         location = new MapLocation(this);
         gameClient = new ClientState(this);
      }

      ~Player()
      {
         Dispose(false);
      }

      #region IDisposable Methods
      public void Dispose()
      {
         Dispose(true);
         GC.SuppressFinalize(this);
      }

      void Dispose(bool isDisposing)
      {
         if (isDisposed)
            return;

         if (isDisposing)
         {
            if (accessor != null)
               accessor.Dispose();

            accessor = null;
         }

         isDisposed = true;
      }
      #endregion

      public void Update(PlayerFieldFlags updateFields = PlayerFieldFlags.All)
      {
         if (version == null)
            this.Version = ClientState.DetectVersion(accessor);

         if (version != null)
         {
            this.GameClient.VersionNumber = this.Version.VersionNumber;
            this.GameClient.VersionKey = this.Version.Key;
         }

         try
         {
            if (updateFields.HasFlag(PlayerFieldFlags.Name))
               UpdateName(accessor);

            if (updateFields.HasFlag(PlayerFieldFlags.Guild))
               UpdateGuild(accessor);

            if (updateFields.HasFlag(PlayerFieldFlags.GuildRank))
               UpdateGuildRank(accessor);

            if (updateFields.HasFlag(PlayerFieldFlags.Title))
               UpdateTitle(accessor);

            if (updateFields.HasFlag(PlayerFieldFlags.Inventory))
               inventory.Update(accessor);

            if (updateFields.HasFlag(PlayerFieldFlags.Equipment))
               equipment.Update(accessor);

            if (updateFields.HasFlag(PlayerFieldFlags.Skillbook))
               skillbook.Update(accessor);

            if (updateFields.HasFlag(PlayerFieldFlags.Spellbook))
               spellbook.Update(accessor);

            if (updateFields.HasFlag(PlayerFieldFlags.Stats))
               stats.Update(accessor);

            if (updateFields.HasFlag(PlayerFieldFlags.Modifiers))
               modifiers.Update(accessor);

            if (updateFields.HasFlag(PlayerFieldFlags.Location))
               location.Update(accessor);

            if (updateFields.HasFlag(PlayerFieldFlags.GameClient))
               gameClient.Update(accessor);

            if (updateFields.HasFlag(PlayerFieldFlags.Window))
               process.Update();
         }
         catch { }
         finally
         {
            this.IsLoggedIn = !string.IsNullOrWhiteSpace(this.Name) && stats.Level > 0;
         }
      }

      void UpdateName(ProcessMemoryAccessor accessor)
      {         
         if (accessor == null)
            throw new ArgumentNullException("accessor");

         string name = null;

         if (version != null && version.ContainsVariable(CharacterNameKey))
         {
            using (var stream = accessor.GetStream())
            using (var reader = new BinaryReader(stream, Encoding.ASCII))
            {
               var nameVariable = version.GetVariable(CharacterNameKey);
               nameVariable.TryReadString(reader, out name);
            }
         }

         if (!string.IsNullOrWhiteSpace(name))
            this.Name = name;
      }

      void UpdateGuild(ProcessMemoryAccessor accessor)
      {
         if (accessor == null)
            throw new ArgumentNullException("accessor");
      }

      void UpdateGuildRank(ProcessMemoryAccessor accessor)
      {
         if (accessor == null)
            throw new ArgumentNullException("accessor");
      }

      void UpdateTitle(ProcessMemoryAccessor accessor)
      {
         if (accessor == null)
            throw new ArgumentNullException("accessor");
      }

      public override string ToString()
      {
         return this.Name ?? string.Format("Process {0}", this.Process.ProcessId.ToString());
      }
   }
}
