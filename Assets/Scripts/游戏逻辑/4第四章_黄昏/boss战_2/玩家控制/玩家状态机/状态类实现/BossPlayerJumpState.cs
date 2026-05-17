﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿using TangmenFramework;

public class BossPlayerJumpState : BossPlayerState
{
    public override E_BossPlayerStateType StateType => E_BossPlayerStateType.跳跃;

    public BossPlayerJumpState(StateMachine<E_BossPlayerStateType, IBossPlayerFSMObj> machine) : base(machine) { }

    public override void EnterState()
    {
        base.EnterState();
        //播放跳跃动画
        PlayAnimation(E_BossPlayerStateType.跳跃);
        //执行跳跃
        AIObj.DoPlayerJump();
        MusicMgr.Instance.PlaySoundSafe(MyAssetBundleName.通用音效包, "跳跃");
        //注册事件：在动画的90%时触发下落事件，确保玩家在跳跃动画快结束时进入下落状态
        AddAnimationEvent(0.9f, FallCondition);
        LogSystem.Info("玩家进入跳跃状态");
    }

    
    public override void UpdateState()
    {
        base.UpdateState();
    }

    //动画事件回调：在动画播放到指定时间点时调用，触发下落逻辑
    private void FallCondition()
    {
        ChangeState(E_BossPlayerStateType.下落);
    }

}
