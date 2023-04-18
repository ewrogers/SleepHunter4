using System;
using System.IO;

namespace SleepHunter.IO
{
    public static class PathHelper
    {
        public static string RootPath(string baseDirectory, string path)
        {
            if (Path.IsPathRooted(path))
                return path;

            var attributes = File.GetAttributes(baseDirectory);

            var directory = baseDirectory;

            if (!attributes.HasFlag(FileAttributes.Directory))
                directory = Path.GetDirectoryName(baseDirectory);

            return Path.Combine(directory, path);
        }

        public static string MakeApplicationPath(string path)
        {
            if (Path.IsPathRooted(path))
                return path;

            var appPath = AppDomain.CurrentDomain.BaseDirectory;
            return Path.Combine(appPath, path);
        }
    }
}
