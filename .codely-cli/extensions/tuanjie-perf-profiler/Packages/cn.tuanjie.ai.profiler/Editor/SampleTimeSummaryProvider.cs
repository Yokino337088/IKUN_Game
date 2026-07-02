#if UNITY_2021_1_OR_NEWER
using System;
using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEditor.Profiling;

namespace Tuanjie.Ai.Profiler
{
    /// <summary>
    /// Builds time-focused summaries for individual profiler samples and related thread overlap.
    /// </summary>
    internal static class SampleTimeSummaryProvider
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

            var threadData = frameDataCache.GetCachedHierarchyFrameDataView(frameIndex, threadIndex, HierarchyFrameDataView.columnTotalTime);
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

            return GetSummary(frameDataCache, frameIndex, threadData.threadName, sampleId, false);
        }

        public static string GetSummary(FrameDataCache frameDataCache, int frameIndex, string threadName, int sampleId, bool inverted)
        {
            int threadIndex = FrameDataCache.GetThreadIndexByName(frameIndex, threadName);
            if (threadIndex == FrameDataView.invalidThreadIndex)
                throw new Exception($"Thread '{threadName}' not found in frame {frameIndex}.");

            var threadData = inverted
                ? frameDataCache.GetCachedInvertedHierarchyFrameDataView(frameIndex, threadIndex, HierarchyFrameDataView.columnSelfTime)
                : frameDataCache.GetCachedHierarchyFrameDataView(frameIndex, threadIndex, HierarchyFrameDataView.columnTotalTime);

            var sampleName = threadData.GetItemName(sampleId);
            var rawIndices = new List<int>();
            threadData.GetItemRawFrameDataViewIndices(sampleId, rawIndices);

            var children = new List<int>();
            threadData.GetItemChildren(sampleId, children);

            var sb = new StringBuilder();

            var firstChildTotalTime = 0f;
            var topSampleCount = Math.Min(k_MaxSamples, children.Count);
            if (topSampleCount > 0)
                firstChildTotalTime = threadData.GetItemColumnDataAsFloat(children[0], HierarchyFrameDataView.columnTotalTime);

            sb.AppendLine($"Time Summary of {sampleName} (SampleId: {sampleId}, RawIndex: {(rawIndices.Count > 0 ? rawIndices[0].ToString() : "n/a")}):");
            sb.AppendLine("─────────────────────────────────────");

            var sampleTotalTime = threadData.GetItemColumnDataAsFloat(sampleId, HierarchyFrameDataView.columnTotalTime);
            var sampleSelfTime = threadData.GetItemColumnDataAsFloat(sampleId, HierarchyFrameDataView.columnSelfTime);

            if (IsSignificantChildTimeContributor(sampleSelfTime, sampleTotalTime) || firstChildTotalTime < sampleSelfTime)
            {
                sb.AppendLine($"Sample Self Time: {sampleSelfTime}ms is significant on its own. Use source code to analyze further");
                sb.AppendLine("─────────────────────────────────────");
            }

            if (IsSignificantChildTimeContributor(firstChildTotalTime, sampleTotalTime))
            {
                sb.AppendLine("Top Child Samples to Investigate:");
                for (var i = 0; i < topSampleCount; ++i)
                {
                    var childId = children[i];
                    var childTotalTime = threadData.GetItemColumnDataAsFloat(childId, HierarchyFrameDataView.columnTotalTime);
                    if (!IsSignificantChildTimeContributor(childTotalTime, sampleTotalTime))
                        break;
                    sb.AppendLine(GetChildSampleSummary(threadData, childId));
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

        public static string GetChildSampleSummary(HierarchyFrameDataView data, int sampleId)
        {
            var sb = new StringBuilder();
            var sampleName = data.GetItemName(sampleId);
            var sampleTime = data.GetItemColumnDataAsSingle(sampleId, HierarchyFrameDataView.columnTotalTime);
            var rawIndices = new List<int>();
            data.GetItemRawFrameDataViewIndices(sampleId, rawIndices);

            string relatedObjectName = GetRelatedObjectName(data, sampleId);

            sb.AppendLine(sampleName);
#if UNITY_6000_0_OR_NEWER
            sb.AppendLine(data.viewMode.HasFlag(HierarchyFrameDataView.ViewModes.InvertHierarchy)
                ? $"   BottomUpId: {sampleId}"
                : $"   SampleId: {sampleId}");
#else
            sb.AppendLine($"   SampleId: {sampleId}");
#endif
            sb.AppendLine($"   RawIndex: {(rawIndices.Count > 0 ? rawIndices[0].ToString() : "n/a")}");
            var frameTime = data.frameTimeMs > 0f ? data.frameTimeMs : 1f;
            sb.AppendLine($"   Total Time: {sampleTime:F3}ms ({sampleTime / frameTime:P1} of Frame Time)");
            if (!string.IsNullOrEmpty(relatedObjectName))
                sb.AppendLine($"   Object Name: {relatedObjectName}");

            return sb.ToString();
        }

        internal static string GetRelatedObjectName(HierarchyFrameDataView data, int sampleId)
        {
            string relatedObjectName = null;
            try
            {
                var relatedInstanceId = data.GetItemInstanceID(sampleId);
                if (relatedInstanceId != 0 && data.GetUnityObjectInfo(relatedInstanceId, out var objInfo))
                {
                    relatedObjectName = objInfo.name;
                    var relatedGameObjectInstanceId = data.GetItemInstanceID(objInfo.relatedGameObjectInstanceId);
                    if (relatedGameObjectInstanceId != 0 && data.GetUnityObjectInfo(relatedGameObjectInstanceId, out var gameObjInfo))
                    {
                        if (relatedObjectName != gameObjInfo.name)
                            relatedObjectName += " of '" + gameObjInfo.name + "' GameObject";
                    }
                }
            }
            catch (Exception)
            {
                // Older Tuanjie versions or capture types may not expose object info; ignore.
            }
            return relatedObjectName;
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

        public static string GetRelatedThreadSummary(
            FrameDataCache frameDataCache,
            int frameIndex,
            string threadName,
            int sampleId,
            string relatedThreadName,
            bool inverted)
        {
            int relatedThreadIndex = FrameDataCache.GetThreadIndexByName(frameIndex, relatedThreadName);
            if (relatedThreadIndex == FrameDataView.invalidThreadIndex)
                return $"No related thread {relatedThreadName} found";

            int threadIndex = FrameDataCache.GetThreadIndexByName(frameIndex, threadName);
            if (threadIndex == FrameDataView.invalidThreadIndex)
                return $"Thread '{threadName}' not found in frame {frameIndex}.";

            var threadData = inverted
                ? frameDataCache.GetCachedInvertedHierarchyFrameDataView(frameIndex, threadIndex, HierarchyFrameDataView.columnSelfTime)
                : frameDataCache.GetCachedHierarchyFrameDataView(frameIndex, threadIndex, HierarchyFrameDataView.columnTotalTime);

            var sampleName = threadData.GetItemName(sampleId);
            var sampleStartTime = threadData.GetItemColumnDataAsDouble(sampleId, HierarchyFrameDataView.columnStartTime);
            var sampleTotalTime = threadData.GetItemColumnDataAsDouble(sampleId, HierarchyFrameDataView.columnTotalTime);

            var relatedThreadData = inverted
                ? frameDataCache.GetCachedInvertedHierarchyFrameDataView(frameIndex, relatedThreadIndex, HierarchyFrameDataView.columnSelfTime)
                : frameDataCache.GetCachedHierarchyFrameDataView(frameIndex, relatedThreadIndex, HierarchyFrameDataView.columnTotalTime);

            var relatedSamples = new List<int>();
            var children = new List<int>();
            relatedThreadData.GetItemChildren(relatedThreadData.GetRootItemID(), children);
            var lookupQueue = new Queue<int>(children);
            while (lookupQueue.Count > 0)
            {
                var relatedSampleIndex = lookupQueue.Dequeue();
                var relatedSampleStartTime = relatedThreadData.GetItemColumnDataAsDouble(relatedSampleIndex, HierarchyFrameDataView.columnStartTime);
                var relatedSampleTotalTime = relatedThreadData.GetItemColumnDataAsDouble(relatedSampleIndex, HierarchyFrameDataView.columnTotalTime);
                if (sampleStartTime < relatedSampleStartTime + relatedSampleTotalTime
                    && relatedSampleStartTime < sampleStartTime + sampleTotalTime)
                {
                    relatedSamples.Add(relatedSampleIndex);
                }
            }

            if (relatedSamples.Count == 0)
                return "No related samples found";

            var sb = new StringBuilder();
            sb.AppendLine($"Related samples in thread {relatedThreadName} overlapping with sample {sampleName}");
            sb.AppendLine("─────────────────────────────────────");
            foreach (var relatedSampleIndex in relatedSamples)
                sb.AppendLine(GetChildSampleSummary(relatedThreadData, relatedSampleIndex));

            return sb.ToString();
        }

        static bool IsSignificantChildTimeContributor(float time, float totalTime)
        {
            const float kSignificantTimeFactor = 0.1f;
            return time >= totalTime * kSignificantTimeFactor;
        }
    }
}
#endif
