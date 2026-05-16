using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TangmenFramework;

public class MazeTipsPanel:BasePanel
{
    public override void ShowMe()
    {
        base.ShowMe();
        this.DoPanelScaleInAnimation();
    }

    protected override void ClickBtn(string btnName)
    {
        base.ClickBtn(btnName);
        switch (btnName)
        {
            case "btn关闭":
                UIMgr.Instance.HidePanelWithAnimation<MazeTipsPanel>(E_HideType.缩放退出);
                break;
        }
    }
}