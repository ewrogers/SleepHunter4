using System.Collections.Generic;
using System.Collections.Concurrent;
using System.IO;
using System.Windows.Media.Imaging;
using System.Threading.Tasks;

using SleepHunter.IO;
using SleepHunter.Settings;

namespace SleepHunter.Media
{
    public sealed class IconManager
    {
        private static readonly IconManager instance = new IconManager();

        public static IconManager Instance => instance;

        private IconManager() { }

        private readonly ConcurrentDictionary<int, BitmapSource> skillIcons = new ConcurrentDictionary<int, BitmapSource>();
        private readonly ConcurrentDictionary<int, BitmapSource> spellIcons = new ConcurrentDictionary<int, BitmapSource>();
        private ColorPalette skillIconPalette;
        private ColorPalette spellIconPalette;
        private EpfImage skillIconImage;
        private EpfImage spellIconImage;

        public TaskScheduler Context { get; set; }

        public int SkillIconCount => skillIcons.Count;
        public int SpellIconCount => spellIcons.Count;

        public IEnumerable<BitmapSource> SkillIcons => skillIcons.Values;
        public IEnumerable<BitmapSource> SpellIcons => spellIcons.Values;

        public void AddSkillIcon(int index, BitmapSource source) => skillIcons[index] = source;

        public void AddSpellIcon(int index, BitmapSource source) => spellIcons[index] = source;

        public bool ContainsSkillIcon(int index) => skillIcons.ContainsKey(index);

        public bool ContainsSpellIcon(int index) => spellIcons.ContainsKey(index);

        public BitmapSource GetSkillIcon(int index)
        {
            if (skillIcons.ContainsKey(index))
                return skillIcons[index];

            var bitmap = RenderSkillIcon(index);

            if (bitmap == null)
                return null;

            var bitmapSource = bitmap.CreateBitmapSource();
            bitmapSource.Freeze();
            skillIcons[index] = bitmapSource;

            if (skillIcons.ContainsKey(index))
                return skillIcons[index];
            else
                return null;
        }

        public BitmapSource GetSpellIcon(int index)
        {
            if (spellIcons.ContainsKey(index))
                return spellIcons[index];

            var bitmap = RenderSpellIcon(index);

            if (bitmap == null)
                return null;

            var bitmapSource = bitmap.CreateBitmapSource();
            bitmapSource.Freeze();

            spellIcons[index] = bitmapSource;

            if (spellIcons.ContainsKey(index))
                return spellIcons[index];
            else
                return null;
        }

        public bool RemoveSkillIcon(int index) => skillIcons.TryRemove(index, out var _);

        public bool RemoveSpellIcon(int index) => spellIcons.TryRemove(index, out var _);

        public void ClearSkillIcons() => skillIcons.Clear();

        public void ClearSpellIcons() => spellIcons.Clear();

        private RenderedBitmap RenderSkillIcon(int index)
        {
            var settings = UserSettingsManager.Instance.Settings;

            if (skillIconPalette == null)
                skillIconPalette = GetColorPalette(settings.IconDataFile, settings.SkillPaletteFile);

            if (skillIconImage == null)
                skillIconImage = GetEpfImage(settings.IconDataFile, settings.SkillIconFile);

            if (skillIconPalette == null || skillIconImage == null)
                return null;

            if (index >= skillIconImage.FrameCount)
                return null;

            var frame = skillIconImage.GetFrameAt(index);
            var bitmap = RenderManager.Instance.Render(frame, skillIconPalette);

            return bitmap;
        }

        private RenderedBitmap RenderSpellIcon(int index)
        {
            var settings = UserSettingsManager.Instance.Settings;

            if (spellIconPalette == null)
                spellIconPalette = GetColorPalette(settings.IconDataFile, settings.SpellPaletteFile);

            if (spellIconImage == null)
                spellIconImage = GetEpfImage(settings.IconDataFile, settings.SpellIconFile);

            if (spellIconPalette == null || spellIconImage == null)
                return null;

            if (index >= spellIconImage.FrameCount)
                return null;

            var frame = spellIconImage.GetFrameAt(index);
            var bitmap = RenderManager.Instance.Render(frame, spellIconPalette);

            return bitmap;
        }

        public void ReloadIcons()
        {
            var settings = UserSettingsManager.Instance.Settings;

            skillIconPalette = GetColorPalette(settings.IconDataFile, settings.SkillPaletteFile);
            spellIconPalette = GetColorPalette(settings.IconDataFile, settings.SpellPaletteFile);

            skillIconImage = GetEpfImage(settings.IconDataFile, settings.SkillIconFile);
            spellIconImage = GetEpfImage(settings.IconDataFile, settings.SpellIconFile);
        }

        private static string GetRelativePath(string currentDirectory, string filename)
        {
            if (Path.IsPathRooted(filename))
                return filename;

            var directory = Path.GetDirectoryName(currentDirectory);
            var path = Path.Combine(directory, filename);

            return path;
        }

        private static ColorPalette GetColorPalette(string archiveFile, string paletteFile)
        {
            var archivePath = GetRelativePath(UserSettingsManager.Instance.Settings.ClientPath, archiveFile);
            var archive = FileArchiveManager.Instance.GetArchive(archivePath);

            if (archive == null || !archive.ContainsFile(paletteFile))
                return null;

            try
            {
                using (var inputStream = archive.GetStream(paletteFile))
                    return new ColorPalette(inputStream);
            }
            catch { return null; }
        }

        private static EpfImage GetEpfImage(string archiveFile, string epfFile)
        {
            var archivePath = GetRelativePath(UserSettingsManager.Instance.Settings.ClientPath, archiveFile);
            var archive = FileArchiveManager.Instance.GetArchive(archivePath);

            if (archive == null || !archive.ContainsFile(epfFile))
                return null;

            try
            {
                using (var inputStream = archive.GetStream(epfFile))
                    return new EpfImage(inputStream);
            }
            catch { return null; }
        }
    }
}
