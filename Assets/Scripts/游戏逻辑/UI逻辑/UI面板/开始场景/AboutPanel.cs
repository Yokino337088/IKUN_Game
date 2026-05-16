using System.Collections;
using System.Collections.Generic;
using TangmenFramework;
using UnityEngine;

public class AboutPanel : BasePanel
{
    private string githubLink = "github.com/Yokino337088/IKUN_Game";

    public override void ShowMe()
    {
        base.ShowMe();
        this.DoPanelSlideInFromTop();
    }

    protected override void Awake()
    {
        base.Awake();
        this.AddAllControlsAnimation();
    }

    protected override void ClickBtn(string btnName)
    {
        base.ClickBtn(btnName);

        switch (btnName)
        {
            case "btn复制":
                GUIUtility.systemCopyBuffer = githubLink;
                LogSystem.Info("已复制");
                break;
            case "btn打开":
                Application.OpenURL("https://" + githubLink);
                break;
            case "btn返回":
                UIMgr.Instance.HidePanelWithAnimation<AboutPanel>(E_HideType.底部滑出, () =>
                {
                    UIMgr.Instance.ShowPanel<BeginPanel>();
                });            
                break;
        }
    }
}