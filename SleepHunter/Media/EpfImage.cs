using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace SleepHunter.Media
{
    internal sealed class EpfImage
    {
        private string name;
        private int width;
        private int height;
        private readonly List<EpfFrame> frames;

        public string Name
        {
            get { return name; }
            set { name = value; }
        }

        public int Width
        {
            get { return width; }
            set { width = value; }
        }

        public int Height
        {
            get { return height; }
            set { height = value; }
        }

        public int FrameCount { get { return frames.Count; } }
        public IEnumerable<EpfFrame> Frames
        {
            get { return from f in frames select f; }
        }

        public EpfImage(string name, int width, int height, IEnumerable<EpfFrame> frames)
        {
            this.name = name;
            this.width = width;
            this.height = height;

            if (frames != null)
                this.frames = new List<EpfFrame>(frames);
            else
                this.frames = new List<EpfFrame>();
        }

        public EpfImage(string filename)
           : this(File.Open(filename, FileMode.Open, FileAccess.Read, FileShare.Read), leaveOpen: false)
        {
            name = filename;
        }

        public EpfImage(Stream stream, bool leaveOpen = true)
        {
            frames = new List<EpfFrame>();

            var reader = new BinaryReader(stream);

            var frameCount = reader.ReadUInt16();

            width = reader.ReadInt16();
            height = reader.ReadInt16();

            var unknown = reader.ReadUInt16();
            var tableOffset = reader.ReadUInt32();

            reader.BaseStream.Position += tableOffset;

            for (int i = 0; i < frameCount; i++)
            {
                var left = reader.ReadInt16();
                var top = reader.ReadInt16();
                var right = reader.ReadInt16();
                var bottom = reader.ReadInt16();

                long startAddress = reader.ReadUInt32();
                long endAddress = reader.ReadUInt32();

                var frameWidth = Math.Abs(right - left);
                var frameHeight = Math.Abs(bottom - top);

                var size = frameWidth * frameHeight;
                var bits = new byte[size];

                if (size > 0)
                {
                    long previousPosition = reader.BaseStream.Position;

                    reader.BaseStream.Position = startAddress;
                    reader.Read(bits, 0, size);

                    reader.BaseStream.Position = previousPosition;
                }

                var frame = new EpfFrame(i, left, top, frameWidth, frameHeight, bits);
                frames.Add(frame);
            }

            if (!leaveOpen)
                stream.Dispose();
        }

        public bool HasFrame(int index)
        {
            return frames.Count > index;
        }

        public EpfFrame GetFrameAt(int index)
        {
            return frames[index];
        }

        public override string ToString()
        {
            return name ?? "<Unknown EPF>";
        }
    }
}
