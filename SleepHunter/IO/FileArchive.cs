using System;
using System.Collections.Generic;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Text;

using SleepHunter.Extensions;

namespace SleepHunter.IO
{
    public sealed class FileArchive : IDisposable
    {
        private static readonly int NameLength = 13;

        private bool isDisposed;
        private readonly Dictionary<string, FileArchiveEntry> entries = new Dictionary<string, FileArchiveEntry>(StringComparer.OrdinalIgnoreCase);
        private readonly MemoryMappedFile mappedFile;

        public string Name { get; set; }

        public int Count => entries.Count;

        public IEnumerable<FileArchiveEntry> Entries
        {
            get
            {
                CheckIfDisposed();
                return entries.Values;
            }
        }

        public FileArchive(string filename)
        {
            if (!File.Exists(filename))
                throw new FileNotFoundException("File archive was not found", filename);

            using (var inputStream = File.Open(filename, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                mappedFile = MemoryMappedFile.CreateFromFile(inputStream, null, 0, MemoryMappedFileAccess.Read, null, HandleInheritability.None, true);
                ReadTableOfContents();
            }

            Name = filename;
        }

        private void ReadTableOfContents()
        {
            Stream stream = null;
            try
            {
                stream = mappedFile.CreateViewStream(0, 0, MemoryMappedFileAccess.Read);
                using (var reader = new BinaryReader(stream, Encoding.ASCII))
                {
                    stream = null;
                    var entryCount = reader.ReadUInt32() - 1;

                    for (int i = 0; i < entryCount; i++)
                    {
                        var index = i;
                        var startAddress = reader.ReadUInt32();
                        var name = reader.ReadFixedString(NameLength).Trim();
                        var size = reader.ReadInt32() - startAddress;

                        reader.BaseStream.Position -= sizeof(uint);

                        var entry = new FileArchiveEntry
                        {
                            Index = index,
                            Name = name,
                            Offset = startAddress,
                            Size = size
                        };

                        entries[name] = entry;
                    }
                }
            }
            finally { stream?.Dispose(); }
        }

        public bool ContainsFile(string filename)
        {
            CheckIfDisposed();
            return entries.ContainsKey(filename);
        }

        public FileArchiveEntry GetEntry(string filename)
        {
            CheckIfDisposed();
            return entries[filename];
        }

        public Stream GetStream(string filename)
        {
            CheckIfDisposed();

            var entry = entries[filename];
            var stream = mappedFile.CreateViewStream(entry.Offset, entry.Size, MemoryMappedFileAccess.Read);

            return stream;
        }

        ~FileArchive() => Dispose(false);

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool isDisposing)
        {
            if (isDisposed)
                return;

            if (isDisposing)
            {
                mappedFile?.Dispose();
            }

            isDisposed = true;
        }

        private void CheckIfDisposed()
        {
            if (isDisposed)
                throw new ObjectDisposedException(GetType().Name);
        }
    }
}
