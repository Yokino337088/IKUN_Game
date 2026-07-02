using System.Collections;
using System.Collections.Generic;
using TangmenFramework;
using UnityEngine;

public class GameInit : MonoBehaviour
{
    
    private List<string> musicList = new List<string>() { "飞机场", "短裙鸡", "蒸汽鸡", "王妃" };

    void Start()
    {
        
        
        UIMgr.Instance.ShowPanel<BeginPanel>(MyAssetBundleName.开始场景UI面板包);
        // 播放BGM
        // 由于原AssetBundle名乱码，请根据你的实际Bundle名修改
        MusicMgr.Instance.PlayBKMusicList(musicList, MyAssetBundleName.开始场景音乐包);
    }

    


    
}
