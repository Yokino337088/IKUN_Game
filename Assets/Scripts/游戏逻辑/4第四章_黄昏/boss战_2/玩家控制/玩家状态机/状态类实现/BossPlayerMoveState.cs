using TangmenFramework;
using UnityEngine;

public class BossPlayerMoveState : BossPlayerState
{
    public override E_BossPlayerStateType StateType => E_BossPlayerStateType.移动;

    private bool _pendingJump;

    public BossPlayerMoveState(StateMachine<E_BossPlayerStateType, IBossPlayerFSMObj> machine) : base(machine) { }

    public override void EnterState()
    {
        base.EnterState();
        _pendingJump = false;
        PlayAnimation(E_BossPlayerStateType.移动);
        LogSystem.Info("玩家进入移动状态");
    }

    public override void UpdateState()
    {
        base.UpdateState();

        if (_canReceiveInput)
        {
            if (_pendingJump)
            {
                ChangeState(E_BossPlayerStateType.跳跃);
                return;
            }

            if (Mathf.Abs(AIObj.PlayerMoveDir.x) <= 0.5f)
            {
                ChangeState(E_BossPlayerStateType.待机);
            }
        }
    }

    protected override void RegisterInputEvent()
    {
        EventCenter.Instance.AddEventListener(MyEventTypeString.玩家停止移动事件, OnStopMove);
        EventCenter.Instance.AddEventListener(MyEventTypeString.玩家跳跃事件, OnJumpInput);
        EventCenter.Instance.AddEventListener(MyEventTypeString.玩家普攻事件, OnAttackInput);
        EventCenter.Instance.AddEventListener(MyEventTypeString.玩家技能事件, OnSkillInput);
        EventCenter.Instance.AddEventListener(MyEventTypeString.玩家大招事件, OnUltimateInput);
    }

    protected override void UnRegisterInputEvent()
    {
        EventCenter.Instance.RemoveEventListener(MyEventTypeString.玩家停止移动事件, OnStopMove);
        EventCenter.Instance.RemoveEventListener(MyEventTypeString.玩家跳跃事件, OnJumpInput);
        EventCenter.Instance.RemoveEventListener(MyEventTypeString.玩家普攻事件, OnAttackInput);
        EventCenter.Instance.RemoveEventListener(MyEventTypeString.玩家技能事件, OnSkillInput);
        EventCenter.Instance.RemoveEventListener(MyEventTypeString.玩家大招事件, OnUltimateInput);
    }

    private void OnStopMove()
    {
        if (!_canReceiveInput) return;
        ChangeState(E_BossPlayerStateType.待机);
    }

    private void OnJumpInput()
    {
        if (!_canReceiveInput)
        {
            _pendingJump = true;
            return;
        }
        ChangeState(E_BossPlayerStateType.跳跃);
    }

    private void OnAttackInput()
    {
        if (!_canReceiveInput) return;
        ChangeState(E_BossPlayerStateType.普攻);
    }

    private void OnSkillInput()
    {
        if (!_canReceiveInput) return;
        ChangeState(E_BossPlayerStateType.技能);
    }

    private void OnUltimateInput()
    {
        if (!_canReceiveInput) return;
        ChangeState(E_BossPlayerStateType.大招);
    }
}
