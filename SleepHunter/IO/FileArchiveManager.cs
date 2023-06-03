using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;

namespace SleepHunter.IO
{
    public sealed class FileArchiveManager
    {
        private static readonly FileArchiveManager instance = new();

        public static FileArchiveManager Instance => instance;

        private FileArchiveManager() { }

        private readonly ConcurrentDictionary<string, FileArchive> archives = new(StringComparer.OrdinalIgnoreCase);

        public int Count => archives.Count;

        public IEnumerable<FileArchive> Archives => from a in archives.Values select a;

        public bool ContainsArchive(string filename) => archives.ContainsKey(filename);

        public FileArchive GetArchive(string filename)
        {
            if (archives.ContainsKey(filename))
                return archives[filename];

            if (!File.Exists(filename))
                return null;

            try
            {
                var archive = new FileArchive(filename);
                archives[filename] = archive;

                return archive;
            }
            catch { return null; }
        }

        public bool RemoveArchive(string filename)
        {
            if (!archives.ContainsKey(filename))
                return false;

            var archive = archives[filename];
            archive.Dispose();

            return archives.TryRemove(filename, out _);
        }

        public void ClearArchives()
        {
            foreach (var archive in archives.Values)
                archive.Dispose();

            archives.Clear();
        }
    }
}
