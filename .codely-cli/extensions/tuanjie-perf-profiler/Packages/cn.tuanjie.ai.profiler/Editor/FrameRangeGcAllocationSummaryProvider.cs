#if UNITY_2021_1_OR_NEWER
using System;
using System.Collections.Generic;
using System.Text;
using UnityEditor.Profiling;
using UnityEngine.Assertions;

namespace Tuanjie.Ai.Profiler
{
    /// <summary>
    /// Summarizes GC allocations over a contiguous range of frames.
    /// </summary>
    internal static class FrameRangeGcAllocationSummaryProvider
    {
        struct FrameValue
        {
            public float GcAllocSize;
            public int FrameIndex;

            public struct GcAllocSizeComparer : IComparer<FrameValue>
            {
                public int Compare(FrameValue x, FrameValue y)
                {
                    if (x.GcAllocSize != y.GcAllocSize)
                        return x.GcAllocSize.CompareTo(y.GcAllocSize);
                    return x.FrameIndex.CompareTo(y.FrameIndex);
                }
            }
        }

        struct FrameSummary
        {
            public FrameValue MedianFrame;
            public FrameValue LowerQuartileFrame;
            public FrameValue UpperQuartileFrame;
            public FrameValue MaxFrame;
        }

        public static string GetSummary(FrameDataCache frameDataCache, int startFrameIndex, int lastFrameIndex, ulong targetGcAllocSize)
        {
            var frameValues = GetFrameValues(frameDataCache, ref startFrameIndex, ref lastFrameIndex);
            if (frameValues == null || frameValues.Count == 0)
                return "No frame data available for the specified range.";

            frameValues.Sort(new FrameValue.GcAllocSizeComparer());
            var frameSummary = GetFrameSummary(frameValues);
            var sb = new StringBuilder();

            sb.AppendLine($"Frame Time Summary for the frame range [{FrameDataViewUtils.GetDisplayFrameNumber(startFrameIndex)}; {FrameDataViewUtils.GetDisplayFrameNumber(lastFrameIndex)}]:");
            sb.AppendLine("─────────────────────────────────────");

            int targetFrameBudgetExceedingFrameIndex = frameValues.BinarySearch(
                new FrameValue { GcAllocSize = targetGcAllocSize },
                new FrameValue.GcAllocSizeComparer());
            if (targetFrameBudgetExceedingFrameIndex < 0)
                targetFrameBudgetExceedingFrameIndex = ~targetFrameBudgetExceedingFrameIndex;
            int targetFrameBudgetExceedingFrameCount = frameValues.Count - targetFrameBudgetExceedingFrameIndex;
            if (targetFrameBudgetExceedingFrameCount > 0)
                sb.AppendLine($"{targetFrameBudgetExceedingFrameCount} frames out of {frameValues.Count} ({1.0f * targetFrameBudgetExceedingFrameCount / frameValues.Count:P1}) exceeding target frame GC Allocation of {targetGcAllocSize} bytes");
            else
                sb.AppendLine($"All frames are within the target frame GC Allocation budget of {targetGcAllocSize} bytes");

            if (IsEditorCapture(frameDataCache, startFrameIndex))
                sb.AppendLine("The capture is from Tuanjie Editor Play mode and contains EditorLoop samples which represent Editor overhead.");
            else
                sb.AppendLine("The capture is from a built player.");

            sb.AppendLine("Frame with Maximum GC Allocation:");
            sb.AppendLine($"  Frame {FrameDataViewUtils.GetDisplayFrameNumber(frameSummary.MaxFrame.FrameIndex)}: {frameSummary.MaxFrame.GcAllocSize} bytes of GC Allocation");
            sb.AppendLine(frameSummary.MaxFrame.GcAllocSize > targetGcAllocSize
                ? $"  Frame {FrameDataViewUtils.GetDisplayFrameNumber(frameSummary.MaxFrame.FrameIndex)} exceeds target frame GC Allocation budget"
                : $"  Frame {FrameDataViewUtils.GetDisplayFrameNumber(frameSummary.MaxFrame.FrameIndex)} is within target frame GC Allocation budget");

            sb.AppendLine("Frame with Median GC Allocation (50th percentile):");
            sb.AppendLine($"  Frame {FrameDataViewUtils.GetDisplayFrameNumber(frameSummary.MedianFrame.FrameIndex)}: {frameSummary.MedianFrame.GcAllocSize} bytes of GC Allocation");
            sb.AppendLine(frameSummary.MedianFrame.GcAllocSize > targetGcAllocSize
                ? $"  Frame {FrameDataViewUtils.GetDisplayFrameNumber(frameSummary.MedianFrame.FrameIndex)} exceeds target frame GC Allocation budget"
                : $"  Frame {FrameDataViewUtils.GetDisplayFrameNumber(frameSummary.MedianFrame.FrameIndex)} is within target frame GC Allocation budget");

            return sb.ToString();
        }

        static FrameSummary GetFrameSummary(IReadOnlyList<FrameValue> frameValues)
        {
            Assert.IsNotNull(frameValues);
            Assert.AreNotEqual(0, frameValues.Count);

            int medianIndex = GetIndexAtPercentage(frameValues.Count, 50);
            int lowerQuartileIndex = GetIndexAtPercentage(frameValues.Count, 25);
            int upperQuartileIndex = GetIndexAtPercentage(frameValues.Count, 75);
            int maxIndex = frameValues.Count - 1;
            return new FrameSummary
            {
                MedianFrame = frameValues[medianIndex],
                LowerQuartileFrame = frameValues[lowerQuartileIndex],
                UpperQuartileFrame = frameValues[upperQuartileIndex],
                MaxFrame = frameValues[maxIndex]
            };
        }

        static List<FrameValue> GetFrameValues(FrameDataCache frameDataCache, ref int startFrameIndex, ref int lastFrameIndex)
        {
            if (frameDataCache.FirstFrameIndex > startFrameIndex)
                startFrameIndex = frameDataCache.FirstFrameIndex;
            if (lastFrameIndex > frameDataCache.LastFrameIndex)
                lastFrameIndex = frameDataCache.LastFrameIndex;

            var frameValues = new List<FrameValue>();
            for (int frameIndex = startFrameIndex; frameIndex <= lastFrameIndex; frameIndex++)
            {
                using (var mainThreadData = frameDataCache.GetRawFrameDataView(frameIndex, FrameDataViewUtils.MainThreadIndex))
                {
                    if (mainThreadData == null || !mainThreadData.valid)
                        continue;
                    var counter = mainThreadData.GetCounterValueAsUInt64(FrameDataViewUtils.GcAllocationsInFrameCounterName);
                    var frameGcAlloc = counter ?? 0UL;
                    frameValues.Add(new FrameValue
                    {
                        GcAllocSize = frameGcAlloc,
                        FrameIndex = frameIndex
                    });
                }
            }

            return frameValues.Count == 0 ? null : frameValues;
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
