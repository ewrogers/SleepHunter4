
namespace SleepHunter.IO
{
    internal sealed class FileArchiveEntry
    {
        public int Index { get; set; }
        public string Name { get; set; }
        public long Offset { get; set; }
        public long Size { get; set; }

        public FileArchiveEntry() { }

        public override string ToString() => Name;
    }
}
