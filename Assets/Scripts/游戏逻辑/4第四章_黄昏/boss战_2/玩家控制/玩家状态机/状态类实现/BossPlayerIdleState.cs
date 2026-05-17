using TangmenFramework;
using UnityEngine;

public class BossPlayerIdleState : BossPlayerState
{
    public override E_BossPlayerStateType StateType => E_BossPlayerStateType.待机;

    public BossPlayerIdleState(StateMachine<E_BossPlayerStateType, IBossPlayerFSMObj> machine) : base(machine) { }

    public override void EnterState()
    {
        base.EnterState();
        PlayAnimation(E_BossPlayerStateType.待机);
        LogSystem.Info("玩家进入待机状态");
    }

    public override void UpdateState()
    {
        base.UpdateState();

        if (_canReceiveInput && Mathf.Abs(AIObj.PlayerMoveDir.x) > 0.1f)
        {
            ChangeState(E_BossPlayerStateType.移动);
        }
    }

    protected override void RegisterInputEvent()
    {
        EventCenter.Instance.AddEventListener(MyEventTypeString.玩家开始移动事件, OnMoveInput);
        EventCenter.Instance.AddEventListener(MyEventTypeString.玩家跳跃事件, OnJumpInput);
        EventCenter.Instance.AddEventListener(MyEventTypeString.玩家普攻事件, OnAttackInput);
        EventCenter.Instance.AddEventListener(MyEventTypeString.玩家技能事件, OnSkillInput);
        EventCenter.Instance.AddEventListener(MyEventTypeString.玩家大招事件, OnUltimateInput);
    }

    protected override void UnRegisterInputEvent()
    {
        EventCenter.Instance.RemoveEventListener(MyEventTypeString.玩家开始移动事件, OnMoveInput);
        EventCenter.Instance.RemoveEventListener(MyEventTypeString.玩家跳跃事件, OnJumpInput);
        EventCenter.Instance.RemoveEventListener(MyEventTypeString.玩家普攻事件, OnAttackInput);
        EventCenter.Instance.RemoveEventListener(MyEventTypeString.玩家技能事件, OnSkillInput);
        EventCenter.Instance.RemoveEventListener(MyEventTypeString.玩家大招事件, OnUltimateInput);
    }

    private void OnMoveInput()
    {
        if (!_canReceiveInput) return;
        ChangeState(E_BossPlayerStateType.移动);
    }

    private void OnJumpInput()
    {
        if (!_canReceiveInput) return;
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
