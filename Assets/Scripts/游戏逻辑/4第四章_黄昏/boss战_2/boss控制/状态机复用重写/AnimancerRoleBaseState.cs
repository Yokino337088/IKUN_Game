using Animancer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using TangmenFramework;

/// <summary>
/// Animancer状态基类，只做一个高级抽象，其他的状态基类都会基础这个类
/// </summary>
public class AnimancerRoleBaseState<TStateType, TFSMObj> : HierarchicalState<TStateType, TFSMObj> where TFSMObj: class, IAnimancerFSMObj
{
    /// <summary>
    /// 状态机对象的Animancer组件
    /// </summary>
    protected AnimancerComponent animancer;

    /// <summary>
    /// 当前的Animancer动画状态
    /// </summary>
    protected AnimancerState _currentAnimState;

    // 存储当前状态的动画事件序列
    protected List<AnimancerEvent.Sequence> _eventSequences = new List<AnimancerEvent.Sequence>();

    public AnimancerRoleBaseState(StateMachine<TStateType, TFSMObj> machine) : base(machine)
    {
        animancer = AIObj.Animancer;
    }

    public override TStateType StateType => throw new NotImplementedException();

    public override void EnterState()
    {
        
    }

    public override void QuitState()
    {
        
    }

    public override void UpdateState()
    {
        
    }

    /// <summary>
    /// 播放动画
    /// </summary>
    /// <param name="state"></param>
    /// <param name="fadeDuration"></param>
    protected void PlayAnimation(AnimationClip clip, float fadeDuration = 0.2f)
    {        
        
        if (clip == null)
        {
            LogSystem.Warning($"找不到动画剪辑: {clip.name}");
            return;
        }

        _currentAnimState = animancer.Play(clip, fadeDuration);
        
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


        // 通过 Events 属性获取事件序列，确保所有权正确
        var events = _currentAnimState.Events(AIObj);
        bool eventExists = false;
        foreach (var existingEvent in events)
        {
            if (Mathf.Abs(existingEvent.normalizedTime - normalizedTime) < 0.01f && existingEvent.callback == callback)
            {
                eventExists = true;
                break;
            }
        }
        //不重复，才添加事件
        if (!eventExists)
        {
            events.Add(normalizedTime, callback);
        }

    }

    /// <summary>
    /// 清除动画事件
    /// </summary>
    protected void ClearAnimationEvents()
    {
        foreach (var sequence in _eventSequences)
        {
            sequence.Clear();
        }
        _eventSequences.Clear();
    }
}