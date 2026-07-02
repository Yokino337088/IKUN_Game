#if UNITY_2021_1_OR_NEWER
using System;
using System.Collections.Generic;
using System.Text;
using UnityEditor.Profiling;

namespace Tuanjie.Ai.Profiler
{
    /// <summary>
    /// Returns top individual samples in a frame, ranked by self time using the inverted view.
    /// </summary>
    internal static class MostExpensiveSamplesInFrameSummaryProvider
    {
        const int k_MaxSamples = 3;

        public static string GetSummary(FrameDataCache frameDataCache, int frameIndex)
        {
            var sb = new StringBuilder();

            var threadData = frameDataCache.GetCachedInvertedHierarchyFrameDataView(
                frameIndex,
                FrameDataViewUtils.MainThreadIndex,
                HierarchyFrameDataView.columnSelfTime);
            if (threadData == null || !threadData.valid)
                return $"No frame data available for frame {FrameDataViewUtils.GetDisplayFrameNumber(frameIndex)}.";

            var children = new List<int>();
            threadData.GetItemChildren(threadData.GetRootItemID(), children);

            var topSampleCount = Math.Min(k_MaxSamples, children.Count);
            sb.AppendLine($"Top {topSampleCount} Individual Samples in Frame {FrameDataViewUtils.GetDisplayFrameNumber(frameIndex)} on Main Thread (thread index 0) by Self Time:");
            sb.AppendLine("─────────────────────────────────────");
            for (var i = 0; i < topSampleCount; ++i)
                sb.AppendLine(SampleTimeSummaryProvider.GetChildSampleSummary(threadData, children[i]));

            return sb.ToString();
        }
    }
}
#endif
