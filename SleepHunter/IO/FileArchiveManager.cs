using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;

namespace SleepHunter.IO
{
    public sealed class FileArchiveManager
    {
        #region Singleton
        static readonly FileArchiveManager instance = new FileArchiveManager();

        public static FileArchiveManager Instance { get { return instance; } }

        private FileArchiveManager()
        {

        }
        #endregion

        ConcurrentDictionary<string, FileArchive> archives = new ConcurrentDictionary<string, FileArchive>(StringComparer.OrdinalIgnoreCase);

        public int Count { get { return archives.Count; } }

        public IEnumerable<FileArchive> Archives { get { return from a in archives.Values select a; } }

        public bool ContainsArchive(string filename)
        {
            return archives.ContainsKey(filename);
        }

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

            FileArchive removedArchive;
            return archives.TryRemove(filename, out removedArchive);
        }

        public void ClearArchives()
        {
            foreach (var archive in archives.Values)
                archive.Dispose();

            archives.Clear();
        }
    }
}
