#if UNITY_2021_1_OR_NEWER
using System;
using System.Collections.Generic;
using System.Text;
using UnityEditor.Profiling;
using UnityEngine.Assertions;

namespace Tuanjie.Ai.Profiler
{
    /// <summary>
    /// Summarizes frame times over a contiguous range of frames.
    /// </summary>
    internal static class FrameRangeTimeSummaryProvider
    {
        struct FrameTime
        {
            public float CpuTimeMs;
            public float CpuActiveTimeMs;
            public float GpuTimeMs;
            public int FrameIndex;

            public struct CpuTimeComparer : IComparer<FrameTime>
            {
                public int Compare(FrameTime x, FrameTime y)
                {
                    if (x.CpuTimeMs != y.CpuTimeMs)
                        return x.CpuTimeMs.CompareTo(y.CpuTimeMs);
                    if (x.CpuActiveTimeMs != y.CpuActiveTimeMs)
                        return x.CpuActiveTimeMs.CompareTo(y.CpuActiveTimeMs);
                    return x.FrameIndex.CompareTo(y.FrameIndex);
                }
            }
        }

        struct FrameSummary
        {
            public FrameTime MedianFrame;
            public FrameTime LowerQuartileFrame;
            public FrameTime UpperQuartileFrame;
            public FrameTime MaxFrame;
        }

        public static string GetSummary(FrameDataCache frameDataCache, int startFrameIndex, int lastFrameIndex, float targetFrameTime)
        {
            var frameTimes = GetFrameTimes(frameDataCache, ref startFrameIndex, ref lastFrameIndex);
            if (frameTimes == null || frameTimes.Count == 0)
                return "No frame data available for the specified range.";

            frameTimes.Sort(new FrameTime.CpuTimeComparer());
            var frameSummary = GetFrameSummary(frameTimes);
            var sb = new StringBuilder();

            sb.AppendLine($"Frame Time Summary for the frame range [{FrameDataViewUtils.GetDisplayFrameNumber(startFrameIndex)}; {FrameDataViewUtils.GetDisplayFrameNumber(lastFrameIndex)}]:");
            sb.AppendLine("─────────────────────────────────────");

            int targetFrameBudgetExceedingFrameIndex = frameTimes.BinarySearch(
                new FrameTime { CpuTimeMs = targetFrameTime },
                new FrameTime.CpuTimeComparer());
            if (targetFrameBudgetExceedingFrameIndex < 0)
                targetFrameBudgetExceedingFrameIndex = ~targetFrameBudgetExceedingFrameIndex;
            int targetFrameBudgetExceedingFrameCount = frameTimes.Count - targetFrameBudgetExceedingFrameIndex;
            if (targetFrameBudgetExceedingFrameCount > 0)
                sb.AppendLine($"{targetFrameBudgetExceedingFrameCount} frames out of {frameTimes.Count} ({1.0f * targetFrameBudgetExceedingFrameCount / frameTimes.Count:P1}) exceeding target frame time of {targetFrameTime}ms");
            else
                sb.AppendLine($"All frames are within the target frame time of {targetFrameTime}ms");

            if (IsEditorCapture(frameDataCache, startFrameIndex))
                sb.AppendLine("The capture is from Tuanjie Editor Play mode and contains EditorLoop samples which represent Editor overhead.");
            else
                sb.AppendLine("The capture is from a built player.");

            sb.AppendLine("Frame with Maximum CPU Time:");
            sb.AppendLine($"  Frame {FrameDataViewUtils.GetDisplayFrameNumber(frameSummary.MaxFrame.FrameIndex)}: {frameSummary.MaxFrame.CpuTimeMs:F2}ms CPU Time, {frameSummary.MaxFrame.GpuTimeMs:F2}ms GPU");
            sb.AppendLine(frameSummary.MaxFrame.CpuTimeMs > targetFrameTime
                ? $"  Frame {FrameDataViewUtils.GetDisplayFrameNumber(frameSummary.MaxFrame.FrameIndex)} exceeds target frame time"
                : $"  Frame {FrameDataViewUtils.GetDisplayFrameNumber(frameSummary.MaxFrame.FrameIndex)} is within target frame time");

            sb.AppendLine("Frame with Median CPU Time (50th percentile):");
            sb.AppendLine($"  Frame {FrameDataViewUtils.GetDisplayFrameNumber(frameSummary.MedianFrame.FrameIndex)}: {frameSummary.MedianFrame.CpuTimeMs:F2}ms CPU Time, {frameSummary.MedianFrame.GpuTimeMs:F2}ms GPU Time");
            sb.AppendLine(frameSummary.MedianFrame.CpuTimeMs > targetFrameTime
                ? $"  Frame {FrameDataViewUtils.GetDisplayFrameNumber(frameSummary.MedianFrame.FrameIndex)} exceeds target frame time"
                : $"  Frame {FrameDataViewUtils.GetDisplayFrameNumber(frameSummary.MedianFrame.FrameIndex)} is within target frame time");

            return sb.ToString();
        }

        static FrameSummary GetFrameSummary(IReadOnlyList<FrameTime> frameTimes)
        {
            Assert.IsNotNull(frameTimes);
            Assert.AreNotEqual(0, frameTimes.Count);

            int medianIndex = GetIndexAtPercentage(frameTimes.Count, 50);
            int lowerQuartileIndex = GetIndexAtPercentage(frameTimes.Count, 25);
            int upperQuartileIndex = GetIndexAtPercentage(frameTimes.Count, 75);
            int maxIndex = frameTimes.Count - 1;
            return new FrameSummary
            {
                MedianFrame = frameTimes[medianIndex],
                LowerQuartileFrame = frameTimes[lowerQuartileIndex],
                UpperQuartileFrame = frameTimes[upperQuartileIndex],
                MaxFrame = frameTimes[maxIndex]
            };
        }

        static List<FrameTime> GetFrameTimes(FrameDataCache frameDataCache, ref int startFrameIndex, ref int lastFrameIndex)
        {
            if (frameDataCache.FirstFrameIndex > startFrameIndex)
                startFrameIndex = frameDataCache.FirstFrameIndex;
            if (lastFrameIndex > frameDataCache.LastFrameIndex)
                lastFrameIndex = frameDataCache.LastFrameIndex;

            var frameTimes = new List<FrameTime>();
            for (int frameIndex = startFrameIndex; frameIndex <= lastFrameIndex; frameIndex++)
            {
                using (var mainThreadData = frameDataCache.GetRawFrameDataView(frameIndex, FrameDataViewUtils.MainThreadIndex))
                {
                    if (mainThreadData == null || !mainThreadData.valid)
                        continue;

                    float cpuTimeMs = mainThreadData.frameTimeMs;
                    var cpuActiveDurationNs = 0UL;
                    var cpuMainThreadActiveDurationNs = mainThreadData.GetCounterValueAsUInt64(FrameDataViewUtils.MainThreadActiveTimeCounterName);
                    var cpuRenderThreadActiveDurationNs = mainThreadData.GetCounterValueAsUInt64(FrameDataViewUtils.RenderThreadActiveTimeCounterName);
                    if (cpuMainThreadActiveDurationNs.HasValue && cpuRenderThreadActiveDurationNs.HasValue)
                        cpuActiveDurationNs = Math.Max(cpuMainThreadActiveDurationNs.Value, cpuRenderThreadActiveDurationNs.Value);

                    // Frame Timing Manager reports GPU timings at a fixed offset of four frames.
                    const int k_FrameTimingManagerFixedDelay = 4;
                    var gpuFrameIndex = frameIndex + k_FrameTimingManagerFixedDelay;
                    var gpuTimeNs = 0UL;
                    if (gpuFrameIndex < frameDataCache.LastFrameIndex)
                    {
                        using (var mainThreadDataOf4FramesForward = frameDataCache.GetRawFrameDataView(gpuFrameIndex, FrameDataViewUtils.MainThreadIndex))
                        {
                            if (mainThreadDataOf4FramesForward != null && mainThreadDataOf4FramesForward.valid)
                            {
                                var counter = mainThreadDataOf4FramesForward.GetCounterValueAsUInt64(FrameDataViewUtils.GpuFrameTimeCounterName);
                                if (counter.HasValue)
                                    gpuTimeNs = counter.Value;
                            }
                        }
                    }

                    float cpuActiveTimeMs = cpuActiveDurationNs * 0.000001f;
                    frameTimes.Add(new FrameTime
                    {
                        CpuTimeMs = cpuTimeMs,
                        CpuActiveTimeMs = cpuActiveTimeMs,
                        GpuTimeMs = gpuTimeNs * 0.000001f,
                        FrameIndex = frameIndex
                    });
                }
            }

            return frameTimes.Count == 0 ? null : frameTimes;
        }

        static int GetIndexAtPercentage(int count, float percent)
        {
            Assert.AreNotEqual(0, count);
            return (int)((count - 1) * percent / 100f);
        }

        static bool IsEditorCapture(FrameDataCache frameDataCache, int frameIndex)
        {
            using (var mainThreadData = frameDataCache.GetHierarchyFrameDataView(
                       frameIndex,
                       FrameDataViewUtils.MainThreadIndex,
                       HierarchyFrameDataView.ViewModes.MergeSamplesWithTheSameName,
                       HierarchyFrameDataView.columnDontSort,
                       false))
            {
                if (mainThreadData == null || !mainThreadData.valid)
                    return false;
                var children = new List<int>();
                mainThreadData.GetItemChildren(mainThreadData.GetRootItemID(), children);
                foreach (var childId in children)
                {
                    if (mainThreadData.GetItemName(childId) == FrameDataViewUtils.EditorLoopName)
                        return true;
                }
            }
            return false;
        }
    }
}
#endif
