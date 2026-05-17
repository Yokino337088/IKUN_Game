using Animancer;
using System;
using System.Collections;
using System.Collections.Generic;
using TangmenFramework;
using UnityEngine;

public class BossPlayerState : BaseState<E_BossPlayerStateType, IBossPlayerFSMObj>
{
    /// <summary>
    /// 状态机对象的Animancer组件
    /// </summary>
    protected AnimancerComponent animancer;

    /// <summary>
    /// 当前的Animancer动画状态
    /// </summary>
    protected AnimancerState _currentAnimState;

    /// <summary>
    /// 当前动画状态的事件序列引用
    /// AnimancerState.Events是懒加载属性，每次访问可能返回不同实例，
    /// 因此需要保存引用以确保能正确清理
    /// </summary>
    protected AnimancerEvent.Sequence _currentEventSequence;

    /// <summary>
    /// 是否允许响应输入事件。
    /// 状态进入的第一帧为 false，给 Animancer 一帧时间稳定动画。
    /// 之后每帧 UpdateState 中设为 true。
    /// 
    /// 设计原因：
    /// EventCenter 是同步分发的。状态进入时调用 RegisterInputEvent 注册事件监听后，
    /// 同一帧内如果有输入事件触发，会立刻回调导致 ChangeState，形成"重入式状态切换"。
    /// Animancer 的 Play() 在同一帧被多次调用会打乱内部混音状态，导致动画冻结。
    /// 因此进入状态后的第一帧内屏蔽所有输入，确保动画稳定后再开放输入响应。
    /// </summary>
    protected bool _canReceiveInput;

    public BossPlayerState(StateMachine<E_BossPlayerStateType, IBossPlayerFSMObj> machine) : base(machine)
    {
    }

    public override E_BossPlayerStateType StateType => throw new System.NotImplementedException();

    public override void EnterState()
    {
        _canReceiveInput = false;
        _currentEventSequence = null;
        animancer = AIObj.Animancer;
        RegisterInputEvent();
    }

    public override void QuitState()
    {
        UnRegisterInputEvent();
    }

    protected virtual void RegisterInputEvent() { }

    protected virtual void UnRegisterInputEvent() { }

    public override void UpdateState()
    {
        _canReceiveInput = true;
    }

    /// <summary>
    /// 播放动画
    /// </summary>
    /// <param name="state"></param>
    /// <param name="fadeDuration"></param>
    protected void PlayAnimation(E_BossPlayerStateType stateType, float fadeDuration = 0.065f)
    {
        AnimationClip clip = AIObj.GetAnimationClip(stateType);

        if (clip == null)
        {
            LogSystem.Warning($"找不到动画剪辑: {stateType}");
            return;
        }

        if (animancer == null)
        {
            LogSystem.Warning($"Animancer组件为空，无法播放动画: {stateType}");
            return;
        }

        _currentAnimState = animancer.Play(clip, fadeDuration);
        
        if (_currentAnimState == null)
        {
            LogSystem.Warning($"播放动画失败，_currentAnimState为空: {stateType}");
        }
    }

    /// <summary>
    /// 使用这个添加事件
    /// </summary>
    /// <param name="normalizedTime"></param>
    /// <param name="callback"></param>
    protected void AddAnimationEvent(float normalizedTime, Action callback)
    {
        if (_currentAnimState == null)
        {
            LogSystem.Debug("状态为空，不能添加动画事件");
            return;
        }

        // AnimancerState.Events 是一个方法，需要传入所有者对象
        // 获取当前动画状态的事件序列，并保存引用
        _currentAnimState.Events(AIObj, out _currentEventSequence);
        
        // 先清除所有旧事件，避免事件累积
        _currentEventSequence.Clear();
        
        // 添加新的动画事件
        _currentEventSequence.Add(normalizedTime, callback);
    }

    /// <summary>
    /// 清除动画事件
    /// </summary>
    protected void ClearAnimationEvents()
    {
        // 清除保存的事件序列引用
        if (_currentEventSequence != null)
        {
            _currentEventSequence.Clear();
            _currentEventSequence = null;
        }
        
        // 同时清除当前动画状态上的事件
        if (_currentAnimState != null)
        {
            AnimancerEvent.Sequence events;
            _currentAnimState.Events(AIObj, out events);
            events.Clear();
        }
    }
}
