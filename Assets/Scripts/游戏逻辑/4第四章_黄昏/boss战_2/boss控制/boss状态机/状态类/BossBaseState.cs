using Animancer;
using System;
using TangmenFramework;
using UnityEngine;

/// <summary>
/// boss状态基类
/// </summary>
public class BossBaseState : BaseState<BossStateType, IBossFSMObj>
{
    protected AnimancerComponent animancer;

    protected AnimancerState _currentAnimState;

    public override BossStateType StateType => throw new NotImplementedException();

    public BossBaseState(StateMachine<BossStateType, IBossFSMObj> machine) : base(machine)
    {
        animancer = AIObj.Animancer;
    }

    protected void CheckPhasePlayAnimation()
    {        
        PlayAnimation(AIObj.GetAnimationClip(AIObj.CheckNowPhase()));
    }

    public override void EnterState()
    {
        EventCenter.Instance.AddEventListener<BossPhaseType>(MyEventTypeString.Boss动画切换事件,ChangePhaseAnimation);
    }

    public override void QuitState()
    {
    }

    public override void UpdateState()
    {
        EventCenter.Instance.RemoveEventListener<BossPhaseType>(MyEventTypeString.Boss动画切换事件, ChangePhaseAnimation);
    }

    protected void PlayAnimation(AnimationClip clip, float fadeDuration = 0.2f)
    {
        if (clip == null)
        {
            LogSystem.Warning("找不到动画剪辑!");
            return;
        }

        if (animancer == null)
        {
            LogSystem.Warning("Animancer组件为空，无法播放动画!");
            return;
        }

        _currentAnimState = animancer.Play(clip, fadeDuration);
    }

    private void ChangePhaseAnimation(BossPhaseType bossPhaseType)
    {
        PlayAnimation(AIObj.GetAnimationClip(bossPhaseType));
    }
}
