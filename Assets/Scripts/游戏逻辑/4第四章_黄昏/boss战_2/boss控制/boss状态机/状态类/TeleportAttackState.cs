using DG.Tweening;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TangmenFramework;
using UnityEngine;

/// <summary>
/// 瞬移攻击状态
/// </summary>
public class TeleportAttackState : BossBaseState
{
    /// <summary>
    /// 状态类型
    /// </summary>
    public override BossStateType StateType => BossStateType.TeleportAttack;

    /// <summary>
    /// 瞬移准备时间
    /// </summary>
    private float teleportDelay = 0.5f;

    /// <summary>
    /// 攻击延迟时间
    /// </summary>
    private float attackDelay = 0.5f;

    /// <summary>
    /// 返回延迟时间
    /// </summary>
    private float returnDelay = 0.3f;

    /// <summary>
    /// 当前计时器
    /// </summary>
    private float timer;

    /// <summary>
    /// 当前阶段
    /// </summary>
    private TeleportPhase currentPhase;

    /// <summary>
    /// 瞬移阶段枚举
    /// </summary>
    private enum TeleportPhase
    {
        Preparing,
        Teleporting,
        Attacking,
        Returning
    }

    public TeleportAttackState(StateMachine<BossStateType, IBossFSMObj> machine) : base(machine)
    {
    }

    /// <summary>
    /// 进入状态
    /// </summary>
    public override void EnterState()
    {
        base.EnterState();

        // 重置状态
        timer = 0f;
        currentPhase = TeleportPhase.Preparing;

        // 获取Boss控制器
        var boss = AIObj.GetBossController();

        // 停止浮空动画
        boss.StopFloatingAnimation();

        

        

        // 输出日志
        LogSystem.Info("Boss进入瞬移攻击");
    }

    /// <summary>
    /// 更新状态
    /// </summary>
    public override void UpdateState()
    {
        base.UpdateState();

        // 获取Boss控制器
        var boss = AIObj.GetBossController();

        // 根据当前阶段执行不同逻辑
        switch (currentPhase)
        {
            case TeleportPhase.Preparing:
                timer += Time.deltaTime;

                if (timer >= teleportDelay)
                {
                    // 瞬移到玩家附近
                    TeleportToPlayer();
                    // 进入攻击阶段
                    currentPhase = TeleportPhase.Attacking;
                    timer = 0f;
                }
                break;

            case TeleportPhase.Attacking:
                timer += Time.deltaTime;

                // 短暂延迟后攻击
                if (timer >= attackDelay)
                {
                    // 执行攻击
                    ExecuteAttack();
                    // 进入返回阶段
                    currentPhase = TeleportPhase.Returning;
                    timer = 0f;
                }
                break;

            case TeleportPhase.Returning:
                timer += Time.deltaTime;

                // 攻击完成后瞬移回原位
                if (timer >= returnDelay)
                {
                    // 瞬移回初始位置
                    TeleportBack();
                    // 通知父状态攻击完成
                    NotifyAttackComplete();
                }
                break;
        }
    }

    /// <summary>
    /// 瞬移到玩家附近
    /// </summary>
    private void TeleportToPlayer()
    {
        var boss = AIObj.GetBossController();
        var player = FindPlayerPosition();

        // 计算瞬移位置：贴在玩家身旁一个身位
        // 玩家朝向决定Boss出现在他的前方还是后方——这里选择到玩家前方（玩家面朝方向）一个身位
        Vector3 playerPos = player;
        float playerFacing = BossLevelMgr.Instance.PlayerControl.transform.localScale.x > 0 ? 1f : -1f;
        
        // Boss出现在玩家面朝方向的反方向（即玩家背后），距离1个单位
        float side = playerPos.x > boss.transform.position.x ? -1f : 1f;
        
        Vector3 teleportPos = playerPos + new Vector3(side * 1.5f, 0f, 0f);
        

        // 隐藏Boss（瞬移特效）
        boss.gameObject.SetActive(false);



        // 瞬移（瞬时移动）
        AIObj.TeleportTo(teleportPos, () =>
        {
            string soundName = AIObj.GetBossData().nowPhase == BossPhaseType.陶喆 ? "爱我还是他" : "鬼叫3";
            MusicMgr.Instance.PlaySoundSafe(MyAssetBundleName.第四章音效包, soundName);
        });

        // 显示Boss
        boss.gameObject.SetActive(true);


        // 播放瞬移到达特效(延迟播放)
        TimerMgr.Instance.CreateTimer(true, 500, () =>
        {
            AIObj.PlayTeleportArrivalEffect();
        });
        
    }

    /// <summary>
    /// 瞬移回初始位置
    /// </summary>
    private void TeleportBack()
    {
        var boss = AIObj.GetBossController();

        // 隐藏Boss
        boss.gameObject.SetActive(false);

       
        // 瞬移回初始位置
        AIObj.TeleportTo(boss.boss原点.position);

        // 显示Boss
        boss.gameObject.SetActive(true);


    }

    /// <summary>
    /// 执行攻击
    /// </summary>
    private void ExecuteAttack()
    {
        var boss = AIObj.GetBossController();
    }

    

    /// <summary>
    /// 查找玩家位置
    /// </summary>
    private Vector3 FindPlayerPosition()
    {
        var player = BossLevelMgr.Instance.PlayerControl;
        return player != null ? player.transform.position : Vector3.zero;
    }

    /// <summary>
    /// 退出状态
    /// </summary>
    public override void QuitState()
    {
        base.QuitState();

        // 恢复浮空动画
        AIObj.GetBossController().StartFloatingAnimation();

        // 输出日志
        LogSystem.Info("Boss退出瞬移攻击");
    }

    /// <summary>
    /// 通知父状态攻击完成
    /// </summary>
    private void NotifyAttackComplete()
    {
        // 索引+1，指向下一个攻击
        AIObj.GetBossData().AddAttackIndex();
        ChangeState(BossStateType.Recover);
    }
}
