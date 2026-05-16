using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TangmenFramework;

public class MazePCPanel:BasePanel
{
    public override void ShowMe()
    {
        base.ShowMe();
        this.DoPanelFadeInAnimation();
    }

    protected override void ClickBtn(string btnName)
    {
        base.ClickBtn(btnName);

        switch (btnName)
        {
            case "btn寻路":
                EventCenter.Instance.EventTrigger(MyEventTypeString.AndroidPlayerFindPath);
                break;
            case "btn提示":
                UIMgr.Instance.ShowPanel<MazeTipsPanel>(MyAssetBundleName.第四章UI面板包);
                break;
        }
    }
}