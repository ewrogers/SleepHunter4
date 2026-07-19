using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace SleepHunter.Media
{
    public sealed class EpfImage
    {
        private const int HeaderSize = 12;

        private readonly List<EpfFrame> frames = new();

        public string Name { get; set; }

        public int Width { get; set; }

        public int Height { get; set; }

        public int FrameCount => frames.Count;
        public IEnumerable<EpfFrame> Frames => from f in frames select f;

        public EpfImage(string name, int width, int height, IEnumerable<EpfFrame> frameCollection)
        {
            Name = name;
            Width = width;
            Height = height;

            if (frameCollection != null)
                frames.AddRange(frameCollection);
        }

        public EpfImage(string filename)
           : this(File.Open(filename, FileMode.Open, FileAccess.Read, FileShare.Read), leaveOpen: false)
        {
            Name = filename;
        }

        public EpfImage(Stream stream, bool leaveOpen = true)
        {
            frames = new List<EpfFrame>();

            using var reader = new BinaryReader(stream, System.Text.Encoding.ASCII, leaveOpen: true);
            var streamStart = stream.Position;

            var frameCount = reader.ReadUInt16();

            Width = reader.ReadInt16();
            Height = reader.ReadInt16();

            reader.ReadUInt16();
            var tableOffset = reader.ReadUInt32();

            var dataStart = checked(streamStart + HeaderSize);
            reader.BaseStream.Position = checked(dataStart + tableOffset);

            for (int i = 0; i < frameCount; i++)
            {
                var top = reader.ReadInt16();
                var left = reader.ReadInt16();
                var bottom = reader.ReadInt16();
                var right = reader.ReadInt16();

                long startAddress = reader.ReadUInt32();
                reader.ReadUInt32();

                var frameWidth = right - left;
                var frameHeight = bottom - top;

                if (left < 0 || top < 0 || frameWidth < 0 || frameHeight < 0)
                    throw new InvalidDataException($"EPF frame {i} has invalid dimensions.");

                var size = checked(frameWidth * frameHeight);
                var bits = new byte[size];

                if (size > 0)
                {
                    long previousPosition = reader.BaseStream.Position;

                    reader.BaseStream.Position = checked(dataStart + startAddress);
                    reader.BaseStream.ReadExactly(bits);

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

        public override string ToString() => Name ?? "<Unknown EPF>";
    }
}
