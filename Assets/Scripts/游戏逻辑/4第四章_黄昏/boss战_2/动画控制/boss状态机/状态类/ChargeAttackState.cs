using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TangmenFramework;
using UnityEngine;

/// <summary>
/// 蓄力攻击状态
/// </summary>
public class ChargeAttackState : BossBaseState
{
    /// <summary>
    /// 状态类型
    /// </summary>
    public override BossStateType StateType => BossStateType.ChargeAttack;

    /// <summary>
    /// 蓄力时间
    /// </summary>
    private float chargeDuration = 2f;

    /// <summary>
    /// 攻击持续时间
    /// </summary>
    private float attackDuration = 1f;

    /// <summary>
    /// 蓄力计时器
    /// </summary>
    private float chargeTimer;

    /// <summary>
    /// 攻击计时器
    /// </summary>
    private float attackTimer;

    /// <summary>
    /// 是否正在蓄力
    /// </summary>
    private bool isCharging = false;

    /// <summary>
    /// 是否已发射
    /// </summary>
    private bool hasFired = false;

    public ChargeAttackState(StateMachine<BossStateType, IBossFSMObj> machine) : base(machine)
    {
    }

    /// <summary>
    /// 进入状态
    /// </summary>
    public override void EnterState()
    {
        base.EnterState();

        // 重置状态
        chargeTimer = 0f;
        attackTimer = 0f;
        isCharging = true;
        hasFired = false;

        // 获取Boss控制器
        var boss = AIObj.GetBossController();

        // 停止浮空动画
        boss.StopFloatingAnimation();

        // 移动到边缘位置（DOTween逻辑在BossController中实现）
        AIObj.DoChargeAnimation(() =>
        {
            // 到达位置后开始蓄力
            StartCharging();
        });

        // 输出日志
        UnityEngine.Debug.Log("Boss进入蓄力攻击");
    }

    /// <summary>
    /// 开始蓄力
    /// </summary>
    private void StartCharging()
    {
        isCharging = true;
        chargeTimer = 0f;

        // 播放蓄力DOTween动画效果（缩放脉冲 + 变红）
        AIObj.DoChargeAnimationEffect(chargeDuration);
    }

    /// <summary>
    /// 更新状态
    /// </summary>
    public override void UpdateState()
    {
        base.UpdateState();

        if (isCharging)
        {
            // 更新蓄力计时器
            chargeTimer += Time.deltaTime;

            // 更新蓄力进度（可以通过动画参数传递）
            float chargePercent = Mathf.Clamp01(chargeTimer / chargeDuration);
            // GetBossController().animator.SetFloat("ChargePercent", chargePercent);

            // 蓄力完成后发射
            if (chargeTimer >= chargeDuration)
            {
                FireChargeAttack();
                isCharging = false;
                hasFired = true;
            }
        }
        else if (hasFired)
        {
            // 更新攻击计时器
            attackTimer += Time.deltaTime;

            // 攻击完成后通知父状态
            if (attackTimer >= attackDuration)
            {
                NotifyAttackComplete();
            }
        }
    }

    /// <summary>
    /// 发射蓄力攻击
    /// </summary>
    private void FireChargeAttack()
    {
        var boss = AIObj.GetBossController();

        // 停止蓄力DOTween效果，恢复缩放和颜色
        AIObj.StopChargeAnimationEffect();

        // 播放攻击动画
        // boss.animator.SetTrigger("FireCharge");

        // 创建大量子弹（360度均匀分布）
        int bulletCount = 24;
        for (int i = 0; i < bulletCount; i++)
        {
            float angle = (360f / bulletCount) * i;
            Vector2 direction = new Vector2(
                Mathf.Cos(angle * Mathf.Deg2Rad),
                Mathf.Sin(angle * Mathf.Deg2Rad)
            );

            // 创建子弹
            // BulletSystem.Instance.SpawnBullet(
            //     boss.transform.position,
            //     direction,
            //     speed: 8f,
            //     damage: 2
            // );
        }
    }

    /// <summary>
    /// 退出状态
    /// </summary>
    public override void QuitState()
    {
        base.QuitState();

        // 停止蓄力DOTween效果（如果状态被中断）
        AIObj.StopChargeAnimationEffect();

        // 停止所有移动
        AIObj.GetBossController().StopCurrentMove();

        // 恢复浮空动画
        AIObj.GetBossController().StartFloatingAnimation();

        // 输出日志
        UnityEngine.Debug.Log("Boss退出蓄力攻击");
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
