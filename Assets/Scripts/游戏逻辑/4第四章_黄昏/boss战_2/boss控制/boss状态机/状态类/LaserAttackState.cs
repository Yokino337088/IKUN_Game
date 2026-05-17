using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TangmenFramework;
using UnityEngine;

/// <summary>
/// 激光攻击状态
/// Boss移动到玩家上方后，生成10道环绕自身旋转的激光
/// </summary>
public class LaserAttackState : BossBaseState
{
    public override BossStateType StateType => BossStateType.LaserAttack;

    /// <summary>
    /// 攻击持续时间
    /// </summary>
    private float attackDuration = 6f;
    private float attackTimer;
    private bool isAttacking;

    public LaserAttackState(StateMachine<BossStateType, IBossFSMObj> machine) : base(machine)
    {
    }

    public override void EnterState()
    {
        base.EnterState();

        attackTimer = 0f;
        isAttacking = false;

        var boss = AIObj.GetBossController();
        boss.StopFloatingAnimation();
        // 播放激光攻击音效
        MusicMgr.Instance.PlaySoundSafe(MyAssetBundleName.第四章音效包, "歌剧", (audio) =>
        {
            // 设置音效持续时间，确保在攻击结束时停止播放
            TimerMgr.Instance.CreateTimer(true, (int)attackDuration * 1000, () =>
            {
                MusicMgr.Instance.StopSound(audio);
            });
        });
        AIObj.DoLaserAnimation(() =>
        {
            isAttacking = true;
            AIObj.SpawnLasers();
            AIObj.DoLaserAnimationEffect();
        });

        LogSystem.Info("Boss进入激光攻击");
    }

    public override void UpdateState()
    {
        base.UpdateState();

        attackTimer += Time.deltaTime;

        if (attackTimer >= attackDuration)
        {
            NotifyAttackComplete();
            return;
        }

        if (!isAttacking)
            return;

    }

    public override void QuitState()
    {
        base.QuitState();

        AIObj.RecycleAllLasers();

        AIObj.StopLaserAnimationEffect();
        AIObj.GetBossController().StopCurrentMove();
        AIObj.GetBossController().StartFloatingAnimation();

        LogSystem.Info("Boss退出激光攻击");
    }

    private void NotifyAttackComplete()
    {
        AIObj.GetBossData().AddAttackIndex();
        ChangeState(BossStateType.Recover);
    }
}
