using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TangmenFramework;
using UnityEngine;

/// <summary>
/// 激光攻击状态
/// </summary>
public class LaserAttackState : BossBaseState
{
    /// <summary>
    /// 状态类型
    /// </summary>
    public override BossStateType StateType => BossStateType.LaserAttack;

    /// <summary>
    /// 攻击持续时间
    /// </summary>
    private float attackDuration = 3f;

    /// <summary>
    /// 攻击计时器
    /// </summary>
    private float attackTimer;

    /// <summary>
    /// 是否正在攻击
    /// </summary>
    private bool isAttacking;

    public LaserAttackState(StateMachine<BossStateType, IBossFSMObj> machine) : base(machine)
    {
    }

    /// <summary>
    /// 进入状态
    /// </summary>
    public override void EnterState()
    {
        base.EnterState();

        // 重置状态
        attackTimer = 0f;
        isAttacking = false;

        // 获取Boss控制器
        var boss = AIObj.GetBossController();

        // 停止浮空动画
        boss.StopFloatingAnimation();

        // 快速移动到玩家上方（DOTween逻辑在BossController中实现）
        AIObj.DoLaserAnimation(() =>
        {
            // 移动完成后开始攻击
            isAttacking = true;
        });

        // 播放激光DOTween动画效果（震动）
        AIObj.DoLaserAnimationEffect();

        // 输出日志
        UnityEngine.Debug.Log("Boss进入激光攻击");
    }

    /// <summary>
    /// 更新状态
    /// </summary>
    public override void UpdateState()
    {
        base.UpdateState();

        // 更新攻击计时器
        attackTimer += Time.deltaTime;

        // 检查攻击是否完成
        if (attackTimer >= attackDuration)
        {
            NotifyAttackComplete();
            return;
        }

        // 只有在攻击阶段才移动
        if (!isAttacking)
            return;

        // 激光水平移动（DOTween逻辑在BossController中实现）
        AIObj.DoLaserHorizontalMove();
    }

    /// <summary>
    /// 退出状态
    /// </summary>
    public override void QuitState()
    {
        base.QuitState();

        // 停止激光DOTween效果
        AIObj.StopLaserAnimationEffect();

        // 停止所有移动
        AIObj.GetBossController().StopCurrentMove();

        // 恢复浮空动画
        AIObj.GetBossController().StartFloatingAnimation();

        // 输出日志
        UnityEngine.Debug.Log("Boss退出激光攻击");
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
