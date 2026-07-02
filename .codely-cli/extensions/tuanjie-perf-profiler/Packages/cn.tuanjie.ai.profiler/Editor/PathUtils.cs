using System;
using System.IO;
using UnityEngine;

namespace Tuanjie.Ai.Profiler
{
    internal static class PathUtils
    {
        /// <summary>
        /// Returns the project root directory (the parent of Application.dataPath).
        /// </summary>
        public static string ProjectPath
        {
            get
            {
                var parent = Directory.GetParent(Application.dataPath);
                return parent != null ? parent.FullName : Application.dataPath;
            }
        }

        public static bool PathsEqual(string a, string b)
        {
            if (string.IsNullOrEmpty(a) || string.IsNullOrEmpty(b))
                return false;

            a = a.Replace('/', Path.DirectorySeparatorChar);
            b = b.Replace('/', Path.DirectorySeparatorChar);

            if (!Path.IsPathRooted(a))
                a = Path.GetFullPath(Path.Combine(ProjectPath, a));
            if (!Path.IsPathRooted(b))
                b = Path.GetFullPath(Path.Combine(ProjectPath, b));

            a = Path.GetFullPath(a);
            b = Path.GetFullPath(b);

            return string.Equals(
                a.TrimEnd(Path.DirectorySeparatorChar),
                b.TrimEnd(Path.DirectorySeparatorChar),
                StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Computes a project-relative path. Avoids Path.GetRelativePath, which is not
        /// available on all .NET runtimes Tuanjie may target.
        /// </summary>
        public static string GetProjectRelativePath(string fullPath)
        {
            if (string.IsNullOrEmpty(fullPath))
                return fullPath;

            var root = ProjectPath;
            var fullUri = new Uri(EnsureTrailingSeparator(Path.GetFullPath(fullPath)));
            var rootUri = new Uri(EnsureTrailingSeparator(Path.GetFullPath(root)));
            var relative = Uri.UnescapeDataString(rootUri.MakeRelativeUri(fullUri).ToString());
            return relative.TrimEnd('/').Replace('\\', '/');
        }

        static string EnsureTrailingSeparator(string path)
        {
            if (string.IsNullOrEmpty(path))
                return path;
            var last = path[path.Length - 1];
            if (last == Path.DirectorySeparatorChar || last == Path.AltDirectorySeparatorChar)
                return path;
            return path + Path.DirectorySeparatorChar;
        }
    }
}
