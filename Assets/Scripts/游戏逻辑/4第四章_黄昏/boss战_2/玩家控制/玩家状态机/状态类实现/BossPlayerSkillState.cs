﻿﻿﻿using TangmenFramework;
using UnityEngine;

/// <summary>
/// 玩家技能状态。
/// 
/// 进入该状态后播放技能动画，并注册一个 Animancer 动画事件回调（在动画 92% 处触发），
/// 回调中发射导弹并切换到待机状态。
/// 
/// 防重入设计与 BossPlayerAttackState 完全一致：
/// - 不注册任何输入事件监听，技能播放期间不可被输入打断
/// - _isCompleted 标志确保 SkillComplete 回调只执行一次
/// - QuitState 中清除动画事件回调，防止幽灵回调
/// 
/// 相关 Bug 记录：Bug #001（攻击动画卡死_同步事件级联导致状态机重入）
/// </summary>
public class BossPlayerSkillState : BossPlayerState
{
    public override E_BossPlayerStateType StateType => E_BossPlayerStateType.技能;

    /// <summary>
    /// 防重入标志。为 true 时表示技能已经完成，后续的 SkillComplete 回调将被忽略。
    /// </summary>
    private bool _isCompleted;

    public BossPlayerSkillState(StateMachine<E_BossPlayerStateType, IBossPlayerFSMObj> machine) : base(machine) { }

    public override void EnterState()
    {
        base.EnterState();
        _isCompleted = false;
        AIObj.StartSkillCooldown();
        EventCenter.Instance.EventTrigger(MyEventTypeString.玩家技能开始冷却事件);
        PlayAnimation(E_BossPlayerStateType.技能);
        MusicMgr.Instance.PlaySoundSafe(MyAssetBundleName.通用音效包, "鸡你太美");
        AddAnimationEvent(0.92f, SkillComplete);
        LogSystem.Info("玩家进入技能状态");
    }

    /// <summary>
    /// 退出状态时：设置防重入标志 + 清除动画事件回调 + 注销输入事件监听。
    /// </summary>
    public override void QuitState()
    {
        _isCompleted = true;
        ClearAnimationEvents();
        base.QuitState();
    }

    public override void UpdateState()
    {
        base.UpdateState();
        
        // 备用方案：如果动画事件没有触发，使用Update检测动画进度
        if (!_isCompleted && _currentAnimState != null)
        {
            float normalizedTime = _currentAnimState.NormalizedTime % 1;
            if (normalizedTime >= 0.92f && normalizedTime < 0.97f)
            {
                SkillComplete();
            }
        }
    }

    /// <summary>
    /// 技能动画完成回调（由 Animancer 在动画播放到 92% 时触发）。
    /// 先设 _isCompleted = true（防止重入），再发射导弹，最后切换到待机。
    /// </summary>
    private void SkillComplete()
    {
        if (_isCompleted)
            return;

        _isCompleted = true;
        AIObj.LaunchMissile();
        ChangeState(E_BossPlayerStateType.待机);
    }
}
