
namespace SleepHunter.IO
{
    internal sealed class FileArchiveEntry
    {
        private int index;
        private string name;
        private long offset;
        private long size;

        public int Index
        {
            get { return index; }
            set { index = value; }
        }

        public string Name
        {
            get { return name; }
            set { name = value; }
        }

        public long Offset
        {
            get { return offset; }
            set { offset = value; }
        }

        public long Size
        {
            get { return size; }
            set { size = value; }
        }

        public FileArchiveEntry() { }

        public override string ToString()
        {
            return base.ToString();
        }
    }
}
