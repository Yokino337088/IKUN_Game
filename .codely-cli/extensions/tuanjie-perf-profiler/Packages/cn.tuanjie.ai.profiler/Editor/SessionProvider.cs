using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditorInternal;

namespace Tuanjie.Ai.Profiler
{
    /// <summary>
    /// Discovers `.data` profiler captures stored under the project root.
    /// </summary>
    internal static class SessionProvider
    {
        public sealed class ProfilerSessionInfo
        {
            public string ProjectRelativePath { get; set; }
            public string FileName { get; set; }
            public long FileSizeBytes { get; set; }
            public DateTime LastModified { get; set; }
        }

        const int k_MaxSessionResultsCount = 5;

        public static List<ProfilerSessionInfo> GetProfilingSessions()
        {
            var sessions = new List<ProfilerSessionInfo>();
            var projectPath = PathUtils.ProjectPath;

            string[] dataFiles;
            try
            {
                dataFiles = Directory.GetFiles(projectPath, "*.data", SearchOption.AllDirectories);
            }
            catch (Exception)
            {
                return sessions;
            }

            foreach (var filePath in dataFiles)
            {
                try
                {
                    var fileInfo = new FileInfo(filePath);
                    var relativePath = PathUtils.GetProjectRelativePath(filePath);
                    sessions.Add(new ProfilerSessionInfo
                    {
                        ProjectRelativePath = relativePath,
                        FileName = Path.GetFileNameWithoutExtension(fileInfo.Name),
                        FileSizeBytes = fileInfo.Length,
                        LastModified = fileInfo.LastWriteTime
                    });
                }
                catch (Exception)
                {
                    // Ignore problematic files.
                }
            }

            return sessions
                .OrderByDescending(s => s.LastModified)
                .Take(k_MaxSessionResultsCount)
                .ToList();
        }

        /// <summary>
        /// Returns true when the Profiler currently has an in-memory capture.
        /// </summary>
        public static bool HasInMemorySession()
        {
            return ProfilerDriver.firstFrameIndex != ProfilerDriver.lastFrameIndex
                   && ProfilerDriver.lastFrameIndex > 1;
        }
    }
}
