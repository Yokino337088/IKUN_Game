#if UNITY_2021_1_OR_NEWER
using System;
using System.Collections.Generic;
using System.Text;
using UnityEditor.Profiling;

namespace Tuanjie.Ai.Profiler
{
    /// <summary>
    /// Returns the top samples on the Main Thread of a single frame, ranked by total time.
    /// </summary>
    internal static class FrameTimeSummaryProvider
    {
        const int k_MaxSamples = 3;

        public static string GetSummary(FrameDataCache frameDataCache, int frameIndex, float targetFrameTime)
        {
            var sb = new StringBuilder();

            var threadData = frameDataCache.GetCachedHierarchyFrameDataView(
                frameIndex,
                FrameDataViewUtils.MainThreadIndex,
                HierarchyFrameDataView.columnTotalTime);
            if (threadData == null || !threadData.valid)
                return $"No frame data available for frame {FrameDataViewUtils.GetDisplayFrameNumber(frameIndex)}.";

            var children = new List<int>();
            threadData.GetItemChildren(threadData.GetRootItemID(), children);
            var topSampleCount = Math.Min(k_MaxSamples, children.Count);
            sb.AppendLine($"Top {children.Count} Samples on Main Thread (thread index 0) by Total Time:");
            sb.AppendLine("─────────────────────────────────────");
            for (var i = 0; i < topSampleCount; ++i)
                sb.AppendLine(SampleTimeSummaryProvider.GetChildSampleSummary(threadData, children[i]));

            sb.AppendLine("─────────────────────────────────────");
            var totalFrameTime = threadData.frameTimeMs;
            sb.AppendLine($"Total Frame Time: {totalFrameTime:F3}ms");

            if (totalFrameTime > targetFrameTime)
                sb.AppendLine($"Frame exceeds target time of {targetFrameTime:F3}ms by {(totalFrameTime - targetFrameTime):F3}ms");
            else
                sb.AppendLine($"Frame is within target time of {targetFrameTime:F3}ms");

            return sb.ToString();
        }
    }
}
#endif
