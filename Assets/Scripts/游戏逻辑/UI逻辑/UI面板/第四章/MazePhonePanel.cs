using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TangmenFramework;

//安卓手机所对应的迷宫关卡UI面板
public class MazePhonePanel:BasePanel
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
            case "btn上":
                EventCenter.Instance.EventTrigger(E_EventType.E_Input_Up);
                break;
            case "btn下":
                EventCenter.Instance.EventTrigger(E_EventType.E_Input_Down);
                break;
            case "btn左":
                EventCenter.Instance.EventTrigger(E_EventType.E_Input_Left);
                break;
            case "btn右":
                EventCenter.Instance.EventTrigger(E_EventType.E_Input_Right);
                break;
            case "btn寻路":
                EventCenter.Instance.EventTrigger(MyEventTypeString.AndroidPlayerFindPath);
                break;
        }
    }
}