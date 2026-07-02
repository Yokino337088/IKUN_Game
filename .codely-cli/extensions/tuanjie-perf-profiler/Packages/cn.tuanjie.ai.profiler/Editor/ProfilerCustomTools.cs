using System;
using System.Collections.Generic;
using System.IO;
using Codely.Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using UnityTcp.Editor.Tools;
namespace Tuanjie.Ai.Profiler
{
    /// <summary>
    /// Custom tools that mirror Tuanjie AI Assistant's Profiler integration. Each tool is
    /// invoked through <see cref="ExecuteCustomTool"/> by name and returns a payload that
    /// matches the standard {success, message, data} envelope used by the bridge.
    /// </summary>
    public static class ProfilerCustomTools
    {
        const ulong k_GcMemoryAllocationThreshold = 8 * 1024; // 8KB

        // Marker path used to represent "use the current in-memory profiler session".
        const string k_ActiveSessionPath = ".active";

        // ───────────────────────────── Session ─────────────────────────────

        [ExecuteCustomTool.CustomTool(
            "Tuanjie.Profiler.Initialize",
            "Initializes a profiling session so that its data is available and returns information about the session. " +
            "Use this tool first when no profiling session is loaded yet.")]
        public static object Initialize(JObject parameters)
        {
            var sessionPath = parameters?["sessionPath"]?.ToString();

            // If the Profiler already has an in-memory session, keep using it. (We deliberately
            // don't peek at ProfilerWindow because UnityEditorInternal.ProfilerWindow is internal
            // on older Tuanjie versions; ProfilerDriver state is sufficient for the bridge.)
            if (SessionProvider.HasInMemorySession())
                return SuccessText("Using existing in-memory profiling session.");

            var profilingSessions = SessionProvider.GetProfilingSessions();
            if (SessionProvider.HasInMemorySession())
            {
                profilingSessions.Insert(0, new SessionProvider.ProfilerSessionInfo
                {
                    ProjectRelativePath = k_ActiveSessionPath,
                    FileName = "Active Session",
                });
            }

            SessionProvider.ProfilerSessionInfo selectedSession = null;

            if (string.IsNullOrEmpty(sessionPath))
            {
                if (profilingSessions.Count == 0)
                    return ErrorText("No profiling sessions found. Record one in the Tuanjie Profiler window before calling profiler tools.");

                // If there's a single candidate, pick it. Otherwise return the list so the
                // caller can re-invoke this tool with a specific sessionPath.
                if (profilingSessions.Count == 1)
                {
                    selectedSession = profilingSessions[0];
                }
                else
                {
                    var sessions = new List<object>(profilingSessions.Count);
                    foreach (var session in profilingSessions)
                    {
                        sessions.Add(new Dictionary<string, object>
                        {
                            ["projectRelativePath"] = session.ProjectRelativePath,
                            ["fileName"] = session.FileName,
                            ["fileSizeBytes"] = session.FileSizeBytes,
                            ["lastModified"] = session.LastModified.ToString("o")
                        });
                    }

                    return new Dictionary<string, object>
                    {
                        ["success"] = false,
                        ["message"] = "Multiple profiling sessions are available. Re-invoke Tuanjie.Profiler.Initialize with `sessionPath` set to one of the entries listed in `data.sessions`.",
                        ["data"] = new Dictionary<string, object>
                        {
                            ["sessions"] = sessions
                        }
                    };
                }
            }
            else
            {
                foreach (var session in profilingSessions)
                {
                    if (PathUtils.PathsEqual(session.ProjectRelativePath, sessionPath))
                    {
                        selectedSession = session;
                        break;
                    }
                }
                if (selectedSession == null)
                {
                    foreach (var session in profilingSessions)
                    {
                        if (session.FileName == sessionPath)
                        {
                            selectedSession = session;
                            break;
                        }
                    }
                }
                if (selectedSession == null)
                    return ErrorText($"Could not find the profiling session at path '{sessionPath}'.");
            }

            if (selectedSession.ProjectRelativePath == k_ActiveSessionPath)
                return SuccessText("Loaded in-memory profiling session.");

            // Loading a different capture invalidates cached views.
#if UNITY_2021_1_OR_NEWER
            FrameDataCacheHolder.Invalidate();
#endif

            ProfilerDriver.LoadProfile(selectedSession.ProjectRelativePath, false);
            return SuccessText($"Initialized session at path: {selectedSession.ProjectRelativePath}");
        }

        // ─────────────────────────── Time Summaries ────────────────────────

        [ExecuteCustomTool.CustomTool(
            "Tuanjie.Profiler.GetFrameRangeTopTimeSummary",
            "Return a summary of the time profiling data over a range of multiple frames. Requires Tuanjie 2021.1+.")]
        public static object GetFrameRangeTopTimeSummary(JObject parameters)
        {
#if UNITY_2021_1_OR_NEWER
            var startFrameIndex = ReadInt(parameters, "startFrameIndex");
            var lastFrameIndex = ReadInt(parameters, "lastFrameIndex");
            var targetFrameTime = ReadFloat(parameters, "targetFrameTime");
            return SuccessText(FrameRangeTimeSummaryProvider.GetSummary(
                FrameDataCacheHolder.Instance,
                startFrameIndex,
                lastFrameIndex,
                targetFrameTime));
#else
            return UnsupportedOnTuanjieBelow2021("Tuanjie.Profiler.GetFrameRangeTopTimeSummary");
#endif
        }

        [ExecuteCustomTool.CustomTool(
            "Tuanjie.Profiler.GetFrameTopTimeSamplesSummary",
            "Return a summary of the top samples of a specific frame based on the sample total time. Requires Tuanjie 2021.1+.")]
        public static object GetFrameTopTimeSamplesSummary(JObject parameters)
        {
#if UNITY_2021_1_OR_NEWER
            var frameIndex = ReadInt(parameters, "frameIndex");
            var targetFrameTime = ReadFloat(parameters, "targetFrameTime");
            return SuccessText(FrameTimeSummaryProvider.GetSummary(
                FrameDataCacheHolder.Instance,
                frameIndex,
                targetFrameTime));
#else
            return UnsupportedOnTuanjieBelow2021("Tuanjie.Profiler.GetFrameTopTimeSamplesSummary");
#endif
        }

        [ExecuteCustomTool.CustomTool(
            "Tuanjie.Profiler.GetFrameSelfTimeSamplesSummary",
            "Return a summary of the top individual samples in a specific frame based on the sample self time. Requires Tuanjie 2021.1+.")]
        public static object GetFrameSelfTimeSamplesSummary(JObject parameters)
        {
#if UNITY_2021_1_OR_NEWER
            var frameIndex = ReadInt(parameters, "frameIndex");
            return SuccessText(MostExpensiveSamplesInFrameSummaryProvider.GetSummary(
                FrameDataCacheHolder.Instance,
                frameIndex));
#else
            return UnsupportedOnTuanjieBelow2021("Tuanjie.Profiler.GetFrameSelfTimeSamplesSummary");
#endif
        }

        [ExecuteCustomTool.CustomTool(
            "Tuanjie.Profiler.GetSampleTimeSummary",
            "Returns a summary of a given profiler sample identified by its SampleId. Requires Tuanjie 2021.1+.")]
        public static object GetSampleTimeSummary(JObject parameters)
        {
#if UNITY_2021_1_OR_NEWER
            var frameIndex = ReadInt(parameters, "frameIndex");
            var threadName = ReadString(parameters, "threadName");
            var sampleId = ReadInt(parameters, "sampleId");
            return SuccessText(SampleTimeSummaryProvider.GetSummary(
                FrameDataCacheHolder.Instance,
                frameIndex,
                threadName,
                sampleId,
                false));
#else
            return UnsupportedOnTuanjieBelow2021("Tuanjie.Profiler.GetSampleTimeSummary");
#endif
        }

        [ExecuteCustomTool.CustomTool(
            "Tuanjie.Profiler.GetBottomUpSampleTimeSummary",
            "Returns a summary of time of a given profiler sample during the bottom-up analysis (use the BottomUpId returned by GetFrameSelfTimeSamplesSummary). Requires Tuanjie 2021.1+.")]
        public static object GetBottomUpSampleTimeSummary(JObject parameters)
        {
#if UNITY_2021_1_OR_NEWER
            var frameIndex = ReadInt(parameters, "frameIndex");
            var threadName = ReadString(parameters, "threadName");
            var bottomUpId = ReadInt(parameters, "bottomUpId");
            return SuccessText(SampleTimeSummaryProvider.GetSummary(
                FrameDataCacheHolder.Instance,
                frameIndex,
                threadName,
                bottomUpId,
                true));
#else
            return UnsupportedOnTuanjieBelow2021("Tuanjie.Profiler.GetBottomUpSampleTimeSummary");
#endif
        }

        [ExecuteCustomTool.CustomTool(
            "Tuanjie.Profiler.GetSampleTimeSummaryByMarkerPath",
            "Returns a summary of a given profiler sample specified by the Marker Id Path. Requires Tuanjie 2021.1+.")]
        public static object GetSampleTimeSummaryByMarkerPath(JObject parameters)
        {
#if UNITY_2021_1_OR_NEWER
            var frameIndex = ReadInt(parameters, "frameIndex");
            var threadName = ReadString(parameters, "threadName");
            var markerIdPath = ReadString(parameters, "markerIdPath");
            return SuccessText(SampleTimeSummaryProvider.GetSummary(
                FrameDataCacheHolder.Instance,
                frameIndex,
                threadName,
                markerIdPath));
#else
            return UnsupportedOnTuanjieBelow2021("Tuanjie.Profiler.GetSampleTimeSummaryByMarkerPath");
#endif
        }

        [ExecuteCustomTool.CustomTool(
            "Tuanjie.Profiler.GetRelatedSamplesTimeSummary",
            "Returns a summary of related samples on another thread that are executed at the same time as a given sample. Requires Tuanjie 2021.1+.")]
        public static object GetRelatedSamplesTimeSummary(JObject parameters)
        {
#if UNITY_2021_1_OR_NEWER
            var frameIndex = ReadInt(parameters, "frameIndex");
            var threadName = ReadString(parameters, "threadName");
            var sampleId = ReadInt(parameters, "sampleId");
            var relatedThreadName = ReadString(parameters, "relatedThreadName");
            return SuccessText(SampleTimeSummaryProvider.GetRelatedThreadSummary(
                FrameDataCacheHolder.Instance,
                frameIndex,
                threadName,
                sampleId,
                relatedThreadName,
                false));
#else
            return UnsupportedOnTuanjieBelow2021("Tuanjie.Profiler.GetRelatedSamplesTimeSummary");
#endif
        }

        // ────────────────────────── GC Summaries ───────────────────────────

        [ExecuteCustomTool.CustomTool(
            "Tuanjie.Profiler.GetOverallGcAllocationsSummary",
            "Return an overall summary of GC allocations in the available profiling data. Requires Tuanjie 2021.1+.")]
        public static object GetOverallGcAllocationsSummary(JObject parameters)
        {
#if UNITY_2021_1_OR_NEWER
            var cache = FrameDataCacheHolder.Instance;
            return SuccessText(FrameRangeGcAllocationSummaryProvider.GetSummary(
                cache,
                cache.FirstFrameIndex,
                cache.LastFrameIndex,
                k_GcMemoryAllocationThreshold));
#else
            return UnsupportedOnTuanjieBelow2021("Tuanjie.Profiler.GetOverallGcAllocationsSummary");
#endif
        }

        [ExecuteCustomTool.CustomTool(
            "Tuanjie.Profiler.GetFrameGcAllocationsSummary",
            "Return a summary of the top GC allocation samples in a specific frame. Requires Tuanjie 2021.1+.")]
        public static object GetFrameGcAllocationsSummary(JObject parameters)
        {
#if UNITY_2021_1_OR_NEWER
            var frameIndex = ReadInt(parameters, "frameIndex");
            return SuccessText(FrameGcAllocationSummaryProvider.GetSummary(
                FrameDataCacheHolder.Instance,
                frameIndex,
                k_GcMemoryAllocationThreshold));
#else
            return UnsupportedOnTuanjieBelow2021("Tuanjie.Profiler.GetFrameGcAllocationsSummary");
#endif
        }

        [ExecuteCustomTool.CustomTool(
            "Tuanjie.Profiler.GetFrameRangeGcAllocationsSummary",
            "Return a summary of the GC allocations over a range of multiple frames. Requires Tuanjie 2021.1+.")]
        public static object GetFrameRangeGcAllocationsSummary(JObject parameters)
        {
#if UNITY_2021_1_OR_NEWER
            var startFrameIndex = ReadInt(parameters, "startFrameIndex");
            var lastFrameIndex = ReadInt(parameters, "lastFrameIndex");
            return SuccessText(FrameRangeGcAllocationSummaryProvider.GetSummary(
                FrameDataCacheHolder.Instance,
                startFrameIndex,
                lastFrameIndex,
                k_GcMemoryAllocationThreshold));
#else
            return UnsupportedOnTuanjieBelow2021("Tuanjie.Profiler.GetFrameRangeGcAllocationsSummary");
#endif
        }

        [ExecuteCustomTool.CustomTool(
            "Tuanjie.Profiler.GetSampleGcAllocationSummary",
            "Returns a summary of GC allocations of a given profiler sample. Requires Tuanjie 2021.1+.")]
        public static object GetSampleGcAllocationSummary(JObject parameters)
        {
#if UNITY_2021_1_OR_NEWER
            var frameIndex = ReadInt(parameters, "frameIndex");
            var threadName = ReadString(parameters, "threadName");
            var sampleId = ReadInt(parameters, "sampleId");
            return SuccessText(SampleGcAllocationSummaryProvider.GetSummary(
                FrameDataCacheHolder.Instance,
                frameIndex,
                threadName,
                sampleId));
#else
            return UnsupportedOnTuanjieBelow2021("Tuanjie.Profiler.GetSampleGcAllocationSummary");
#endif
        }

        [ExecuteCustomTool.CustomTool(
            "Tuanjie.Profiler.GetSampleGcAllocationSummaryByMarkerPath",
            "Returns a summary of a given profiler sample specified by the Marker Id Path (GC view). Requires Tuanjie 2021.1+.")]
        public static object GetSampleGcAllocationSummaryByMarkerPath(JObject parameters)
        {
#if UNITY_2021_1_OR_NEWER
            var frameIndex = ReadInt(parameters, "frameIndex");
            var threadName = ReadString(parameters, "threadName");
            var markerIdPath = ReadString(parameters, "markerIdPath");
            return SuccessText(SampleGcAllocationSummaryProvider.GetSummary(
                FrameDataCacheHolder.Instance,
                frameIndex,
                threadName,
                markerIdPath));
#else
            return UnsupportedOnTuanjieBelow2021("Tuanjie.Profiler.GetSampleGcAllocationSummaryByMarkerPath");
#endif
        }

        // ────────────────────────── Script Lookup ──────────────────────────

        [ExecuteCustomTool.CustomTool(
            "Tuanjie.Profiler.FindScriptFile",
            "Search for content within project files and return matching files with their context. Pair `searchPattern` with a focused `nameRegex` to keep results small.")]
        public static object FindScriptFile(JObject parameters)
        {
            var searchPattern = ReadString(parameters, "searchPattern", optional: true);
            var nameRegex = ReadString(parameters, "nameRegex", optional: true);
            var result = FileSearchHelper.FindFiles(searchPattern, nameRegex);
            return new Dictionary<string, object>
            {
                ["success"] = true,
                ["message"] = $"Found {result.Matches.Count} match(es) across {result.ScannedFiles.Count} file(s).",
                ["data"] = new Dictionary<string, object>
                {
                    ["matches"] = MatchesToList(result.Matches),
                    ["scannedFiles"] = result.ScannedFiles
                }
            };
        }

        [ExecuteCustomTool.CustomTool(
            "Tuanjie.Profiler.GetFileContentLineCount",
            "Returns the number of lines of a script file.")]
        public static object GetFileContentLineCount(JObject parameters)
        {
            var filePath = ReadString(parameters, "filePath");
            var fullPath = ResolveProjectPath(filePath);
            if (!File.Exists(fullPath))
                return ErrorText($"File at path '{filePath}' not found");
            var lines = File.ReadAllLines(fullPath);
            return new Dictionary<string, object>
            {
                ["success"] = true,
                ["message"] = $"{lines.Length} lines",
                ["data"] = new Dictionary<string, object>
                {
                    ["filePath"] = filePath,
                    ["lineCount"] = lines.Length
                }
            };
        }

        [ExecuteCustomTool.CustomTool(
            "Tuanjie.Profiler.GetFileContent",
            "Returns the text content of a script file. Optional startLine (0-based) and lineCount (-1 for all) allow paginating large files.")]
        public static object GetFileContent(JObject parameters)
        {
            var filePath = ReadString(parameters, "filePath");
            var startLine = parameters?["startLine"]?.ToObject<int?>() ?? 0;
            var lineCount = parameters?["lineCount"]?.ToObject<int?>() ?? -1;

            var fullPath = ResolveProjectPath(filePath);
            if (!File.Exists(fullPath))
                return ErrorText($"File at path '{filePath}' not found");

            var lines = File.ReadAllLines(fullPath);
            var start = Math.Min(Math.Max(startLine, 0), lines.Length);
            if (lineCount == -1)
                lineCount = lines.Length;
            var count = Math.Min(lineCount, lines.Length - start);
            var content = string.Join(Environment.NewLine, lines, start, count);
            return new Dictionary<string, object>
            {
                ["success"] = true,
                ["message"] = $"Returned {count} line(s) starting at {start}.",
                ["data"] = new Dictionary<string, object>
                {
                    ["filePath"] = filePath,
                    ["startLine"] = start,
                    ["lineCount"] = count,
                    ["content"] = content
                }
            };
        }

        [ExecuteCustomTool.CustomTool(
            "Tuanjie.Profiler.GetMarkerCode",
            "Returns the C# code of a specific profiling marker, if available. Strips namespace, function brackets, and the `Inl_` prefix before searching.")]
        public static object GetMarkerCode(JObject parameters)
        {
            var markerName = ReadString(parameters, "markerName");
            var scriptTypeName = markerName;

            var indexOfFunctionBrackets = scriptTypeName?.LastIndexOf("()", StringComparison.Ordinal) ?? -1;
            if (!string.IsNullOrEmpty(scriptTypeName) && indexOfFunctionBrackets > 0)
                scriptTypeName = scriptTypeName.Substring(0, indexOfFunctionBrackets);

            var lastIndexOfColon = scriptTypeName?.LastIndexOf(":", StringComparison.Ordinal) ?? -1;
            if (!string.IsNullOrEmpty(scriptTypeName) && lastIndexOfColon > 0)
                scriptTypeName = scriptTypeName.Substring(lastIndexOfColon + 1);

            if (!string.IsNullOrEmpty(scriptTypeName))
            {
                var parts = scriptTypeName.Split('.');
                if (parts.Length >= 2)
                    scriptTypeName = parts[lastIndexOfColon > 0 ? 0 : parts.Length - 2];
            }

            // Strip URP `Inl_` prefix used for inlined render passes.
            if (!string.IsNullOrEmpty(scriptTypeName) && scriptTypeName.StartsWith("Inl_", StringComparison.Ordinal))
                scriptTypeName = scriptTypeName.Substring(4);

            if (string.IsNullOrEmpty(scriptTypeName))
                return new Dictionary<string, object>
                {
                    ["success"] = true,
                    ["message"] = "No usable type name extracted from marker.",
                    ["data"] = new Dictionary<string, object>
                    {
                        ["matches"] = new List<object>(),
                        ["scannedFiles"] = new List<string>()
                    }
                };

            var result = FileSearchHelper.FindFiles(scriptTypeName, "\\.cs$");
            return new Dictionary<string, object>
            {
                ["success"] = true,
                ["message"] = $"Found {result.Matches.Count} match(es) for '{scriptTypeName}' across {result.ScannedFiles.Count} file(s).",
                ["data"] = new Dictionary<string, object>
                {
                    ["query"] = scriptTypeName,
                    ["matches"] = MatchesToList(result.Matches),
                    ["scannedFiles"] = result.ScannedFiles
                }
            };
        }

        // ─────────────────────────── Helpers ───────────────────────────────

        static int ReadInt(JObject parameters, string name)
        {
            var token = parameters?[name];
            if (token == null || token.Type == JTokenType.Null)
                throw new ArgumentException($"Required integer parameter '{name}' is missing.");
            return token.ToObject<int>();
        }

        static float ReadFloat(JObject parameters, string name)
        {
            var token = parameters?[name];
            if (token == null || token.Type == JTokenType.Null)
                throw new ArgumentException($"Required float parameter '{name}' is missing.");
            return token.ToObject<float>();
        }

        static string ReadString(JObject parameters, string name, bool optional = false)
        {
            var token = parameters?[name];
            if (token == null || token.Type == JTokenType.Null)
            {
                if (optional)
                    return string.Empty;
                throw new ArgumentException($"Required string parameter '{name}' is missing.");
            }
            return token.ToString();
        }

        static string ResolveProjectPath(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
                return filePath;
            if (Path.IsPathRooted(filePath))
                return filePath;
            return Path.GetFullPath(Path.Combine(PathUtils.ProjectPath, filePath));
        }

        static List<object> MatchesToList(List<FileSearchHelper.SearchMatch> matches)
        {
            var list = new List<object>(matches.Count);
            foreach (var m in matches)
            {
                list.Add(new Dictionary<string, object>
                {
                    ["filePath"] = m.FilePath,
                    ["lineNumber"] = m.LineNumber,
                    ["line"] = m.Line
                });
            }
            return list;
        }

        static object SuccessText(string text)
        {
            return new Dictionary<string, object>
            {
                ["success"] = true,
                ["message"] = string.IsNullOrEmpty(text) ? "OK" : text,
                ["data"] = new Dictionary<string, object> { ["text"] = text ?? string.Empty }
            };
        }

        static object ErrorText(string text)
        {
            Debug.LogWarning($"[ProfilerCustomTools] {text}");
            return new Dictionary<string, object>
            {
                ["success"] = false,
                ["message"] = text
            };
        }

        static object UnsupportedOnTuanjieBelow2021(string toolName)
        {
            var msg =
                $"Tool '{toolName}' requires Tuanjie 2021.1 or newer. " +
                $"Current editor: {Application.unityVersion}. " +
                "The Tuanjie.Profiler.* analysis tools depend on UnityEditor.Profiling.HierarchyFrameDataView which is not reliably available on Tuanjie 2019/2020. " +
                "On this editor, the bridge still exposes Tuanjie.Profiler.Initialize, FindScriptFile, GetFileContent, GetFileContentLineCount, and GetMarkerCode.";
            return new Dictionary<string, object>
            {
                ["success"] = false,
                ["code"] = "tuanjie_version_unsupported",
                ["message"] = msg,
                ["data"] = new Dictionary<string, object>
                {
                    ["minimumTuanjieVersion"] = "2021.1",
                    ["currentTuanjieVersion"] = Application.unityVersion
                }
            };
        }
    }
}
