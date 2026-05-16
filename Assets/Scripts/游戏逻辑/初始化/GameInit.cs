using System.Collections;
using System.Collections.Generic;
using TangmenFramework;
using UnityEngine;

public class GameInit : MonoBehaviour
{
    private List<string> musicList = new List<string>() { "王妃", "短裙鸡", "蒸汽鸡", "飞机场" };

    void Start()
    {
        LoadJsonData();
        UIMgr.Instance.ShowPanel<BeginPanel>();
        //播放BGM
        MusicMgr.Instance.PlayBKMusicList(musicList,MyAssetBundleName.开始场景音乐包);

    }


    private void LoadJsonData()
    {
        JsonDataMgr.Instance.LoadTableFromAB<T_BulletCommentsContainer, T_BulletComments>();

        foreach (var info in JsonDataMgr.Instance.GetTable<T_BulletCommentsContainer>().dataDic.Values)
        {
            LogSystem.Info(info.textInfo);
        }
    }
}
