using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using SleepHunter.Extensions;
using SleepHunter.IO.Process;

namespace SleepHunter.Settings
{
    public sealed class ClientVersionManager
    {
        public const string VersionsFile = @"Versions.xml";
        
        private static readonly ClientVersionManager instance = new();

        public static ClientVersionManager Instance => instance;

        private ClientVersionManager()
        {
            if (!clientVersions.ContainsKey("Auto-Detect"))
                AddVersion(ClientVersion.AutoDetect);
        }

        private readonly ConcurrentDictionary<string, ClientVersion> clientVersions = new(StringComparer.OrdinalIgnoreCase);

        public event ClientVersionEventHandler VersionAdded;
        public event ClientVersionEventHandler VersionChanged;
        public event ClientVersionEventHandler VersionRemoved;

        public ClientVersion this[string key]
        {
            get => GetVersion(key);
            set => AddVersion(value);
        }

        public int Count => clientVersions.Count;

        public IEnumerable<ClientVersion> Versions => 
            from v in clientVersions.Values orderby v.Key select v;

        public ClientVersion DefaultVersion => clientVersions.Values.FirstOrDefault(version => version.IsDefault);

        public void AddVersion(ClientVersion version)
        {
            if (version == null)
                throw new ArgumentNullException(nameof(version));

            if (string.IsNullOrWhiteSpace(version.Key))
                throw new ArgumentException("Key cannot be null or whitespace.", nameof(version));

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

            bool wasRemoved = clientVersions.TryRemove(key, out var removedVersion);

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
            using var inputStream = File.Open(filename, FileMode.Open, FileAccess.Read, FileShare.Read);
            LoadFromStream(inputStream);
        }

        public void LoadFromStream(Stream stream)
        {
            var serializer = new XmlSerializer(typeof(ClientVersionCollection));

            if (serializer.Deserialize(stream) is not ClientVersionCollection collection)
                return;

            foreach (var version in collection.Versions)
                AddVersion(version);
        }

        public void SaveToFile(string filename)
        {
            using var outputStream = File.Create(filename);
            SaveToStream(outputStream);
            outputStream.Flush();
        }

        public void SaveToStream(Stream stream)
        {
            var collection = new ClientVersionCollection(Versions);
            var serializer = new XmlSerializer(typeof(ClientVersionCollection));
            var namespaces = new XmlSerializerNamespaces();
            namespaces.Add("", "");

            serializer.Serialize(stream, collection, namespaces);
        }

        public static bool TryDetectClientVersion(int processId, out ClientVersion detectedVersion)
        {
            detectedVersion = null;

            using var accessor = new ProcessMemoryAccessor(processId, ProcessAccess.Read);
            using var stream = accessor.GetStream();
            using var reader = new BinaryReader(stream, Encoding.ASCII, leaveOpen: true);

            foreach (var version in Instance.Versions)
            {
                // Skip with invalid or missing signatures
                if (version.Signature == null || string.IsNullOrWhiteSpace(version.Signature.Value))
                    continue;

                var signatureLength = version.Signature.Value.Length;

                // Read the signature from the process
                stream.Position = version.Signature.Address;
                var readValue = reader.ReadFixedString(signatureLength);

                // If signature matches the expected value, assume this client version
                if (string.Equals(readValue, version.Signature.Value))
                {
                    detectedVersion = version;
                    return true;
                }
            }

            return false;
        }

        void OnVersionAdded(ClientVersion version)
        {
            if (version == null)
                throw new ArgumentNullException(nameof(version));

            VersionAdded?.Invoke(this, new ClientVersionEventArgs(version));
        }

        void OnVersionChanged(ClientVersion version)
        {
            if (version == null)
                throw new ArgumentNullException(nameof(version));

            VersionChanged?.Invoke(this, new ClientVersionEventArgs(version));
        }

        void OnVersionRemoved(ClientVersion version)
        {
            if (version == null)
                throw new ArgumentNullException(nameof(version));

            VersionRemoved?.Invoke(this, new ClientVersionEventArgs(version));
        }
    }
}
