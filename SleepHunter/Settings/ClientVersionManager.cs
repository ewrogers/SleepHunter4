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
