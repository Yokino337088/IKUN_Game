using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TangmenFramework;
using UnityEngine.UI;
using DG.Tweening;

public class StoryPanel : BasePanel
{
    //剧情播放器的引用
    
    public StoryPlayer storyPlayer;

    //显示剧情的text组件

    public Text txtStory;

    //打一个字的时间

    public float typeSpeed = 0.1f;

    public override void ShowMe()
    {
        base.ShowMe();
        this.DoPanelScaleInAnimation();
        //注册事件
        storyPlayer.OnStoryStarted += OnStoryStarted;
        storyPlayer.OnDialogueUpdated += OnDialogueUpdated;
        storyPlayer.OnStoryEnded += OnStoryEnded;
    }

    public override void HideMe()
    {
        base.HideMe();
        //注销事件
        storyPlayer.OnStoryStarted -= OnStoryStarted;
        storyPlayer.OnDialogueUpdated -= OnDialogueUpdated;
        storyPlayer.OnStoryEnded -= OnStoryEnded;
    }

    private void Update()
    {
        //检测玩家是否按下了任意键或者点击了屏幕
        if (Input.anyKeyDown || (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began))
        {
            //显示下一段对话
            storyPlayer.NextDialogue();
        }
    }

    private void OnStoryStarted(StoryLineData storyLineData)
    {

    }

    private void OnDialogueUpdated(int index,string speakerName,string content)
    {
        //先清空字符
        txtStory.text = null;
        //dotween打字机效果
        txtStory.DOText(content, content.Length * typeSpeed);
        //播放音效
        MusicMgr.Instance.PlaySoundSafe(MyAssetBundleName.第四章音效包, "打字机", content.Length * typeSpeed);
    }

    private void OnStoryEnded()
    {
        //隐藏该面板
        UIMgr.Instance.HidePanelWithAnimation<StoryPanel>(E_HideType.缩放退出, () =>
        {
            //这里去触发关卡初始化的事件，实现解耦
            EventCenter.Instance.EventTrigger(MyEventTypeString.StoryEndAndLevelInit);
        });
    }

    /// <summary>
    /// 提供给外部进行调用的开始对话方法
    /// </summary>
    /// <param name="ChapterId">章节号</param>
    /// <param name="ModuleId">剧情号</param>
    public void StartStoryDialog(int ChapterId,int ModuleId)
    {
        storyPlayer.PlayByLookup(ChapterId, ModuleId);
    }
}
