using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace Tuanjie.Ai.Profiler
{
    /// <summary>
    /// Lightweight file search helper used by the profiler script tools.
    /// </summary>
    internal static class FileSearchHelper
    {
        public sealed class SearchMatch
        {
            public string FilePath { get; set; }
            public int LineNumber { get; set; }
            public string Line { get; set; }
        }

        public sealed class SearchResult
        {
            public List<SearchMatch> Matches { get; } = new List<SearchMatch>();
            public List<string> ScannedFiles { get; } = new List<string>();
        }

        const int k_MaxFiles = 200;
        const int k_MaxMatchesPerFile = 5;
        const int k_MaxTotalMatches = 200;

        public static SearchResult FindFiles(string searchPattern, string nameRegex)
        {
            var result = new SearchResult();
            var projectPath = PathUtils.ProjectPath;
            if (string.IsNullOrEmpty(projectPath) || !Directory.Exists(projectPath))
                return result;

            Regex nameMatcher = null;
            if (!string.IsNullOrEmpty(nameRegex))
            {
                try { nameMatcher = new Regex(nameRegex, RegexOptions.IgnoreCase); }
                catch (ArgumentException) { /* fallthrough: treat as no filter */ }
            }

            Regex contentMatcher = null;
            if (!string.IsNullOrEmpty(searchPattern))
            {
                try { contentMatcher = new Regex(searchPattern); }
                catch (ArgumentException) { /* fallthrough: treat as plain literal */ }
            }

            var assetsRoot = Path.Combine(projectPath, "Assets");
            var packagesRoot = Path.Combine(projectPath, "Packages");
            var scanned = 0;

            foreach (var root in new[] { assetsRoot, packagesRoot })
            {
                if (!Directory.Exists(root))
                    continue;
                IEnumerable<string> files;
                try
                {
                    files = Directory.EnumerateFiles(root, "*.*", SearchOption.AllDirectories);
                }
                catch (Exception)
                {
                    continue;
                }

                foreach (var fullPath in files)
                {
                    if (scanned >= k_MaxFiles)
                        return result;

                    var relative = PathUtils.GetProjectRelativePath(fullPath);
                    if (nameMatcher != null && !nameMatcher.IsMatch(relative))
                        continue;
                    if (nameMatcher == null && !relative.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
                        continue;

                    scanned++;
                    result.ScannedFiles.Add(relative);

                    string[] lines;
                    try { lines = File.ReadAllLines(fullPath); }
                    catch (Exception) { continue; }

                    if (contentMatcher == null)
                    {
                        // No content filter: surface a small preview.
                        var previewCount = Math.Min(lines.Length, 5);
                        for (var i = 0; i < previewCount; ++i)
                        {
                            result.Matches.Add(new SearchMatch
                            {
                                FilePath = relative,
                                LineNumber = i + 1,
                                Line = lines[i]
                            });
                            if (result.Matches.Count >= k_MaxTotalMatches)
                                return result;
                        }
                        continue;
                    }

                    var perFile = 0;
                    for (var i = 0; i < lines.Length; ++i)
                    {
                        if (!contentMatcher.IsMatch(lines[i]))
                            continue;
                        result.Matches.Add(new SearchMatch
                        {
                            FilePath = relative,
                            LineNumber = i + 1,
                            Line = lines[i]
                        });
                        perFile++;
                        if (perFile >= k_MaxMatchesPerFile)
                            break;
                        if (result.Matches.Count >= k_MaxTotalMatches)
                            return result;
                    }
                }
            }

            return result;
        }
    }
}
