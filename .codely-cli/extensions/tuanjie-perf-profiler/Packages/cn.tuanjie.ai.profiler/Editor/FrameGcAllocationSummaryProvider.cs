#if UNITY_2021_1_OR_NEWER
using System;
using System.Collections.Generic;
using System.Text;
using UnityEditor.Profiling;

namespace Tuanjie.Ai.Profiler
{
    /// <summary>
    /// Returns top GC allocation samples in a frame across all threads.
    /// </summary>
    internal static class FrameGcAllocationSummaryProvider
    {
        const int k_MaxSamples = 3;

        struct GcAllocSample
        {
            public int ThreadIndex;
            public int ItemId;
            public ulong GcAllocSize;
        }

        public static string GetSummary(FrameDataCache frameDataCache, int frameIndex, ulong maxAllocationsPerFrame)
        {
            var mainThreadData = frameDataCache.GetCachedHierarchyFrameDataView(
                frameIndex,
                FrameDataViewUtils.MainThreadIndex,
                HierarchyFrameDataView.columnGcMemory);
            if (mainThreadData == null || !mainThreadData.valid)
                return $"No frame data available for frame {FrameDataViewUtils.GetDisplayFrameNumber(frameIndex)}.";

            var totalFrameGcAllocations = mainThreadData.GetCounterValueAsUInt64(FrameDataViewUtils.GcAllocationsInFrameCounterName) ?? 0UL;

            var topGcAllocSamples = new List<GcAllocSample>();
            var children = new List<int>();
            for (var threadIndex = 0; ; ++threadIndex)
            {
                var threadData = frameDataCache.GetCachedHierarchyFrameDataView(frameIndex, threadIndex, HierarchyFrameDataView.columnGcMemory);
                if (threadData == null || !threadData.valid)
                    break;

                children.Clear();
                threadData.GetItemChildren(threadData.GetRootItemID(), children);
                for (var i = 0; i < children.Count; ++i)
                {
                    var childId = children[i];
                    var gcAllocSize = (ulong)threadData.GetItemColumnDataAsDouble(childId, HierarchyFrameDataView.columnGcMemory);
                    if (gcAllocSize > 0)
                    {
                        topGcAllocSamples.Add(new GcAllocSample
                        {
                            ThreadIndex = threadIndex,
                            ItemId = childId,
                            GcAllocSize = gcAllocSize
                        });
                    }
                }
            }

            topGcAllocSamples.Sort((a, b) => b.GcAllocSize.CompareTo(a.GcAllocSize));

            var sb = new StringBuilder();
            var topSampleCount = Math.Min(k_MaxSamples, topGcAllocSamples.Count);
            sb.AppendLine($"Top {topSampleCount} Samples in Frame {FrameDataViewUtils.GetDisplayFrameNumber(frameIndex)} by GC Allocation:");
            sb.AppendLine("─────────────────────────────────────");
            for (var i = 0; i < topSampleCount; ++i)
            {
                var threadData = frameDataCache.GetCachedHierarchyFrameDataView(
                    frameIndex,
                    topGcAllocSamples[i].ThreadIndex,
                    HierarchyFrameDataView.columnGcMemory);
                sb.AppendLine(SampleGcAllocationSummaryProvider.GetChildSampleSummary(threadData, topGcAllocSamples[i].ItemId, totalFrameGcAllocations));
            }

            sb.AppendLine("─────────────────────────────────────");
            sb.AppendLine($"Total Frame GC Allocations: {totalFrameGcAllocations} bytes");

            if (totalFrameGcAllocations > maxAllocationsPerFrame)
                sb.AppendLine($"Frame exceeds target allocations of {maxAllocationsPerFrame} bytes by {(totalFrameGcAllocations - maxAllocationsPerFrame)} bytes");
            else
                sb.AppendLine($"Frame is within target allocations of {maxAllocationsPerFrame} bytes");

            return sb.ToString();
        }
    }
}
#endif
