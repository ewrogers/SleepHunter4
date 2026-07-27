using System;
using System.Collections.Concurrent;
using System.IO;

namespace SleepHunter.IO
{
    public sealed class FileArchiveManager : IDisposable
    {
        private readonly ConcurrentDictionary<string, FileArchive> archives = new(StringComparer.OrdinalIgnoreCase);

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

        public void ClearArchives()
        {
            foreach (var archive in archives.Values)
                archive.Dispose();

            archives.Clear();
        }

        public void Dispose() => ClearArchives();
    }
}
