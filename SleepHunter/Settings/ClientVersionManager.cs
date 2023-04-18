using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Serialization;

using SleepHunter.IO.Process;

namespace SleepHunter.Settings
{
    public sealed class ClientVersionManager
    {
        public static readonly string VersionsFile = @"Versions.xml";

        private static readonly ClientVersionManager instance = new ClientVersionManager();

        public static ClientVersionManager Instance => instance;

        private ClientVersionManager() { }

        private readonly ConcurrentDictionary<string, ClientVersion> clientVersions = new ConcurrentDictionary<string, ClientVersion>(StringComparer.OrdinalIgnoreCase);

        public event ClientVersionEventHandler VersionAdded;
        public event ClientVersionEventHandler VersionChanged;
        public event ClientVersionEventHandler VersionRemoved;

        public ClientVersion this[string key]
        {
            get => GetVersion(key);
            set => AddVersion(value);
        }

        public int Count => clientVersions.Count;

        public IEnumerable<ClientVersion> Versions => from version in clientVersions.Values
                                                      orderby version.Key
                                                      select version;
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
                LoadFromStream(inputStream);
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
