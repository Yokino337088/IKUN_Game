﻿using TangmenFramework;

public class BossPlayerFallState : BossPlayerState
{
    public override E_BossPlayerStateType StateType => E_BossPlayerStateType.下落;

    /// <summary>
    /// 超时自动切回待机的时间（秒），防止卡在下落状态
    /// </summary>
    private float fallTimeout = 2f;

    private float fallTimer;

    public BossPlayerFallState(StateMachine<E_BossPlayerStateType, IBossPlayerFSMObj> machine) : base(machine) { }

    public override void EnterState()
    {
        base.EnterState();
        fallTimer = 0f;
        PlayAnimation(E_BossPlayerStateType.下落);
        EventCenter.Instance.AddEventListener(MyEventTypeString.玩家落地事件, CheckLandCondition);
        LogSystem.Info("玩家进入下落状态");
    }

    public override void UpdateState()
    {
        base.UpdateState();

        fallTimer += UnityEngine.Time.deltaTime;
        if (fallTimer >= fallTimeout)
        {
            ChangeState(E_BossPlayerStateType.待机);
        }
    }

    public override void QuitState()
    {
        base.QuitState();
        MusicMgr.Instance.PlaySoundSafe(MyAssetBundleName.通用音效包, "落地");
        EventCenter.Instance.RemoveEventListener(MyEventTypeString.玩家落地事件, CheckLandCondition);
    }

    private void CheckLandCondition()
    {
        if (!_canReceiveInput) return;
        ChangeState(E_BossPlayerStateType.待机);
    }
}
