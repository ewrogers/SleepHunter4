using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace SleepHunter.Macro
{
    public sealed class MacroLocalStorage : IDictionary<string, string>
    {
        private readonly ConcurrentDictionary<string, string> keyValueStorage;

        public int Count => keyValueStorage.Count;
        public ICollection<string> Keys => keyValueStorage.Keys.ToList();
        public ICollection<string> Values => keyValueStorage.Values.ToList();
        public bool IsReadOnly => false;

        public string this[string key]
        {
            get => keyValueStorage[key];
            set => keyValueStorage[key] = value;
        }

        public MacroLocalStorage()
        {
            keyValueStorage = new ConcurrentDictionary<string, string>();
        }

        public MacroLocalStorage(IDictionary<string, string> collection)
        {
            if (collection == null)
                throw new ArgumentNullException(nameof(collection));

            keyValueStorage = new ConcurrentDictionary<string, string>(collection);
        }

        public bool ContainsKey(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
                return false;

            return keyValueStorage.ContainsKey(key);
        }

        public bool Contains(KeyValuePair<string, string> item)
        {
            if (!keyValueStorage.TryGetValue(item.Key, out var value))
                return false;

            return string.Equals(item.Value, value);
        }

        public bool TryGetValue(string key, out string value) => keyValueStorage.TryGetValue(key, out value);

        public string GetStringOrDefault(string key, string defaultValue = null)
        {
            if (!TryGetValue(key, out var value))
                return defaultValue;

            return value;
        }

        public bool GetBoolOrDefault(string key, bool defaultValue = false)
        {
            if (!TryGetValue(key, out var stringValue))
                return defaultValue;

            if (!bool.TryParse(stringValue, out var boolValue))
                return defaultValue;

            return boolValue;
        }

        public int GetIntegerOrDefault(string key, int defaultValue = 0)
        {
            if (!TryGetValue(key, out var stringValue))
                return defaultValue;

            if (!int.TryParse(stringValue, out var integerValue))
                return defaultValue;

            return integerValue;
        }

        public double GetDoubleOrDefault(string key, double defaultValue = 0.0)
        {
            if (!TryGetValue(key, out var stringValue))
                return defaultValue;

            if (!double.TryParse(stringValue, out var doubleValue))
                return defaultValue;

            return doubleValue;
        }

        public void Add(string key, string value) => keyValueStorage[key] = value;

        public void Add(string key, bool value) => Add(key, value.ToString());

        public void Add(string key, int value) => Add(key, value.ToString());

        public void Add(string key, double value) => Add(key, value.ToString());

        public bool Remove(string key) => keyValueStorage.TryRemove(key, out _);

        public void Clear() => keyValueStorage.Clear();

        void ICollection<KeyValuePair<string, string>>.Add(KeyValuePair<string, string> item) => Add(item.Key, item.Value);

        void ICollection<KeyValuePair<string, string>>.CopyTo(KeyValuePair<string, string>[] array, int arrayIndex)
        {
            foreach(var pair in keyValueStorage)
                array[arrayIndex++] = pair;
        }

        bool ICollection<KeyValuePair<string, string>>.Remove(KeyValuePair<string, string> item)
        {
            if (!TryGetValue(item.Key, out var value))
                return false;

            if (!string.Equals(item.Value, value))
                return false;

            return Remove(item.Key);
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public IEnumerator<KeyValuePair<string, string>> GetEnumerator() => keyValueStorage.GetEnumerator();
    }
}
