using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using Cysharp.Threading.Tasks;

namespace TangmenFramework
{
/// <summary>
/// 场景切换管理器，主要用于统一处理同步和异步场景切换。
/// </summary>
public class SceneMgr : BaseManager<SceneMgr>
{
    private SceneMgr() { }

    /// <summary>
    /// 获取当前激活场景在 Build Settings 中的索引。
    /// </summary>
    public int NowSceneIndex => SceneManager.GetActiveScene().buildIndex;

    /// <summary>
    /// 异步场景开始加载时触发。
    /// </summary>
    public event Action onSceneLoadStart;

    /// <summary>
    /// 异步场景加载完成时触发。
    /// </summary>
    public event Action onSceneLoadComplete;

    /// <summary>
    /// 根据场景名称同步切换场景。
    /// </summary>
    /// <param name="name">需要加载的场景名称。</param>
    /// <param name="callBack">场景切换完成后执行的回调。</param>
    public void LoadScene(string name, Action callBack = null)
    {
        // 同步加载并切换到目标场景。
        SceneManager.LoadScene(name);

        // 场景加载完成后调用回调。
        callBack?.Invoke();
        callBack = null;
    }

    /// <summary>
    /// 根据场景名称异步切换场景。
    /// </summary>
    /// <param name="name">需要加载的场景名称。</param>
    /// <param name="callBack">场景加载完成后执行的回调。</param>
    public async void LoadSceneAsyn(string name, Action callBack = null)
    {
        await ReallyLoadSceneAsyn(name, callBack);
    }

    /// <summary>
    /// 根据场景在 Build Settings 中的索引异步切换场景。
    /// </summary>
    /// <param name="sceneIndex">需要加载的场景索引。</param>
    /// <param name="callBack">场景加载完成后执行的回调。</param>
    public async void LoadSceneAsyn(int sceneIndex, Action callBack = null)
    {
        await ReallyLoadSceneAsyn(sceneIndex, callBack);
    }

    /// <summary>
    /// 执行基于场景名称的异步加载流程。
    /// </summary>
    /// <param name="name">需要加载的场景名称。</param>
    /// <param name="callBack">场景加载完成后执行的回调。</param>
    private async UniTask ReallyLoadSceneAsyn(string name, Action callBack)
    {
        // 切换场景前清空对象池，避免上一个场景的缓存对象残留。
        GOPoolMgr.Instance.ClearPool();

        AsyncOperation ao = SceneManager.LoadSceneAsync(name);
        onSceneLoadStart?.Invoke();

        // 每帧检测异步加载是否完成，并持续向外分发当前加载进度。
        while (!ao.isDone)
        {
            // 将当前进度发送给加载界面等需要显示进度的模块。
            EventCenter.Instance.EventTrigger<float>(E_EventType.E_SceneLoadChange, ao.progress);
            await UniTask.Yield();
        }

        // Unity 的异步进度可能不会自然上报到 1，因此完成时主动补发最终进度。
        EventCenter.Instance.EventTrigger<float>(E_EventType.E_SceneLoadChange, 1);
        onSceneLoadComplete?.Invoke();
        callBack?.Invoke();
        callBack = null;
    }

    /// <summary>
    /// 执行基于场景索引的异步加载流程。
    /// </summary>
    /// <param name="sceneIndex">需要加载的场景索引。</param>
    /// <param name="callBack">场景加载完成后执行的回调。</param>
    private async UniTask ReallyLoadSceneAsyn(int sceneIndex, Action callBack)
    {
        // 切换场景前清空对象池，避免上一个场景的缓存对象残留。
        GOPoolMgr.Instance.ClearPool();

        AsyncOperation ao = SceneManager.LoadSceneAsync(sceneIndex);
        onSceneLoadStart?.Invoke();

        // 每帧检测异步加载是否完成，并持续向外分发当前加载进度。
        while (!ao.isDone)
        {
            // 将当前进度发送给加载界面等需要显示进度的模块。
            EventCenter.Instance.EventTrigger<float>(E_EventType.E_SceneLoadChange, ao.progress);
            await UniTask.Yield();
        }

        // Unity 的异步进度可能不会自然上报到 1，因此完成时主动补发最终进度。
        EventCenter.Instance.EventTrigger<float>(E_EventType.E_SceneLoadChange, 1);
        onSceneLoadComplete?.Invoke();
        callBack?.Invoke();
        callBack = null;
    }
}
}
