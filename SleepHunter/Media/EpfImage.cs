using System;
using System.Collections.Generic;
using System.IO;

namespace SleepHunter.Media
{
    internal sealed class EpfImage
    {
        private readonly List<EpfFrame> frames;

        public string Name { get; }

        public int Width { get; }

        public int Height { get; }

        public int FrameCount => frames.Count;

        public IReadOnlyCollection<EpfFrame> Frames => frames;

        public EpfImage(string name, int width, int height, IEnumerable<EpfFrame> frames)
        {
            Name = name;
            Width = width;
            Height = height;

            if (frames != null)
                this.frames = new List<EpfFrame>(frames);
            else
                this.frames = new List<EpfFrame>();
        }

        public EpfImage(string filename)
           : this(File.Open(filename, FileMode.Open, FileAccess.Read, FileShare.Read), leaveOpen: false)
        {
            Name = filename;
        }

        public EpfImage(Stream stream, bool leaveOpen = true)
        {
            frames = new List<EpfFrame>();

            var reader = new BinaryReader(stream);

            var frameCount = reader.ReadUInt16();

            Width = reader.ReadInt16();
            Height = reader.ReadInt16();

            var headerBytes = reader.ReadUInt16();
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
                    var previousPosition = reader.BaseStream.Position;

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

        public bool HasFrame(int index) => frames.Count > index;

        public EpfFrame GetFrameAt(int index) => frames[index];

        public override string ToString() => $"{Name}, Frames={FrameCount}";
    }
}
