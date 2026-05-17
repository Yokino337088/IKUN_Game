using System;
using System.Collections.Generic;
using System.Text;
using DG.Tweening;
using TangmenFramework;
using UnityEngine;
using UnityEngine.UI;

public class FourBossPCPanel : BasePanel
{
    [SerializeField]
    private Image skillImage;

    [SerializeField]
    private Image ultimateImage;

    private Tweener _skillCooldownTween;
    private Tweener _ultimateCooldownTween;

    public override void ShowMe()
    {
        base.ShowMe();
        this.DoPanelFadeInAnimation();
        // 初始状态：无冷却
        if (skillImage != null)
        {
            skillImage.fillAmount = 0;
            skillImage.enabled = false;
        }
        if (ultimateImage != null)
        {
            ultimateImage.fillAmount = 0;
            ultimateImage.enabled = false;
        }

        // 注册冷却事件监听
        EventCenter.Instance.AddEventListener(MyEventTypeString.玩家技能开始冷却事件, OnSkillCooldownStart);
        EventCenter.Instance.AddEventListener(MyEventTypeString.玩家大招开始冷却事件, OnUltimateCooldownStart);
    }

    public override void HideMe()
    {
        base.HideMe();
        // 注销冷却事件监听
        EventCenter.Instance.RemoveEventListener(MyEventTypeString.玩家技能开始冷却事件, OnSkillCooldownStart);
        EventCenter.Instance.RemoveEventListener(MyEventTypeString.玩家大招开始冷却事件, OnUltimateCooldownStart);

        // 清理DOTween动画
        _skillCooldownTween?.Kill();
        _ultimateCooldownTween?.Kill();
    }

    /// <summary>
    /// 技能开始冷却回调：fillAmount从1渐变到0
    /// </summary>
    private void OnSkillCooldownStart()
    {
        if (skillImage == null)
            return;

        // 杀掉上一次的冷却动画（防止重复触发时动画叠加）
        _skillCooldownTween?.Kill();

        BossPlayerControl playerControl = BossLevelMgr.Instance?.PlayerControl;
        if (playerControl == null)
            return;

        skillImage.enabled = true;
        skillImage.fillAmount = 1f;

        // fillAmount从1渐变到0，时长为技能冷却时间
        _skillCooldownTween = skillImage.DOFillAmount(0f, playerControl.skillCooldown)
            .SetEase(Ease.Linear)
            .OnComplete(() =>
            {
                skillImage.enabled = false;
            });
    }

    /// <summary>
    /// 大招开始冷却回调：fillAmount从1渐变到0
    /// </summary>
    private void OnUltimateCooldownStart()
    {
        if (ultimateImage == null)
            return;

        _ultimateCooldownTween?.Kill();

        BossPlayerControl playerControl = BossLevelMgr.Instance?.PlayerControl;
        if (playerControl == null)
            return;

        ultimateImage.enabled = true;
        ultimateImage.fillAmount = 1f;

        // fillAmount从1渐变到0，时长为大招冷却时间
        _ultimateCooldownTween = ultimateImage.DOFillAmount(0f, playerControl.ultimateCooldown)
            .SetEase(Ease.Linear)
            .OnComplete(() =>
            {
                ultimateImage.enabled = false;
            });
    }
}
