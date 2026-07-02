using System;
#if UNITY_2021_1_OR_NEWER
using UnityEditor.Profiling;
#endif

namespace Tuanjie.Ai.Profiler
{
    /// <summary>
    /// Helper constants and extensions for Profiler frame data processing.
    /// Mirrors Tuanjie AI Assistant's FrameDataViewUtils.
    /// The FrameDataView extension is only compiled on Tuanjie 2021.1+, where the
    /// UnityEditor.Profiling.FrameDataView API surface is stable.
    /// </summary>
    internal static class FrameDataViewUtils
    {
        public const int MainThreadIndex = 0;
        public const int RenderThreadIndex = 1;

        public const string EditorLoopName = "EditorLoop";
        public const string PlayerLoopName = "PlayerLoop";
        public const string GcAllocName = "GC.Alloc";

        public const string MainThreadActiveTimeCounterName = "CPU Main Thread Active Time";
        public const string RenderThreadActiveTimeCounterName = "CPU Render Thread Active Time";
        public const string GpuFrameTimeCounterName = "GPU Frame Time";
        public const string GcAllocationsInFrameCounterName = "GC Allocated In Frame";

        /// <summary>
        /// Converts a 0-based frame index to the 1-based frame number displayed in the Profiler UI.
        /// </summary>
        public static int GetDisplayFrameNumber(int frameIndex)
        {
            return frameIndex + 1;
        }

#if UNITY_2021_1_OR_NEWER
        public static ulong? GetCounterValueAsUInt64(this FrameDataView threadData, string markerName)
        {
            var markerId = threadData.GetMarkerId(markerName);
            if (markerId == FrameDataView.invalidMarkerId)
                return null;

            var value = threadData.GetCounterValueAsLong(markerId);
            // Counters can occasionally report negative timings; clamp to zero.
            if (value < 0)
                value = 0L;

            return Convert.ToUInt64(value);
        }
#endif
    }
}
