using System.Buffers.Binary;
using System.Text;

using SleepHunter.IO;
using SleepHunter.Media;

namespace SleepHunter.Tests.Media
{
    [TestFixture]
    public sealed class ItemIconRendererTests
    {
        private string directory;

        [SetUp]
        public void SetUp()
        {
            directory = Path.Combine(Path.GetTempPath(), "SleepHunter.Tests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
        }

        [TearDown]
        public void TearDown()
        {
            FileArchiveManager.Instance.ClearArchives();

            if (Directory.Exists(directory))
                Directory.Delete(directory, recursive: true);
        }

        [Test]
        public void ShouldRenderItemIconsWithPaletteTableAndPreferLegendArchive()
        {
            var palette = WindowsPalette(
                (1, 200, 10, 20),
                (2, 10, 200, 20));

            WriteDat(
                "Legend.dat",
                ("item001.epf", Epf(2, 1, new byte[] { 1, 2 })),
                ("item002.epf", Epf(1, 1, new byte[] { 2 })),
                ("item000.pal", palette),
                ("itempal.tbl", Text("1 1 0\n267 267 0\n")));

            WriteDat(
                "setoa.dat",
                ("item001.epf", Epf(2, 1, new byte[] { 2, 1 })));

            var renderer = ItemIconRenderer.Load(Path.Combine(directory, "Darkages.exe"));
            var first = renderer.Render(1);
            var secondGroup = renderer.Render(267);

            Assert.Multiple(() =>
            {
                Assert.That(first, Is.Not.Null);
                Assert.That(first.Width, Is.EqualTo(2));
                Assert.That(first.Height, Is.EqualTo(1));
                Assert.That(first.Bits, Is.EqualTo(new byte[]
                {
                    20, 10, 200, 255,
                    20, 200, 10, 255
                }));
                Assert.That(secondGroup, Is.Not.Null);
                Assert.That(secondGroup.Bits, Is.EqualTo(new byte[] { 20, 200, 10, 255 }));
            });
        }

        [Test]
        public void ShouldRenderDyedItemIconVariants()
        {
            var palette = Palette(
                (98, 10, 10, 10),
                (99, 20, 20, 20),
                (100, 30, 30, 30),
                (101, 40, 40, 40),
                (102, 50, 50, 50),
                (103, 60, 60, 60));

            const string dyeTable = """
                                    6
                                    0
                                    10,10,10
                                    20,20,20
                                    30,30,30
                                    40,40,40
                                    50,50,50
                                    60,60,60
                                    1
                                    200,100,50
                                    201,101,51
                                    202,102,52
                                    203,103,53
                                    204,104,54
                                    205,105,55
                                    """;

            WriteDat(
                "setoa.dat",
                ("item001.epf", Epf(1, 1, new byte[] { 98 })),
                ("item000.pal", palette),
                ("itempal.tbl", Text("1 1 0\n")),
                ("color0.tbl", Text(dyeTable)));

            var renderer = ItemIconRenderer.Load(Path.Combine(directory, "Darkages.exe"));

            Assert.Multiple(() =>
            {
                Assert.That(renderer.Render(1, 0).Bits, Is.EqualTo(new byte[] { 10, 10, 10, 255 }));
                Assert.That(renderer.Render(1, 1).Bits, Is.EqualTo(new byte[] { 50, 100, 200, 255 }));
            });
        }

        private string WriteDat(string name, params (string Name, byte[] Data)[] assets)
        {
            const int headerSize = sizeof(uint);
            const int tableEntrySize = sizeof(uint) + 13;

            var tableEntryCount = assets.Length + 1;
            var dataOffset = headerSize + tableEntryCount * tableEntrySize;
            var bytes = new byte[dataOffset + assets.Sum(asset => asset.Data.Length)];
            BinaryPrimitives.WriteUInt32LittleEndian(bytes, (uint)tableEntryCount);

            var currentOffset = dataOffset;
            for (var index = 0; index < assets.Length; index++)
            {
                WriteDatTableEntry(bytes, index, currentOffset, assets[index].Name);
                assets[index].Data.CopyTo(bytes, currentOffset);
                currentOffset += assets[index].Data.Length;
            }

            WriteDatTableEntry(bytes, assets.Length, currentOffset, string.Empty);
            var path = Path.Combine(directory, name);
            File.WriteAllBytes(path, bytes);
            return path;
        }

        private static byte[] Epf(int width, int height, params byte[][] frames)
        {
            var pixelLength = frames.Sum(frame => frame.Length);
            var bytes = new byte[12 + pixelLength + frames.Length * 16];
            BinaryPrimitives.WriteUInt16LittleEndian(bytes, checked((ushort)frames.Length));
            BinaryPrimitives.WriteInt16LittleEndian(bytes.AsSpan(2), checked((short)width));
            BinaryPrimitives.WriteInt16LittleEndian(bytes.AsSpan(4), checked((short)height));
            BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(8), checked((uint)pixelLength));

            var pixelOffset = 0;
            for (var index = 0; index < frames.Length; index++)
            {
                var frame = frames[index];
                if (frame.Length != width * height)
                    throw new ArgumentException("Test EPF frames must fill the declared canvas.");

                var entryOffset = 12 + pixelLength + index * 16;
                BinaryPrimitives.WriteInt16LittleEndian(bytes.AsSpan(entryOffset + 4), checked((short)height));
                BinaryPrimitives.WriteInt16LittleEndian(bytes.AsSpan(entryOffset + 6), checked((short)width));
                BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(entryOffset + 8), checked((uint)pixelOffset));
                BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(entryOffset + 12),
                    checked((uint)(pixelOffset + frame.Length)));

                frame.CopyTo(bytes, 12 + pixelOffset);
                pixelOffset += frame.Length;
            }

            return bytes;
        }

        private static byte[] Palette(params (byte Index, byte Red, byte Green, byte Blue)[] colors)
        {
            var bytes = new byte[ColorPalette.ColorCount * 3];
            foreach (var color in colors)
            {
                var offset = color.Index * 3;
                bytes[offset] = color.Red;
                bytes[offset + 1] = color.Green;
                bytes[offset + 2] = color.Blue;
            }

            return bytes;
        }

        private static byte[] WindowsPalette(params (byte Index, byte Red, byte Green, byte Blue)[] colors)
        {
            var colorsByIndex = colors.ToDictionary(color => color.Index);
            using var stream = new MemoryStream();
            using (var writer = new BinaryWriter(stream, Encoding.ASCII, leaveOpen: true))
            {
                writer.Write(Encoding.ASCII.GetBytes("RIFF"));
                writer.Write(16 + ColorPalette.ColorCount * 4);
                writer.Write(Encoding.ASCII.GetBytes("PAL "));
                writer.Write(Encoding.ASCII.GetBytes("data"));
                writer.Write(4 + ColorPalette.ColorCount * 4);
                writer.Write((ushort)0x0300);
                writer.Write((ushort)ColorPalette.ColorCount);

                for (var index = 0; index < ColorPalette.ColorCount; index++)
                {
                    var color = colorsByIndex.GetValueOrDefault((byte)index);
                    writer.Write(color.Red);
                    writer.Write(color.Green);
                    writer.Write(color.Blue);
                    writer.Write((byte)0);
                }
            }

            return stream.ToArray();
        }

        private static byte[] Text(string value) => Encoding.ASCII.GetBytes(value);

        private static void WriteDatTableEntry(byte[] bytes, int index, int offset, string name)
        {
            var entryOffset = sizeof(uint) + index * (sizeof(uint) + 13);
            BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(entryOffset), checked((uint)offset));
            var nameBytes = Encoding.ASCII.GetBytes(name);
            nameBytes.AsSpan(0, Math.Min(nameBytes.Length, 13)).CopyTo(bytes.AsSpan(entryOffset + 4, 13));
        }
    }
}
