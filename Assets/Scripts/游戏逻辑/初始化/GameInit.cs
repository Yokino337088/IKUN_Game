using System.Collections;
using System.Collections.Generic;
using TangmenFramework;
using UnityEngine;

public class GameInit : MonoBehaviour
{
    // 假设原来的中文歌名是：晴天、稻香、七里香、花海
    // 由于原文件乱码，这里用英文代替，请你根据实际需要改成正确的中文
    private List<string> musicList = new List<string>() { "飞机场", "短裙鸡", "蒸汽鸡", "王妃" };

    void Start()
    {
        LoadJsonData();
        UIMgr.Instance.ShowPanel<BeginPanel>();
        // 播放BGM
        // 由于原AssetBundle名乱码，请根据你的实际Bundle名修改
        MusicMgr.Instance.PlayBKMusicList(musicList, MyAssetBundleName.开始场景音乐包);
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
