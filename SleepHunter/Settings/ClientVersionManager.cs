using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Serialization;

using SleepHunter.IO.Process;

namespace SleepHunter.Settings
{
    internal sealed class ClientVersionManager
    {
        public static readonly string VersionsFile = @"Versions.xml";

        private static readonly ClientVersionManager instance = new ClientVersionManager();

        public static ClientVersionManager Instance { get { return instance; } }

        private ClientVersionManager()
        {
            if (!clientVersions.ContainsKey("Auto-Detect"))
                AddVersion(ClientVersion.AutoDetect);
        }

        private readonly ConcurrentDictionary<string, ClientVersion> clientVersions = new ConcurrentDictionary<string, ClientVersion>(StringComparer.OrdinalIgnoreCase);

        public event ClientVersionEventHandler VersionAdded;
        public event ClientVersionEventHandler VersionChanged;
        public event ClientVersionEventHandler VersionRemoved;

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

        public void AddVersion(ClientVersion version)
        {
            if (version == null)
                throw new ArgumentNullException(nameof(version));

            if (string.IsNullOrWhiteSpace(version.Key))
                throw new ArgumentException("Key cannot be null or whitespace.", nameof(version));

            var alreadyExists = clientVersions.ContainsKey(version.Key);

            clientVersions[version.Key] = version;

            if (alreadyExists)
                OnVersionChanged(version);
            else
                OnVersionAdded(version);
        }

        public ClientVersion GetVersion(string key)
        {
            if (key == null)
                throw new ArgumentNullException(nameof(key));

            return clientVersions[key];
        }

        public bool ContainsVersion(string key)
        {
            if (key == null)
                throw new ArgumentNullException(nameof(key));

            return clientVersions.ContainsKey(key);
        }

        public bool RemoveVersion(string key)
        {
            if (key == null)
                throw new ArgumentNullException(nameof(key));

            if (!clientVersions.ContainsKey(key))
                return false;

            var wasRemoved = clientVersions.TryRemove(key, out var removedVersion);

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

        public void LoadFromFile(string filename)
        {
            using (var inputStream = File.Open(filename, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                LoadFromStream(inputStream);
            }
        }

        public void LoadFromStream(Stream stream)
        {
            var serializer = new XmlSerializer(typeof(ClientVersionCollection));

            if (serializer.Deserialize(stream) is ClientVersionCollection collection)
                foreach (var version in collection.Versions)
                    AddVersion(version);
        }

        public void SaveToFile(string filename)
        {
            using (var outputStream = File.Create(filename))
            {
                SaveToStream(outputStream);
                outputStream.Flush();
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

            var version = new ClientVersion("7.41")
            {
                Hash = @"3244DC0E68CD26F4FB1626DA3673FDA8",
                VersionNumber = 741,
                MultipleInstanceAddress = 0x57A7CE,
                IntroVideoAddress = 0x42E61F,
                NoWallAddress = 0x5FD874
            };

            version.Variables.Add(new MemoryVariable("CharacterName", 0x73D910, maxLength: 13));
            version.Variables.Add(new DynamicMemoryVariable("CurrentHealth", 0x755AA4, maxLength: 10, offsets: 0x4C6));
            version.Variables.Add(new DynamicMemoryVariable("MaximumHealth", 0x755AA4, maxLength: 10, offsets: 0x546));
            version.Variables.Add(new DynamicMemoryVariable("CurrentMana", 0x755AA4, maxLength: 10, offsets: 0x5C6));
            version.Variables.Add(new DynamicMemoryVariable("MaximumMana", 0x755AA4, maxLength: 10, offsets: 0x646));

            version.Variables.Add(new DynamicMemoryVariable("Level", 0x755AA4, maxLength: 3, offsets: 0x8C6));
            version.Variables.Add(new DynamicMemoryVariable("AbilityLevel", 0x755AA4, maxLength: 3, offsets: 0x9C6));

            version.Variables.Add(new DynamicMemoryVariable("MapName", 0x82B76C, maxLength: 32, offsets: 0x4E3C));
            version.Variables.Add(new DynamicMemoryVariable("MapNumber", 0x882E68, offsets: 0x26C));
            version.Variables.Add(new DynamicMemoryVariable("MapX", 0x882E68, offsets: 0x23C));
            version.Variables.Add(new DynamicMemoryVariable("MapY", 0x882E68, offsets: 0x238));

            version.Variables.Add(new DynamicMemoryVariable("Inventory", 0x882E68, maxLength: 256, size: 261, count: 60, offsets: new long[] { 0x2CC, 0x1092 }));
            version.Variables.Add(new DynamicMemoryVariable("Equipment", 0x6FC914, maxLength: 128, size: 261, count: 18, offsets: 0x1152));
            version.Variables.Add(new DynamicMemoryVariable("Skillbook", 0x882E68, maxLength: 256, size: 260, count: 90, offsets: new long[] { 0x2CC, 0x10210 }));
            version.Variables.Add(new DynamicMemoryVariable("Spellbook", 0x882E68, maxLength: 256, size: 518, count: 90, offsets: new long[] { 0x2CC, 0x4DFA }));

            version.Variables.Add(new SearchMemoryVariable("SkillCooldowns", 0x67639C, 0x194, size: 4, offsets: 0x322));

            version.Variables.Add(new DynamicMemoryVariable("ActivePanel", 0x82B76C, offsets: 0x4FA8));
            version.Variables.Add(new DynamicMemoryVariable("InventoryExpanded", 0x82B76C, offsets: 0x4FB0));
            version.Variables.Add(new DynamicMemoryVariable("MinimizedMode", 0x82B76C, offsets: 0x4DF0));
            version.Variables.Add(new DynamicMemoryVariable("DialogOpen", 0x85118C, offsets: new long[] { 0x59C, 0x594, 0x18, 0x24, 0xA20 }));
            version.Variables.Add(new DynamicMemoryVariable("SenseOpen", 0x6FB194, offsets: new long[] { 0x4C, 0x44, 0x18, 0xE }));
            version.Variables.Add(new DynamicMemoryVariable("UserChatting", 0x6EB118, offsets: 0x438));

            AddVersion(version);
        }

        public string DetectVersion(string hash)
        {
            foreach (var version in clientVersions.Values)
                if (string.Equals(version.Hash, hash, StringComparison.OrdinalIgnoreCase))
                    return version.Key;

            return null;
        }

        private void OnVersionAdded(ClientVersion version)
        {
            if (version == null)
                throw new ArgumentNullException(nameof(version));

            VersionAdded?.Invoke(this, new ClientVersionEventArgs(version));
        }

        private void OnVersionChanged(ClientVersion version)
        {
            if (version == null)
                throw new ArgumentNullException(nameof(version));

            VersionChanged?.Invoke(this, new ClientVersionEventArgs(version));
        }

        private void OnVersionRemoved(ClientVersion version)
        {
            if (version == null)
                throw new ArgumentNullException(nameof(version));

            VersionRemoved?.Invoke(this, new ClientVersionEventArgs(version));
        }
    }
}
