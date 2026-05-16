using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using TangmenFramework;

//迷宫关卡初始化
public class MazelevelInit:MonoBehaviour
{
    private void Start()
    {
        //注册事件
        EventCenter.Instance.AddEventListener(MyEventTypeString.StoryEndAndLevelInit, IevelInit);

        //显示UI面板
        UIMgr.Instance.ShowPanel<StoryPanel>(MyAssetBundleName.过场景UI面板包, (panel) =>
        {
            panel.StartStoryDialog(4, 1);
        });
    }

    private void IevelInit()
    {
        if (PlatformHelper.IsMobile)
        {
            UIMgr.Instance.ShowPanel<MazePhonePanel>(MyAssetBundleName.第四章UI面板包);
        }
        else
        {
            UIMgr.Instance.ShowPanel<MazePCPanel>(MyAssetBundleName.第四章UI面板包);
        }
        //播放音乐
        MusicMgr.Instance.ChangeBKMusicValue(0.25f);
        MusicMgr.Instance.PlayBKMusic(MyAssetBundleName.第四章音乐包, "因为你");

        //注销事件,这里用完了就马上注销，防止占用资源
        EventCenter.Instance.RemoveEventListener(MyEventTypeString.StoryEndAndLevelInit, IevelInit);
    }


}