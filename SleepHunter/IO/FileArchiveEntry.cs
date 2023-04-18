
namespace SleepHunter.IO
{
    public sealed class FileArchiveEntry
    {
        public int Index { get; set; }
        public string Name { get; set; }
        public long Offset { get; set; }
        public long Size { get; set; }

        public override string ToString() => Name;
    }
}
