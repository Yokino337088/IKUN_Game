using TangmenFramework;
using UnityEngine;

/// <summary>
/// 第一章节奏接箱场景入口。
/// 只负责初始化框架输入和按平台显示 UI，具体玩法由 RhythmCatchGameManager 自行启动。
/// </summary>
public class ChapterOneLevelBootstrap : MonoBehaviour
{


    private void Start()
    {
        EventCenter.Instance.AddEventListener(MyEventTypeString.StoryEndAndLevelInit, InitLevel);

        //显示UI面板
        UIMgr.Instance.ShowPanel<StoryPanel>(MyAssetBundleName.过场景UI面板包, (panel) =>
        {
            panel.StartStoryDialog(1, 1);
        });

        
    }

    private void InitLevel()
    {
        // 主动初始化框架 InputMgr，PC 端的 Horizontal 轴会把 A/D 和方向键转成事件。
        InputMgr.Instance.SetEnabled(true);

        // HUD 在所有平台显示，并由 UIMgr 挂到持久化 Canvas 的 Middle 层。
        UIMgr.Instance.ShowPanel<RhythmCatchHUDPanel>(MyAssetBundleName.第一章UI面板包,null,E_UILayer.Middle);

        // PlatformHelper 统一判断运行平台：移动端额外显示长按左右按钮，PC 不创建操作面板。
        if (PlatformHelper.IsMobile)
        {
            UIMgr.Instance.ShowPanel<RhythmCatchMobilePanel>(MyAssetBundleName.第一章UI面板包,null,E_UILayer.Top);
        }
        else
        {
            UIMgr.Instance.HidePanel<RhythmCatchMobilePanel>(true);
        }

        // 剧情对话结束，正式启动节奏接箱玩法（GameManager 的 autoStart 已设为 false）。
        RhythmCatchGameManager.Instance?.StartLevel();
    }

    private void OnDestroy()
    {
        // 关卡退出时销毁本章面板，避免 UIMgr 的持久化 Canvas 在其他场景继续显示。
        UIMgr.Instance.HidePanel<RhythmCatchHUDPanel>(true);
        UIMgr.Instance.HidePanel<RhythmCatchMobilePanel>(true);

        EventCenter.Instance.RemoveEventListener(MyEventTypeString.StoryEndAndLevelInit, InitLevel);
    }
}
