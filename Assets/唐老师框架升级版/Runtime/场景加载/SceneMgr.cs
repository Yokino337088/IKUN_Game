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
/// �����л������� ��Ҫ�����л�����
/// </summary>
public class SceneMgr : BaseManager<SceneMgr>
{
    private SceneMgr() { }

    /// <summary>
    /// ��ǰ��������
    /// </summary>
    public int NowSceneIndex => SceneManager.GetActiveScene().buildIndex;

    public event Action onSceneLoadStart;
    public event Action onSceneLoadComplete;

    //ͬ���л������ķ���
    public void LoadScene(string name, Action callBack = null)
    {
        //�л�����
        SceneManager.LoadScene(name);
        //���ûص�
        callBack?.Invoke();
        callBack = null;
    }

    //�첽�л������ķ���
    public async void LoadSceneAsyn(string name, Action callBack = null)
    {
        await ReallyLoadSceneAsyn(name, callBack);
    }

    public async void LoadSceneAsyn(int sceneIndex, Action callBack = null)
    {
        await ReallyLoadSceneAsyn(sceneIndex, callBack);
    }


    private async UniTask ReallyLoadSceneAsyn(string name, Action callBack)
    {
        //����������
        GOPoolMgr.Instance.ClearPool();

        AsyncOperation ao = SceneManager.LoadSceneAsync(name);
        onSceneLoadStart?.Invoke();
        //��ͣ�����첽������ÿ֡����Ƿ���ؽ��� ������ؽ����Ͳ�������ѭ��ÿִ֡����
        while (!ao.isDone)
        {
            //���������������¼����� ÿһ֡�����ȷ��͸���Ҫ�õ��ĵط�
            EventCenter.Instance.EventTrigger<float>(E_EventType.E_SceneLoadChange, ao.progress);
            await UniTask.Yield();
        }
        //�������һֱ֡�ӽ����� û��ͬ��1��ȥ
        EventCenter.Instance.EventTrigger<float>(E_EventType.E_SceneLoadChange, 1);
        onSceneLoadComplete?.Invoke();
        callBack?.Invoke();
        callBack = null;
    }

    private async UniTask ReallyLoadSceneAsyn(int sceneIndex, Action callBack)
    {
        //����������
        GOPoolMgr.Instance.ClearPool();

        AsyncOperation ao = SceneManager.LoadSceneAsync(sceneIndex);
        onSceneLoadStart?.Invoke();
        //��ͣ�����첽������ÿ֡����Ƿ���ؽ��� ������ؽ����Ͳ�������ѭ��ÿִ֡����
        while (!ao.isDone)
        {
            //���������������¼����� ÿһ֡�����ȷ��͸���Ҫ�õ��ĵط�
            EventCenter.Instance.EventTrigger<float>(E_EventType.E_SceneLoadChange, ao.progress);
            await UniTask.Yield();
        }
        //�������һֱ֡�ӽ����� û��ͬ��1��ȥ
        EventCenter.Instance.EventTrigger<float>(E_EventType.E_SceneLoadChange, 1);
        onSceneLoadComplete?.Invoke();
        callBack?.Invoke();
        callBack = null;
    }

    
}
}
