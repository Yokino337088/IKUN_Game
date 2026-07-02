using System.Collections;
using System.Collections.Generic;
using TangmenFramework;
using UnityEngine;

public class BosslevelInit : MonoBehaviour
{
    
    void Start()
    {
        MusicMgr.Instance.ChangeBKMusicValue(0.2f);
        MusicMgr.Instance.ChangeSoundValue(0.2f);
        UIMgr.Instance.ShowPanel<FourBossPanel>(MyAssetBundleName.第四章UI面板包);

        if(PlatformHelper.IsPC)
            UIMgr.Instance.ShowPanel<FourBossPCPanel>(MyAssetBundleName.第四章UI面板包, null, E_UILayer.Top);
        else
            UIMgr.Instance.ShowPanel<FourBossPhonePanel>(MyAssetBundleName.第四章UI面板包, null, E_UILayer.Top);

        MusicMgr.Instance.PlayBKMusic(MyAssetBundleName.第四章音乐包, "找自己");

        
    }

    
}
