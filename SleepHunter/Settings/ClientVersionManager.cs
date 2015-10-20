﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Serialization;

using SleepHunter.IO.Process;

namespace SleepHunter.Settings
{
   public sealed class ClientVersionManager
   {
      public static readonly string VersionsFile = @"Versions.xml";

      #region Singleton
      static readonly ClientVersionManager instance = new ClientVersionManager();

      public static ClientVersionManager Instance { get { return instance; } }

      private ClientVersionManager()
      {
         if (!clientVersions.ContainsKey("Auto-Detect"))
            AddVersion(ClientVersion.AutoDetect);
      }
      #endregion

      readonly ConcurrentDictionary<string, ClientVersion> clientVersions = new ConcurrentDictionary<string, ClientVersion>(StringComparer.OrdinalIgnoreCase);

      public event ClientVersionEventHandler VersionAdded;
      public event ClientVersionEventHandler VersionChanged;
      public event ClientVersionEventHandler VersionRemoved;

      #region Collection Properties
      public ClientVersion this[string key]
      {
         get { return GetVersion(key); }
         set { AddVersion(value); }
      }

      public int Count { get { return clientVersions.Count; } }

      public IEnumerable<ClientVersion> Versions
      {
         get { return from v in clientVersions.Values orderby v.Key select v; }
      }
      #endregion

      #region Collection Methods
      public void AddVersion(ClientVersion version)
      {
         if (version == null)
            throw new ArgumentNullException("version");

         if (string.IsNullOrWhiteSpace(version.Key))
            throw new ArgumentException("Key cannot be null or whitespace.", "version");

         bool alreadyExists = clientVersions.ContainsKey(version.Key);

         clientVersions[version.Key] = version;

         if (alreadyExists)
            OnVersionChanged(version);
         else
            OnVersionAdded(version);
      }

      public ClientVersion GetVersion(string key)
      {
         if (key == null)
            throw new ArgumentNullException("key");

         return clientVersions[key];
      }

      public bool ContainsVersion(string key)
      {
         if (key == null)
            throw new ArgumentNullException("key");

         return clientVersions.ContainsKey(key);
      }

      public bool RemoveVersion(string key)
      {
         if (key == null)
            throw new ArgumentNullException("key");

         if (!clientVersions.ContainsKey(key))
            return false;

         ClientVersion removedVersion;
         bool wasRemoved = clientVersions.TryRemove(key, out removedVersion);

         if (wasRemoved)
            OnVersionRemoved(removedVersion);

         return wasRemoved;
      }

      public void ClearVersions()
      {
         foreach (var version in clientVersions.Values)
            OnVersionRemoved(version);

         clientVersions.Clear();
      }
      #endregion

      #region Load/Save Methods
      public void LoadFromFile(string filename)
      {
         using (var inputStream = File.Open(filename, FileMode.Open, FileAccess.Read, FileShare.Read))
         {
            LoadFromStream(inputStream);
            inputStream.Close();
         }
      }

      public void LoadFromStream(Stream stream)
      {
         var serializer = new XmlSerializer(typeof(ClientVersionCollection));
         var collection = serializer.Deserialize(stream) as ClientVersionCollection;

         if (collection != null)
            foreach (var version in collection.Versions)
               AddVersion(version);
      }

      public void SaveToFile(string filename)
      {
         using (var outputStream = File.Create(filename))
         {
            SaveToStream(outputStream);
            outputStream.Flush();
            outputStream.Close();
         }
      }

      public void SaveToStream(Stream stream)
      {
         var collection = new ClientVersionCollection(this.Versions);
         var serializer = new XmlSerializer(typeof(ClientVersionCollection));
         var namespaces = new XmlSerializerNamespaces();
         namespaces.Add("", "");

         serializer.Serialize(stream, collection, namespaces);
      }

      public void LoadDefaultVersions()
      {
         if (!clientVersions.ContainsKey("Auto-Detect"))
            AddVersion(ClientVersion.AutoDetect);

         var version = new ClientVersion("7.39");
         version.Hash = @"CA31B8165EA7409D285D81616D8CA4F2";
         version.VersionNumber = 739;
         version.MultipleInstanceAddress = 0x5911A9;
         version.IntroVideoAddress = 0x42F495;
         version.NoWallAddress = 0x624BD5;
         version.Variables.Add(new MemoryVariable("VersionNumber", 0x772534)); 
         version.Variables.Add(new MemoryVariable("CharacterName", 0x75F890, maxLength: 13));
         version.Variables.Add(new DynamicMemoryVariable("CurrentHealth", 0x777A24, maxLength: 10, offsets: 0x4C6));
         version.Variables.Add(new DynamicMemoryVariable("MaximumHealth", 0x777A24, maxLength: 10, offsets: 0x546));
         version.Variables.Add(new DynamicMemoryVariable("CurrentMana", 0x777A24, maxLength: 10, offsets: 0x5C6));
         version.Variables.Add(new DynamicMemoryVariable("MaximumMana", 0x777A24, maxLength: 10, offsets: 0x646));

         version.Variables.Add(new DynamicMemoryVariable("Level", 0x777A24, maxLength: 3, offsets: 0x8C6));
         version.Variables.Add(new DynamicMemoryVariable("AbilityLevel", 0x777A24, maxLength: 3, offsets: 0x9C6));

         version.Variables.Add(new DynamicMemoryVariable("MapName", 0x84D6E4, maxLength: 32, offsets: 0x4E40));
         version.Variables.Add(new DynamicMemoryVariable("MapNumber", 0x8A4DE0, offsets: 0x26C));
         version.Variables.Add(new DynamicMemoryVariable("MapX", 0x8A4DE0, offsets: 0x23C));
         version.Variables.Add(new DynamicMemoryVariable("MapY", 0x8A4DE0, offsets: 0x238));

         version.Variables.Add(new DynamicMemoryVariable("Inventory", 0x8A4DE0, maxLength: 256, size: 261, count: 60, offsets: new long[] { 0x2CC, 0x1092 }));
         version.Variables.Add(new DynamicMemoryVariable("Equipment", 0x71E894, maxLength: 128, size: 261, count: 18, offsets: 0x1152));
         version.Variables.Add(new DynamicMemoryVariable("Skillbook", 0x8A4DE0, maxLength: 256, size: 260, count: 90, offsets: new long[] { 0x2CC, 0x10210 }));
         version.Variables.Add(new DynamicMemoryVariable("Spellbook", 0x8A4DE0, maxLength: 256, size: 518, count: 90, offsets: new long[] { 0x2CC, 0x4DFA }));

         version.Variables.Add(new SearchMemoryVariable("SkillCooldowns", 0x69B43C, 0x194, size: 4, offsets: 0x322));
         
         version.Variables.Add(new DynamicMemoryVariable("ActivePanel", 0x84D6E4, offsets: 0x4FA8));
         version.Variables.Add(new DynamicMemoryVariable("InventoryExpanded", 0x84D6E4, offsets: 0x4FB0));
         version.Variables.Add(new DynamicMemoryVariable("MinimizedMode", 0x84D6E4, offsets: 0x4DF0));
         version.Variables.Add(new DynamicMemoryVariable("DialogOpen", 0x873104, offsets: new long[] { 0x59C, 0x594, 0x18, 0x24, 0xA20 }));
         version.Variables.Add(new DynamicMemoryVariable("UserChatting", 0x70D098, offsets: 0x438));

         AddVersion(version);
      }
      #endregion

      public string DetectVersion(string hash)
      {
         foreach (var version in clientVersions.Values)
            if (string.Equals(version.Hash, hash, StringComparison.OrdinalIgnoreCase))
               return version.Key;

         return null;
      }

      #region Event Handler Methods
      void OnVersionAdded(ClientVersion version)
      {
         if (version == null)
            throw new ArgumentNullException("version");

         var handler = this.VersionAdded;

         if (handler != null)
            handler(this, new ClientVersionEventArgs(version));
      }

      void OnVersionChanged(ClientVersion version)
      {
         if (version == null)
            throw new ArgumentNullException("version");

         var handler = this.VersionChanged;

         if (handler != null)
            handler(this, new ClientVersionEventArgs(version));
      }

      void OnVersionRemoved(ClientVersion version)
      {
         if (version == null)
            throw new ArgumentNullException("version");

         var handler = this.VersionRemoved;

         if (handler != null)
            handler(this, new ClientVersionEventArgs(version));
      }
      #endregion
   }
}