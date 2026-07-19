using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Windows.Media;

namespace SleepHunter.Media
{
    public sealed class ColorPalette : IEnumerable<Color>
    {
        public const int ColorCount = 256;

        private readonly List<Color> colors;

        public string Name { get; set; }

        public int Count => colors.Count;

        public ColorPalette(string filename)
           : this(File.Open(filename, FileMode.Open, FileAccess.Read, FileShare.Read), leaveOpen: false)
        {
            Name = filename;
        }

        public Color this[int index]
        {
            get => colors[index];
            set => colors[index] = value;
        }

        public ColorPalette(Stream stream, bool leaveOpen = true)
        {
            using var buffer = new MemoryStream();
            stream.CopyTo(buffer);
            var bytes = buffer.ToArray();

            if (bytes.Length != ColorCount * 3)
                bytes = ParseRiffPalette(bytes);

            colors = new List<Color>(ColorCount);
            for (int i = 0; i < ColorCount; i++)
            {
                var offset = i * 3;
                var red = bytes[offset];
                var green = bytes[offset + 1];
                var blue = bytes[offset + 2];

                var color = Color.FromRgb(red, green, blue);
                colors.Add(color);
            }

            if (!leaveOpen)
                stream.Dispose();
        }

        public ColorPalette(IEnumerable<Color> collection)
        {
            if (collection != null)
                colors = new List<Color>(collection);
            else
                colors = new List<Color>();
        }

        public ColorPalette MakeGrayscale(double redWeight = 0.30, double greenWeight = 0.59, double blueWeight = 0.11)
        {
            var grayColors = new List<Color>(Count);

            foreach (var color in this)
            {
                var luma = color.R * redWeight + color.G * greenWeight + color.B * blueWeight;
                luma = Math.Min(luma, 255);

                var gray = Color.FromRgb((byte)luma, (byte)luma, (byte)luma);
                grayColors.Add(gray);
            }

            return new ColorPalette(grayColors);
        }

        internal bool DyeRangeMatches(IReadOnlyList<Color> dyeColors)
        {
            if (dyeColors == null || dyeColors.Count > ItemDyeTable.ColorCount)
                return false;

            for (var index = 0; index < dyeColors.Count; index++)
            {
                var color = colors[ItemDyeTable.StartIndex + index];
                var dyeColor = dyeColors[index];
                if (color.R != dyeColor.R || color.G != dyeColor.G || color.B != dyeColor.B)
                    return false;
            }

            return true;
        }

        internal ColorPalette WithDye(IReadOnlyList<Color> dyeColors)
        {
            var dyedColors = new List<Color>(colors);
            for (var index = 0; index < Math.Min(dyeColors.Count, ItemDyeTable.ColorCount); index++)
                dyedColors[ItemDyeTable.StartIndex + index] = dyeColors[index];

            return new ColorPalette(dyedColors);
        }

        private static byte[] ParseRiffPalette(ReadOnlySpan<byte> bytes)
        {
            if (bytes.Length < 12 ||
                !bytes.Slice(0, 4).SequenceEqual("RIFF"u8) ||
                !bytes.Slice(8, 4).SequenceEqual("PAL "u8))
            {
                throw new InvalidDataException(
                    $"A palette must contain {ColorCount * 3} RGB bytes or use the RIFF PAL format.");
            }

            var riffLength = checked((int)System.Buffers.Binary.BinaryPrimitives.ReadUInt32LittleEndian(bytes.Slice(4)) + 8);
            if (riffLength > bytes.Length)
                throw new InvalidDataException("The RIFF palette exceeds the file bounds.");

            var offset = 12;
            while (offset <= riffLength - 8)
            {
                var chunkName = bytes.Slice(offset, 4);
                var chunkLength = checked((int)System.Buffers.Binary.BinaryPrimitives.ReadUInt32LittleEndian(
                    bytes.Slice(offset + 4)));
                var chunkOffset = offset + 8;

                if (chunkOffset > riffLength - chunkLength)
                    throw new InvalidDataException("A RIFF palette chunk exceeds the file bounds.");

                if (chunkName.SequenceEqual("data"u8))
                {
                    if (chunkLength < 4)
                        throw new InvalidDataException("The RIFF palette data chunk is incomplete.");

                    var colorCount = System.Buffers.Binary.BinaryPrimitives.ReadUInt16LittleEndian(
                        bytes.Slice(chunkOffset + 2));
                    var requiredLength = checked(4 + colorCount * 4);
                    if (colorCount > ColorCount || chunkLength < requiredLength)
                        throw new InvalidDataException("The RIFF palette contains invalid color data.");

                    var colors = new byte[ColorCount * 3];
                    for (var index = 0; index < colorCount; index++)
                    {
                        var sourceOffset = chunkOffset + 4 + index * 4;
                        bytes.Slice(sourceOffset, 3).CopyTo(colors.AsSpan(index * 3, 3));
                    }

                    return colors;
                }

                offset = checked(chunkOffset + chunkLength + (chunkLength & 1));
            }

            throw new InvalidDataException("The RIFF palette does not contain a data chunk.");
        }

        public IEnumerator<Color> GetEnumerator()
        {
            foreach (var color in colors)
                yield return color;
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
