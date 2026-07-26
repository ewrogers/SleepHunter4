using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Media;

using SleepHunter.IO;

namespace SleepHunter.Media
{
    internal sealed class ItemIconRenderer
    {
        private const int FramesPerImage = 266;
        private const string DefaultDyeTableName = "color0.tbl";

        private readonly object imageLock = new();
        private readonly IReadOnlyDictionary<int, IReadOnlyList<ArchivedAsset>> imageAssets;
        private readonly IReadOnlyDictionary<int, ColorPalette> palettes;
        private readonly ItemPaletteTable paletteTable;
        private readonly ItemDyeTable dyeTable;
        private readonly IReadOnlyList<Color> defaultDyeColors;
        private readonly Dictionary<int, EpfImage> images = new();
        private readonly HashSet<int> failedImages = new();

        private ItemIconRenderer(
            IReadOnlyDictionary<int, IReadOnlyList<ArchivedAsset>> imageAssets,
            IReadOnlyDictionary<int, ColorPalette> palettes,
            ItemPaletteTable paletteTable,
            ItemDyeTable dyeTable,
            IReadOnlyList<Color> defaultDyeColors)
        {
            this.imageAssets = imageAssets;
            this.palettes = palettes;
            this.paletteTable = paletteTable;
            this.dyeTable = dyeTable;
            this.defaultDyeColors = defaultDyeColors;
        }

        public static ItemIconRenderer Load(string clientExecutablePath)
        {
            var directory = Path.GetDirectoryName(clientExecutablePath);
            if (string.IsNullOrWhiteSpace(directory) || !Directory.Exists(directory))
                throw new DirectoryNotFoundException("The configured client directory was not found.");

            var selectedAssets = new Dictionary<string, ArchivedAsset>(StringComparer.OrdinalIgnoreCase);
            var assetVariants = new Dictionary<string, List<ArchivedAsset>>(StringComparer.OrdinalIgnoreCase);

            var archivePaths = Directory.EnumerateFiles(directory)
                .Where(path => string.Equals(Path.GetExtension(path), ".dat", StringComparison.OrdinalIgnoreCase))
                .OrderBy(Path.GetFileName, StringComparer.OrdinalIgnoreCase);

            foreach (var archivePath in archivePaths)
            {
                var archive = FileArchiveManager.Instance.GetArchive(archivePath);
                if (archive == null)
                    continue;

                foreach (var entry in archive.Entries)
                {
                    var asset = new ArchivedAsset(archive, entry);
                    selectedAssets[entry.Name] = asset;

                    if (!assetVariants.TryGetValue(entry.Name, out var variants))
                    {
                        variants = new List<ArchivedAsset>();
                        assetVariants[entry.Name] = variants;
                    }

                    variants.Add(asset);
                }
            }

            var imageAssets = assetVariants
                .Select(pair => (Identifier: GetNumericIdentifier(pair.Key, "item", ".epf"), pair.Value))
                .Where(pair => pair.Identifier.HasValue && pair.Identifier.Value > 0)
                .GroupBy(pair => pair.Identifier.Value)
                .ToDictionary(
                    group => group.Key,
                    group => (IReadOnlyList<ArchivedAsset>)group.SelectMany(pair => pair.Value).ToList());

            if (imageAssets.Count == 0)
                throw new FileNotFoundException("No item EPF assets were found.", "item*.epf");

            var tableNames = selectedAssets.Keys
                .Where(name => name.StartsWith("itempal", StringComparison.OrdinalIgnoreCase) &&
                               name.EndsWith(".tbl", StringComparison.OrdinalIgnoreCase))
                .Order(StringComparer.OrdinalIgnoreCase)
                .ToArray();

            if (tableNames.Length == 0)
                throw new FileNotFoundException("No item palette table was found.", "itempal*.tbl");

            var paletteTable = new ItemPaletteTable();
            foreach (var tableName in tableNames)
            {
                using var stream = selectedAssets[tableName].OpenRead();
                paletteTable.Merge(stream);
            }

            var palettes = new Dictionary<int, ColorPalette>();
            foreach (var pair in selectedAssets)
            {
                var identifier = GetNumericIdentifier(pair.Key, "item", ".pal");
                if (!identifier.HasValue)
                    continue;

                using var stream = pair.Value.OpenRead();
                palettes[identifier.Value] = new ColorPalette(stream);
            }

            if (palettes.Count == 0)
                throw new FileNotFoundException("No item palettes were found.", "item*.pal");

            var dyeTable = ItemDyeTable.Empty;
            IReadOnlyList<Color> defaultDyeColors = null;
            if (selectedAssets.TryGetValue(DefaultDyeTableName, out var dyeAsset))
            {
                using var stream = dyeAsset.OpenRead();
                dyeTable = ItemDyeTable.Parse(stream);
                defaultDyeColors = dyeTable.GetColors(0);
            }

            return new ItemIconRenderer(imageAssets, palettes, paletteTable, dyeTable, defaultDyeColors);
        }

        public RenderedBitmap Render(int itemId, byte color = 0)
        {
            if (itemId <= 0 || itemId > ushort.MaxValue)
                return null;

            var imageIdentifier = ((itemId - 1) / FramesPerImage) + 1;
            var frameIndex = (itemId - 1) % FramesPerImage;
            var image = GetImage(imageIdentifier);
            if (image == null || !image.HasFrame(frameIndex))
                return null;

            var frame = image.GetFrameAt(frameIndex);
            if (frame.Width == 0 || frame.Height == 0)
                return null;

            var paletteId = paletteTable.GetPaletteId(itemId);
            var useLuminanceAlpha = paletteId >= 1000;
            if (useLuminanceAlpha)
                paletteId -= 1000;

            if (!palettes.TryGetValue(paletteId, out var palette))
                return null;

            var dyeColors = color == 0 ? null : dyeTable.GetColors(color);
            if (dyeColors != null && defaultDyeColors != null && palette.DyeRangeMatches(defaultDyeColors))
                palette = palette.WithDye(dyeColors);

            return RenderManager.RenderItem(frame, palette, useLuminanceAlpha);
        }

        private EpfImage GetImage(int imageIdentifier)
        {
            lock (imageLock)
            {
                if (images.TryGetValue(imageIdentifier, out var image))
                    return image;

                if (failedImages.Contains(imageIdentifier) ||
                    !imageAssets.TryGetValue(imageIdentifier, out var variants))
                {
                    return null;
                }

                try
                {
                    image = LoadBestImage(variants);
                    images[imageIdentifier] = image;
                    return image;
                }
                catch (Exception exception) when (exception is IOException or InvalidDataException or OverflowException)
                {
                    failedImages.Add(imageIdentifier);
                    return null;
                }
            }
        }

        private static EpfImage LoadBestImage(IReadOnlyList<ArchivedAsset> variants)
        {
            var images = new List<(ArchivedAsset Asset, EpfImage Image)>();
            Exception firstException = null;

            foreach (var asset in variants)
            {
                try
                {
                    using var stream = asset.OpenRead();
                    images.Add((asset, new EpfImage(stream)));
                }
                catch (Exception exception) when (exception is IOException or InvalidDataException or OverflowException)
                {
                    firstException ??= exception;
                }
            }

            var best = images
                .OrderByDescending(candidate =>
                    string.Equals(candidate.Asset.ArchiveName, "Legend.dat", StringComparison.OrdinalIgnoreCase))
                .ThenByDescending(candidate => candidate.Image.FrameCount)
                .ThenByDescending(candidate => candidate.Asset.Size)
                .FirstOrDefault();

            if (best.Asset == null)
                throw firstException ?? new InvalidDataException("No valid item EPF variant was found.");

            return best.Image;
        }

        private static int? GetNumericIdentifier(string name, string prefix, string suffix)
        {
            if (!name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) ||
                !name.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            var identifier = name.Substring(prefix.Length, name.Length - prefix.Length - suffix.Length);
            return int.TryParse(identifier, out var value) ? value : null;
        }

        private sealed class ArchivedAsset
        {
            private readonly FileArchive archive;
            private readonly FileArchiveEntry entry;

            public string ArchiveName => Path.GetFileName(archive.Name);
            public long Size => entry.Size;

            public ArchivedAsset(FileArchive archive, FileArchiveEntry entry)
            {
                this.archive = archive;
                this.entry = entry;
            }

            public Stream OpenRead() => archive.GetStream(entry.Name);
        }
    }
}
