using System.Collections.Generic;
using UnityEngine.SceneManagement;

namespace TangmenFramework
{

    /// <summary>
    /// 场景AB包卸载管理器 —— 通用、可复用的旧场景AB包内存释放工具。
    ///
    /// 使用方式：
    /// 1. 在游戏初始化时调用 RegisterSceneABs 注册每个场景用到的AB包。
    /// 2. 调用 RegisterSharedABs 注册常驻/跨场景共享的AB包（这些不会被卸载）。
    /// 3. 之后每次场景切换时，管理器会自动释放旧场景独享的AB包。
    /// 4. 也可以随时调用 UnloadSceneABs 手动卸载指定场景的AB包。
    /// </summary>
    public class SceneABUnloadManager : BaseManager<SceneABUnloadManager>
    {
        /// <summary>场景名 → 该场景独享的AB包名列表。</summary>
        private readonly Dictionary<string, List<string>> sceneABMap = new Dictionary<string, List<string>>();

        /// <summary>跨场景共享、不应在切场景时卸载的AB包名集合。</summary>
        private readonly HashSet<string> sharedABs = new HashSet<string>();

        /// <summary>是否已在构造函数中订阅了场景加载事件。</summary>
        private bool isEventListenerRegistered;

        private SceneABUnloadManager()
        {
            RegisterSceneLoadListener();
        }

        /// <summary>
        /// 注册一个场景所使用的AB包列表。
        /// 可多次调用同一场景名来追加AB包。
        /// </summary>
        /// <param name="sceneName">场景名（与 Build Settings 中的名称一致）。</param>
        /// <param name="abNames">该场景独享的AB包名列表。</param>
        public void RegisterSceneABs(string sceneName, List<string> abNames)
        {
            if (string.IsNullOrEmpty(sceneName) || abNames == null || abNames.Count == 0)
                return;

            if (!sceneABMap.ContainsKey(sceneName))
                sceneABMap[sceneName] = new List<string>();

            foreach (string abName in abNames)
            {
                if (!string.IsNullOrEmpty(abName) && !sceneABMap[sceneName].Contains(abName))
                    sceneABMap[sceneName].Add(abName);
            }
        }

        /// <summary>
        /// 注册跨场景共享的AB包。这些包在切场景时不会被释放。
        /// </summary>
        /// <param name="abNames">常驻AB包名列表。</param>
        public void RegisterSharedABs(List<string> abNames)
        {
            if (abNames == null)
                return;

            foreach (string abName in abNames)
            {
                if (!string.IsNullOrEmpty(abName))
                    sharedABs.Add(abName);
            }
        }

        /// <summary>
        /// 手动卸载指定场景的所有独享AB包（排除共享包）。
        /// 适用于需要在特定时机精确控制内存的场景。
        /// </summary>
        /// <param name="sceneName">要卸载的场景名。</param>
        public void UnloadSceneABs(string sceneName)
        {
            if (string.IsNullOrEmpty(sceneName) || !sceneABMap.ContainsKey(sceneName))
                return;

            List<string> abNames = sceneABMap[sceneName];
            for (int i = abNames.Count - 1; i >= 0; i--)
            {
                string abName = abNames[i];
                if (sharedABs.Contains(abName))
                    continue;

                ABResMgr.Instance.ReleaseResIfExists(abName);
            }

            // 触发一次全局无用AB清理，确保所有引用计数归零的包都被真正卸载。
            ABResMgr.Instance.TriggerUnloadCheck();
        }

        /// <summary>
        /// 手动卸载除指定场景之外的所有已注册场景的AB包。
        /// 适用于加载新场景前集中释放旧场景资源的场景。
        /// </summary>
        /// <param name="keepSceneName">需要保留AB包的目标场景名。</param>
        public void UnloadAllExceptScene(string keepSceneName)
        {
            foreach (string sceneName in sceneABMap.Keys)
            {
                if (sceneName == keepSceneName)
                    continue;

                UnloadSceneABs(sceneName);
            }
        }

        /// <summary>订阅场景加载事件，在切场景时自动释放旧场景资源。</summary>
        private void RegisterSceneLoadListener()
        {
            if (isEventListenerRegistered)
                return;

            SceneMgr.Instance.onSceneLoadStart += OnSceneLoadStart;
            isEventListenerRegistered = true;
        }

        /// <summary>场景开始加载时的回调：释放当前激活场景的AB包。</summary>
        private void OnSceneLoadStart()
        {
            string oldSceneName = SceneManager.GetActiveScene().name;

            if (string.IsNullOrEmpty(oldSceneName))
                return;

            LogSystem.Info($"SceneABUnloadManager: 即将离开场景 [{oldSceneName}]，开始释放其AB包");
            UnloadSceneABs(oldSceneName);
        }

        public override void Dispose()
        {
            if (isEventListenerRegistered)
            {
                SceneMgr.Instance.onSceneLoadStart -= OnSceneLoadStart;
                isEventListenerRegistered = false;
            }

            sceneABMap.Clear();
            sharedABs.Clear();
            base.Dispose();
        }
    }
}
