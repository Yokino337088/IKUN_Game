using DG.Tweening;
using System;
using System.Collections.Generic;
using System.Text;
using TangmenFramework;
using UnityEngine;
using UnityEngine.UI;

public class FourBossPhonePanel : BasePanel
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

        // 注册移动按钮的按下/松开事件
        RegisterMoveButtonEvents();
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

        // 注销移动按钮事件
        UnregisterMoveButtonEvents();
    }

    private LongPressButton _leftMoveBtn;
    private LongPressButton _rightMoveBtn;

    /// <summary>
    /// 注册左移/右移按钮的按下和松开事件
    /// </summary>
    private void RegisterMoveButtonEvents()
    {
        _leftMoveBtn = GetControl<LongPressButton>("左移");
        _rightMoveBtn = GetControl<LongPressButton>("右移");

        if (_leftMoveBtn != null)
        {
            _leftMoveBtn.onPointerDown.AddListener(OnLeftMovePointerDown);
            _leftMoveBtn.onPointerUp.AddListener(OnLeftMovePointerUp);
        }
        if (_rightMoveBtn != null)
        {
            _rightMoveBtn.onPointerDown.AddListener(OnRightMovePointerDown);
            _rightMoveBtn.onPointerUp.AddListener(OnRightMovePointerUp);
        }
    }

    /// <summary>
    /// 注销左移/右移按钮的按下和松开事件
    /// </summary>
    private void UnregisterMoveButtonEvents()
    {
        if (_leftMoveBtn != null)
        {
            _leftMoveBtn.onPointerDown.RemoveListener(OnLeftMovePointerDown);
            _leftMoveBtn.onPointerUp.RemoveListener(OnLeftMovePointerUp);
        }
        if (_rightMoveBtn != null)
        {
            _rightMoveBtn.onPointerDown.RemoveListener(OnRightMovePointerDown);
            _rightMoveBtn.onPointerUp.RemoveListener(OnRightMovePointerUp);
        }
    }

    /// 左移按钮按下时发送水平输入-1，松开时如果右移按钮未按下则发送0停止移动
    private void OnLeftMovePointerDown()
    {
        EventCenter.Instance.EventTrigger<float>(E_EventType.E_Input_Horizontal, -1f);
    }

    /// 右移按钮按下时发送水平输入1，松开时如果左移按钮未按下则发送0停止移动
    private void OnLeftMovePointerUp()
    {
        // 如果右移按钮没有按下，才发送0停止移动
        if (_rightMoveBtn == null || !_rightMoveBtn.IsPointerDown)
            EventCenter.Instance.EventTrigger<float>(E_EventType.E_Input_Horizontal, 0f);
    }

    /// 右移按钮按下时发送水平输入1，松开时如果左移按钮未按下则发送0停止移动
    private void OnRightMovePointerDown()
    {
        EventCenter.Instance.EventTrigger<float>(E_EventType.E_Input_Horizontal, 1f);
    }

    /// 右移按钮松开时，如果左移按钮未按下则发送0停止移动
    private void OnRightMovePointerUp()
    {
        // 如果左移按钮没有按下，才发送0停止移动
        if (_leftMoveBtn == null || !_leftMoveBtn.IsPointerDown)
            EventCenter.Instance.EventTrigger<float>(E_EventType.E_Input_Horizontal, 0f);
    }


    

    protected override void ClickBtn(string btnName)
    {
        base.ClickBtn(btnName);
        switch (btnName)
        {
            case "普攻":
                EventCenter.Instance.EventTrigger(E_EventType.E_Input_Attack);
                break;
            case "技能":
                EventCenter.Instance.EventTrigger(E_EventType.E_Input_Skill);
                break;
            case "大招":
                EventCenter.Instance.EventTrigger(E_EventType.E_Input_Ultimate);
                break;
            case "跳跃":
                EventCenter.Instance.EventTrigger(E_EventType.E_Input_Jump);
                break;
        }
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
