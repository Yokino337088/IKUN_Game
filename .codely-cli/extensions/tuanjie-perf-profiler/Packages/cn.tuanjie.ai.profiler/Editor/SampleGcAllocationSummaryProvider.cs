#if UNITY_2021_1_OR_NEWER
using System;
using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEditor.Profiling;

namespace Tuanjie.Ai.Profiler
{
    /// <summary>
    /// Builds GC allocation summaries for individual profiler samples.
    /// </summary>
    internal static class SampleGcAllocationSummaryProvider
    {
        const int k_MaxSamples = 3;

        enum CallstackSampleType
        {
            None = 0,
            GCAlloc,
            WaitForJobGroupID,
            UnsafeUtilityMallocPersistent,
        }

        public static string GetSummary(FrameDataCache frameDataCache, int frameIndex, string threadName, string markerIdPath)
        {
            int threadIndex = FrameDataCache.GetThreadIndexByName(frameIndex, threadName);
            if (threadIndex == FrameDataView.invalidThreadIndex)
                throw new Exception($"Thread '{threadName}' not found in frame {frameIndex}.");

            var threadData = frameDataCache.GetCachedHierarchyFrameDataView(frameIndex, threadIndex, HierarchyFrameDataView.columnGcMemory);
            var markerStringIds = markerIdPath.Split('/');
            var markerIds = new List<int>(markerStringIds.Length);
            foreach (var markerStringId in markerStringIds)
            {
                if (!int.TryParse(markerStringId, out var parsed))
                    throw new Exception($"Invalid marker id '{markerStringId}' in path '{markerIdPath}'.");
                markerIds.Add(parsed);
            }

            var foundSampleId = false;
            var sampleId = threadData.GetRootItemID();
            var children = new List<int>();
            foreach (var markerId in markerIds)
            {
                children.Clear();
                threadData.GetItemChildren(sampleId, children);
                foundSampleId = false;
                foreach (var childrenId in children)
                {
                    if (threadData.GetItemMarkerID(childrenId) != markerId)
                        continue;
                    sampleId = childrenId;
                    foundSampleId = true;
                    break;
                }
                if (!foundSampleId)
                    break;
            }

            if (!foundSampleId)
                throw new Exception("Could not find sample id for " + markerIdPath);

            return GetSummary(frameDataCache, frameIndex, threadName, sampleId);
        }

        public static string GetSummary(FrameDataCache frameDataCache, int frameIndex, string threadName, int sampleId)
        {
            int threadIndex = FrameDataCache.GetThreadIndexByName(frameIndex, threadName);
            if (threadIndex == FrameDataView.invalidThreadIndex)
                throw new Exception($"Thread '{threadName}' not found in frame {frameIndex}.");

            var threadData = frameDataCache.GetCachedHierarchyFrameDataView(frameIndex, threadIndex, HierarchyFrameDataView.columnGcMemory);
            var frameGcAllocCounter = threadData.GetCounterValueAsUInt64(FrameDataViewUtils.GcAllocationsInFrameCounterName);
            var frameGcAllocSize = frameGcAllocCounter ?? 0UL;

            var sampleName = threadData.GetItemName(sampleId);

            var rawIndices = new List<int>();
            threadData.GetItemRawFrameDataViewIndices(sampleId, rawIndices);

            var children = new List<int>();
            threadData.GetItemChildren(sampleId, children);

            var sb = new StringBuilder();
            sb.AppendLine($"Gc Allocation Summary of {sampleName} (SampleId: {sampleId}, RawIndex: {(rawIndices.Count > 0 ? rawIndices[0].ToString() : "n/a")}):");
            sb.AppendLine("─────────────────────────────────────");

            var gcAllocMarkerId = FrameDataView.invalidMarkerId;

            var firstChildTotalValue = 0UL;
            var childSampleTotalValue = 0UL;
            foreach (var childId in children)
            {
                if (gcAllocMarkerId == FrameDataView.invalidMarkerId)
                    gcAllocMarkerId = threadData.GetMarkerId(FrameDataViewUtils.GcAllocName);
                if (threadData.GetItemMarkerID(childId) == gcAllocMarkerId)
                    continue;

                var childGcAlloc = (ulong)threadData.GetItemColumnDataAsDouble(childId, HierarchyFrameDataView.columnGcMemory);
                childSampleTotalValue += childGcAlloc;
                if (firstChildTotalValue == 0)
                    firstChildTotalValue = childGcAlloc;
            }

            var topSampleCount = Math.Min(k_MaxSamples, children.Count);
            var sampleTotalValue = (ulong)threadData.GetItemColumnDataAsDouble(sampleId, HierarchyFrameDataView.columnGcMemory);
            var sampleSelfValue = sampleTotalValue >= childSampleTotalValue
                ? sampleTotalValue - childSampleTotalValue
                : 0UL;

            if (IsSignificantChildContributor(sampleSelfValue, sampleTotalValue) || firstChildTotalValue < sampleSelfValue)
            {
                sb.AppendLine($"Sample Self Gc Allocation: {sampleSelfValue} is significant on its own. Use source code to analyze further");
                sb.AppendLine("─────────────────────────────────────");
            }

            if (IsSignificantChildContributor(firstChildTotalValue, sampleTotalValue))
            {
                sb.AppendLine("Top Child Samples to Investigate:");
                for (var i = 0; i < topSampleCount; ++i)
                {
                    var childId = children[i];
                    var childTotalValue = (ulong)threadData.GetItemColumnDataAsDouble(childId, HierarchyFrameDataView.columnGcMemory);
                    if (!IsSignificantChildContributor(childTotalValue, sampleTotalValue))
                        break;
                    sb.AppendLine(GetChildSampleSummary(threadData, childId, frameGcAllocSize));
                }
                sb.AppendLine("─────────────────────────────────────");
            }

            CallstackSampleType callstackSampleType;
            switch (sampleName)
            {
                case "GC.Alloc": callstackSampleType = CallstackSampleType.GCAlloc; break;
                case "WaitForJobGroupID": callstackSampleType = CallstackSampleType.WaitForJobGroupID; break;
                case "UnsafeUtility.Malloc(Persistent)": callstackSampleType = CallstackSampleType.UnsafeUtilityMallocPersistent; break;
                default: callstackSampleType = CallstackSampleType.None; break;
            }
            var sampleInstanceCountAtScope = threadData.GetItemMergedSamplesCount(sampleId);
            if (callstackSampleType != CallstackSampleType.None)
                AddCallstackInformation(callstackSampleType, sb, threadData, sampleId, sampleInstanceCountAtScope);

            return sb.ToString();
        }

        public static string GetChildSampleSummary(HierarchyFrameDataView data, int sampleId, ulong frameGcAllocSize)
        {
            var sb = new StringBuilder();
            var sampleName = data.GetItemName(sampleId);
            var gcAllocSize = (ulong)data.GetItemColumnDataAsDouble(sampleId, HierarchyFrameDataView.columnGcMemory);
            var rawIndices = new List<int>();
            data.GetItemRawFrameDataViewIndices(sampleId, rawIndices);

            var relatedObjectName = SampleTimeSummaryProvider.GetRelatedObjectName(data, sampleId);

            sb.AppendLine(sampleName);
            sb.AppendLine($"   Thread Index: {data.threadIndex}");
            sb.AppendLine($"   SampleId: {sampleId}");
            sb.AppendLine($"   RawIndex: {(rawIndices.Count > 0 ? rawIndices[0].ToString() : "n/a")}");
            var divisor = frameGcAllocSize > 0 ? frameGcAllocSize : 1UL;
            sb.AppendLine($"   Gc Alocation: {gcAllocSize} bytes ({1.0f * gcAllocSize / divisor:P1} of Frame Gc Allocation)");
            if (!string.IsNullOrEmpty(relatedObjectName))
                sb.AppendLine($"   Object Name: {relatedObjectName}");

            return sb.ToString();
        }

        static void AddCallstackInformation(
            CallstackSampleType callstackSampleType,
            StringBuilder sb,
            HierarchyFrameDataView threadData,
            int sampleId,
            int sampleInstanceCountAtScope)
        {
            var callSites = new List<ulong>();
            sb.AppendLine("Callstack information for sample instances:");
            sb.AppendLine("─────────────────────────────────────");
            var totalGCAmount = 0L;
            var foundCallstacks = false;
            for (var i = 0; i < sampleInstanceCountAtScope; i++)
            {
                var size = 0L;
                try
                {
                    if (callstackSampleType == CallstackSampleType.GCAlloc)
                        size = threadData.GetItemMergedSamplesMetadataAsLong(sampleId, i, HierarchyFrameDataView.columnGcMemory);
                    callSites.Clear();
                    threadData.GetItemMergedSampleCallstack(sampleId, i, callSites);
                }
                catch (Exception)
                {
                    continue;
                }
                if (callSites.Count > 0)
                {
                    if (callstackSampleType == CallstackSampleType.GCAlloc)
                        sb.AppendLine($"Sample instance #{i} represents a managed memory allocation of {EditorUtility.FormatBytes(size)}.\n");
                    sb.AppendLine($"The callstack for this allocation is: \n {threadData.ResolveItemMergedSampleCallstack(sampleId, i)}");
                    foundCallstacks = true;
                }
                totalGCAmount += size;
            }
            if (!foundCallstacks)
                sb.AppendLine("The Profiler data was gathered without turning on \"Capture Callstacks for GC.Alloc\"");
            if (callstackSampleType == CallstackSampleType.GCAlloc)
                sb.AppendLine($"Total managed memory allocated: {totalGCAmount}");
        }

        static bool IsSignificantChildContributor(ulong childGcAlloc, ulong parentGcAlloc)
        {
            const float kSignificanceFactor = 0.1f;
            return childGcAlloc >= parentGcAlloc * kSignificanceFactor;
        }
    }
}
#endif
