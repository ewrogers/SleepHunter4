using System;
using System.Collections.Generic;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Linq;
using System.Text;

namespace SleepHunter.IO
{
   public sealed class FileArchive : IDisposable
   {
      static readonly int NameLength = 13;

      bool isDisposed;
      string name;
      Dictionary<string, FileArchiveEntry> entries = new Dictionary<string, FileArchiveEntry>(StringComparer.OrdinalIgnoreCase);
      MemoryMappedFile mappedFile;
      
      public string Name
      {
         get { return name; }
         set { name = value; }
      }

      public int Count { get { return entries.Count; } }

      public IEnumerable<FileArchiveEntry> Entries { get { return from e in entries.Values select e; } }

      public FileArchive(string filename)
      {
         using (var inputStream = File.Open(filename, FileMode.Open, FileAccess.Read, FileShare.Read))
         {
            mappedFile = MemoryMappedFile.CreateFromFile(inputStream, null, 0, MemoryMappedFileAccess.Read, null, HandleInheritability.None, true);
            ReadTableOfContents();
         }

         name = filename;
      }

      void ReadTableOfContents()
      {
         using (var stream = mappedFile.CreateViewStream(0, 0, MemoryMappedFileAccess.Read))
         using (var reader = new BinaryReader(stream, Encoding.ASCII))
         {
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

            stream.Close();
         }
      }

      public bool ContainsFile(string filename)
      {
         return entries.ContainsKey(filename);
      }

      public FileArchiveEntry GetEntry(string filename)
      {
         return entries[filename];
      }

      public Stream GetStream(string filename)
      {
         var entry = entries[filename];
         var stream = mappedFile.CreateViewStream(entry.Offset, entry.Size, MemoryMappedFileAccess.Read);

         return stream;
      }

      #region IDisposable Methods
      public void Dispose()
      {
         Dispose(true);
         GC.SuppressFinalize(this);
      }

      void Dispose(bool isDisposing)
      {
         if (isDisposed) return;

         if (mappedFile != null) 
            mappedFile.Dispose();

         mappedFile = null;

         isDisposed = true;
      }
      #endregion
   }
}
