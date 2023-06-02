
namespace SleepHunter.IO
{
    public sealed class FileArchiveEntry
    {
        private int index;
        private string name;
        private long offset;
        private long size;

        public int Index
        {
            get => index;
            set => index = value;
        }

        public string Name
        {
            get => name;
            set => name = value;
        }

        public long Offset
        {
            get => offset;
            set => offset = value;
        }

        public long Size
        {
            get => size;
            set => size = value;
        }

        public FileArchiveEntry() { }

        public override string ToString() => Name;
    }
}
