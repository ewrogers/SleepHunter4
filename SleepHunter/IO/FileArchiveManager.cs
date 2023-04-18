using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.IO;

namespace SleepHunter.IO
{
    internal sealed class FileArchiveManager
    {
        private static readonly FileArchiveManager instance = new FileArchiveManager();

        public static FileArchiveManager Instance => instance;

        private readonly ConcurrentDictionary<string, FileArchive> archives = new ConcurrentDictionary<string, FileArchive>(StringComparer.OrdinalIgnoreCase);

        public int Count => archives.Count;

        public IEnumerable<FileArchive> Archives => archives.Values;

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

            return archives.TryRemove(filename, out var _);
        }

        public void ClearArchives()
        {
            foreach (var archive in archives.Values)
                archive.Dispose();

            archives.Clear();
        }
    }
}
