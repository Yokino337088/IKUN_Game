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
    private Dropdown myDropdown;

    [SerializeField]
    private Image img开始游戏按钮;

    [SerializeField]
    private Image img代码开源按钮;

    [SerializeField]
    private Image img退出游戏按钮;

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
        this.AddAllControlsAnimation();
        myDropdown = GetControl<Dropdown>("drop音乐选择");
        
        //安卓端的AB包加载有点问题，如果直接显示面板的话，那么按钮的材质就会丢失，所以必须先把材质从AB包当中加载出来
        //然后再把材质的shader给设置好，这样才能正常显示
        ABResMgr.Instance.LoadResAsync<Material>(MyAssetBundleName.开始场景材质包, "按钮材质", (mat) =>
        {
            mat.shader = Shader.Find("Custom/ButtonSelectEffect");
            img代码开源按钮.material = mat;
            img开始游戏按钮.material = mat;
            img退出游戏按钮.material = mat;
        });
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
                UIMgr.Instance.HidePanelWithAnimation<BeginPanel>(E_HideType.淡出, () =>
                {
                    UIMgr.Instance.ShowPanel<SelectLevelPanel>(MyAssetBundleName.开始场景UI面板包);
                }, 0.3f);
                break;
            case "btn代码开源":
                UIMgr.Instance.HidePanelWithAnimation<BeginPanel>(E_HideType.淡出, () =>
                {
                    UIMgr.Instance.ShowPanel<AboutPanel>(MyAssetBundleName.开始场景UI面板包);
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
