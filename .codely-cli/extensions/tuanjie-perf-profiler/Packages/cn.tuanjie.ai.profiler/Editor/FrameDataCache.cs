#if UNITY_2021_1_OR_NEWER
using System;
using System.Collections.Concurrent;
using UnityEditor.Profiling;
using UnityEditorInternal;

namespace Tuanjie.Ai.Profiler
{
    /// <summary>
    /// Caches HierarchyFrameDataView instances keyed by (frameIndex, threadIndex, sortColumn)
    /// so repeated tool calls for the same frame avoid the cost of recreating the view.
    ///
    /// A single shared instance is used per Editor process (no per-conversation isolation),
    /// since the bridge's profiler tools operate on whatever capture is currently loaded
    /// in the Editor.
    /// </summary>
    internal sealed class FrameDataCache : IDisposable
    {
        struct FrameDataDesc : IEquatable<FrameDataDesc>
        {
            public int FrameIndex;
            public int ThreadIndex;
            public int SortColumn;

            public bool Equals(FrameDataDesc other)
            {
                return FrameIndex == other.FrameIndex
                       && ThreadIndex == other.ThreadIndex
                       && SortColumn == other.SortColumn;
            }

            public override bool Equals(object obj) => obj is FrameDataDesc other && Equals(other);

            public override int GetHashCode()
            {
                unchecked
                {
                    var hash = FrameIndex;
                    hash = (hash * 397) ^ ThreadIndex;
                    hash = (hash * 397) ^ SortColumn;
                    return hash;
                }
            }
        }

        readonly ConcurrentDictionary<FrameDataDesc, Lazy<HierarchyFrameDataView>> m_FrameDataCache =
            new ConcurrentDictionary<FrameDataDesc, Lazy<HierarchyFrameDataView>>();
        readonly ConcurrentDictionary<FrameDataDesc, Lazy<HierarchyFrameDataView>> m_InvertedFrameDataCache =
            new ConcurrentDictionary<FrameDataDesc, Lazy<HierarchyFrameDataView>>();

        static void CleanUpCache(ConcurrentDictionary<FrameDataDesc, Lazy<HierarchyFrameDataView>> collection)
        {
            foreach (var kvp in collection)
            {
                if (kvp.Value.IsValueCreated)
                {
                    try { kvp.Value.Value.Dispose(); }
                    catch { /* ignore */ }
                }
            }
            collection.Clear();
        }

        public void Dispose()
        {
            CleanUpCache(m_FrameDataCache);
            CleanUpCache(m_InvertedFrameDataCache);
        }

        public int FirstFrameIndex => ProfilerDriver.firstFrameIndex;
        public int LastFrameIndex => ProfilerDriver.lastFrameIndex;

        public RawFrameDataView GetRawFrameDataView(int frameIndex, int threadIndex)
        {
            return ProfilerDriver.GetRawFrameDataView(frameIndex, threadIndex);
        }

        public HierarchyFrameDataView GetHierarchyFrameDataView(
            int frameIndex,
            int threadIndex,
            HierarchyFrameDataView.ViewModes viewMode,
            int sortColumn,
            bool sortAscending)
        {
            return ProfilerDriver.GetHierarchyFrameDataView(frameIndex, threadIndex, viewMode, sortColumn, sortAscending);
        }

        public HierarchyFrameDataView GetCachedHierarchyFrameDataView(int frameIndex, int threadIndex, int sortColumn)
        {
            var desc = new FrameDataDesc { FrameIndex = frameIndex, ThreadIndex = threadIndex, SortColumn = sortColumn };
            var view = m_FrameDataCache.GetOrAdd(desc, d => new Lazy<HierarchyFrameDataView>(() =>
                ProfilerDriver.GetHierarchyFrameDataView(
                    d.FrameIndex,
                    d.ThreadIndex,
                    HierarchyFrameDataView.ViewModes.MergeSamplesWithTheSameName,
                    d.SortColumn,
                    false)));
            return view.Value;
        }

        public HierarchyFrameDataView GetCachedInvertedHierarchyFrameDataView(int frameIndex, int threadIndex, int sortColumn)
        {
            var desc = new FrameDataDesc { FrameIndex = frameIndex, ThreadIndex = threadIndex, SortColumn = sortColumn };
            var view = m_InvertedFrameDataCache.GetOrAdd(desc, d => new Lazy<HierarchyFrameDataView>(() =>
                ProfilerDriver.GetHierarchyFrameDataView(
                    d.FrameIndex,
                    d.ThreadIndex,
#if UNITY_6000_0_OR_NEWER
                    HierarchyFrameDataView.ViewModes.MergeSamplesWithTheSameName | HierarchyFrameDataView.ViewModes.InvertHierarchy,
#else
                    // ViewModes.InvertHierarchy was added in Unity 6.0; older editors fall back to the non-inverted merged view.
                    HierarchyFrameDataView.ViewModes.MergeSamplesWithTheSameName,
#endif
                    d.SortColumn,
                    false)));
            return view.Value;
        }

        public static int GetThreadIndexByName(int frameIndex, string threadName)
        {
            if (threadName == "Main Thread")
                return FrameDataViewUtils.MainThreadIndex;
            if (threadName == "Render Thread")
                return FrameDataViewUtils.RenderThreadIndex;

            for (var threadIndex = 2; ; threadIndex++)
            {
                using (var rawFrameData = ProfilerDriver.GetRawFrameDataView(frameIndex, threadIndex))
                {
                    if (rawFrameData == null || !rawFrameData.valid)
                        return FrameDataView.invalidThreadIndex;
                    if (rawFrameData.threadName == threadName)
                        return threadIndex;
                }
            }
        }
    }

    /// <summary>
    /// Singleton holder for the active FrameDataCache. The cache is invalidated whenever
    /// a new capture is loaded so cached HierarchyFrameDataView handles do not point at
    /// stale frame data.
    /// </summary>
    [UnityEditor.InitializeOnLoad]
    internal static class FrameDataCacheHolder
    {
        static readonly object s_Lock = new object();
        static FrameDataCache s_Cache;

        static FrameDataCacheHolder()
        {
            UnityEditor.AssemblyReloadEvents.beforeAssemblyReload += Invalidate;
        }

        public static FrameDataCache Instance
        {
            get
            {
                lock (s_Lock)
                {
                    if (s_Cache == null)
                        s_Cache = new FrameDataCache();
                    return s_Cache;
                }
            }
        }

        public static void Invalidate()
        {
            lock (s_Lock)
            {
                if (s_Cache != null)
                {
                    s_Cache.Dispose();
                    s_Cache = null;
                }
            }
        }
    }
}
#endif
