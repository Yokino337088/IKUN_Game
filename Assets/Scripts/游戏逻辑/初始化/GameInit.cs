using System.Collections;
using System.Collections.Generic;
using TangmenFramework;
using UnityEngine;

public class GameInit : MonoBehaviour
{
    
    private List<string> musicList = new List<string>() { "飞机场", "短裙鸡", "蒸汽鸡", "王妃" };

    void Start()
    {
        // 初始化场景AB包卸载管理器：注册各场景与AB包的映射关系
        InitSceneABMappings();
        
        UIMgr.Instance.ShowPanel<BeginPanel>(MyAssetBundleName.开始场景UI面板包);
        // 播放BGM
        // 由于原AssetBundle名乱码，请根据你的实际Bundle名修改
        MusicMgr.Instance.PlayBKMusicList(musicList, MyAssetBundleName.开始场景音乐包);
    }

    /// <summary>
    /// 向 SceneABUnloadManager 注册每个场景所使用的AB包。
    /// 之后切场景时，管理器会自动释放旧场景独享的AB包以优化内存。
    /// </summary>
    private void InitSceneABMappings()
    {
        var mgr = SceneABUnloadManager.Instance;

        // ---- 注册共享/常驻AB包（切场景时不会被卸载） ----
        mgr.RegisterSharedABs(new List<string>
        {
            MyAssetBundleName.过场景UI面板包,
            MyAssetBundleName.通用音效包
        });

        // ---- 注册各场景独享的AB包 ----
        mgr.RegisterSceneABs("开始场景", new List<string>
        {
            MyAssetBundleName.开始场景音乐包,
            MyAssetBundleName.开始场景UI面板包,
            MyAssetBundleName.开始场景材质包
        });

        mgr.RegisterSceneABs("第一章", new List<string>
        {
            MyAssetBundleName.第一章UI面板包,
            MyAssetBundleName.第一章音乐包
        });

        mgr.RegisterSceneABs("第四章_迷宫", new List<string>
        {
            MyAssetBundleName.第四章物体包,
            MyAssetBundleName.第四章音乐包,
            MyAssetBundleName.第四章音效包,
            MyAssetBundleName.第四章UI面板包,
            MyAssetBundleName.第四章json数据包
        });

        mgr.RegisterSceneABs("第四章_boss战", new List<string>
        {
            MyAssetBundleName.第四章物体包,
            MyAssetBundleName.第四章音乐包,
            MyAssetBundleName.第四章音效包,
            MyAssetBundleName.第四章UI面板包,
            MyAssetBundleName.第四章json数据包
        });

        mgr.RegisterSceneABs("第四章剧情演出", new List<string>
        {
            MyAssetBundleName.第四章音乐包,
            MyAssetBundleName.第四章UI面板包
        });
    }
}
