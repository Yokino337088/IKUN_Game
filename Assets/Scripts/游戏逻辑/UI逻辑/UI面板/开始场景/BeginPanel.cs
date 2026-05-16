using System.Collections;
using System.Collections.Generic;
using TangmenFramework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BeginPanel : BasePanel
{
    /// <summary>
    /// 下拉列表
    /// </summary>
    private TMP_Dropdown myDropdown;

    //音乐字典(避免switch-case语句)
    private Dictionary<int, string> musicDic = new Dictionary<int, string>()
    {
        { 0,"王妃"},{1,"短裙鸡" },{2,"蒸汽鸡" },{3,"飞机场" }
    };

    /// <summary>
    /// 是否通过代码设置dropdown的选项
    /// </summary>
    private bool isSettingByCode = false;

    protected override void Awake()
    {
        base.Awake();
        myDropdown = GetControl<TMP_Dropdown>("drop音乐选择");
        this.AddAllControlsAnimation();
    }

    public override void ShowMe()
    {
        base.ShowMe();
        //添加面板淡入动画
        this.DoPanelFadeInAnimation(0.2f);
        //注册事件
        MusicMgr.Instance.OnMusicPlaybackCompleted += OnMusicPlaybackOnCompleted;
    }

    public override void HideMe()
    {
        base.HideMe();
        //注销事件(注册和注销必须配对，防止内存泄漏)
        MusicMgr.Instance.OnMusicPlaybackCompleted -= OnMusicPlaybackOnCompleted;
    }

    /// <summary>
    /// 监听按钮事件
    /// </summary>
    /// <param name="btnName"></param>
    protected override void ClickBtn(string btnName)
    {
        base.ClickBtn(btnName);

        switch (btnName)
        {
            case "btn开始游戏":

                break;
            case "btn代码开源":
                UIMgr.Instance.HidePanelWithAnimation<BeginPanel>(E_HideType.淡出, () =>
                {
                    UIMgr.Instance.ShowPanel<AboutPanel>();
                },0.3f);
                break;
            case "btn退出游戏":
                Application.Quit();
                break;
        }
    }

    /// <summary>
    /// 监听下拉列表事件
    /// </summary>
    /// <param name="dropDownName"></param>
    /// <param name="index"></param>
    protected override void DropDownSelectChange(string dropDownName, int index)
    {
        base.DropDownSelectChange(dropDownName, index);
        //如果是通过事件触发的话，那么直接跳过·
        if (isSettingByCode)
            return;

        switch (dropDownName)
        {
            case "drop音乐选择": 
                OnDropDownSelect(index);
                break;
        }
    }

    /// <summary>
    /// 根据索引切换音乐
    /// </summary>
    /// <param name="index"></param>
    private void OnDropDownSelect(int index)
    {
        //这里如果不用字典存索引对应的音乐名字的话那么就要用switch-case语句写4次
        MusicMgr.Instance.PlayBKMusicListFromSong(musicDic[index]);
    }

    /// <summary>
    /// 音乐播放完成时触发的事件函数(这就是观察者模式)
    /// </summary>
    /// <param name="index"></param>
    private void OnMusicPlaybackOnCompleted(int index)
    {
        isSettingByCode = true;

        myDropdown.value = index;
        myDropdown.RefreshShownValue();

        isSettingByCode = false;
    }
}
