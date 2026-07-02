using System.Collections;
using System.Collections.Generic;
using TangmenFramework;
using UnityEngine;
using UnityEngine.UI;

public class AboutPanel : BasePanel
{
    private string githubLink = "github.com/Yokino337088/IKUN_Game";

    [SerializeField]
    private Image img背景板;
    [SerializeField]
    private Image img打开按钮;
    [SerializeField]
    private Image img前往按钮;

    public override void ShowMe()
    {
        base.ShowMe();
        this.DoPanelSlideInFromTop();
    }

    protected override void Awake()
    {
        base.Awake();
        this.AddAllControlsAnimation();

        

        ABResMgr.Instance.LoadResAsync<Material>(MyAssetBundleName.开始场景材质包, "按钮材质", (mat) =>
        {
            mat.shader = Shader.Find("Custom/ButtonSelectEffect");
            img打开按钮.material = mat;
            img前往按钮.material = mat;
        });

        ABResMgr.Instance.LoadResAsync<Material>(MyAssetBundleName.开始场景材质包, "练习室", (mat) =>
        {
            mat.shader = Shader.Find("Custom/DanceStudioEffect");
            img背景板.material = mat;
        });


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
