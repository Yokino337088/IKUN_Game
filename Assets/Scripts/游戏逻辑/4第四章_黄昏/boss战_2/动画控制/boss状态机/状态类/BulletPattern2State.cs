using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TangmenFramework;
using UnityEngine;

/// <summary>
/// 螺旋弹幕攻击状态
/// </summary>
public class BulletPattern2State : BossBaseState
{
    /// <summary>
    /// 状态类型
    /// </summary>
    public override BossStateType StateType => BossStateType.BulletPattern2;

    /// <summary>
    /// 攻击持续时间
    /// </summary>
    private float attackDuration = 3f;

    /// <summary>
    /// 攻击计时器
    /// </summary>
    private float attackTimer;

    /// <summary>
    /// 是否正在发射子弹
    /// </summary>
    private bool isFiring;

    /// <summary>
    /// 子弹发射间隔
    /// </summary>
    private float fireInterval = 0.05f;

    /// <summary>
    /// 上次发射时间
    /// </summary>
    private float lastFireTime;

    /// <summary>
    /// 螺旋角度
    /// </summary>
    private float spiralAngle;

    /// <summary>
    /// 旋转速度
    /// </summary>
    private float rotationSpeed = 180f;

    public BulletPattern2State(StateMachine<BossStateType, IBossFSMObj> machine) : base(machine)
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
        isFiring = true;
        lastFireTime = 0f;
        spiralAngle = 0f;

        // 获取Boss控制器
        var boss = AIObj.GetBossController();

        // 停止浮空动画
        boss.StopFloatingAnimation();

        // 向玩家方向缓慢移动（DOTween逻辑在BossController中实现）
        AIObj.DoBullet2Animation();

        // 输出日志
        LogSystem.Info("Boss进入螺旋弹幕攻击");
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

        // 更新螺旋角度
        spiralAngle += rotationSpeed * Time.deltaTime;

        // 发射子弹
        if (isFiring && attackTimer >= lastFireTime + fireInterval)
        {
            FireBullet();
            lastFireTime = attackTimer;
        }
    }

    /// <summary>
    /// 退出状态
    /// </summary>
    public override void QuitState()
    {
        base.QuitState();

        // 停止移动动画
        AIObj.GetBossController().StopCurrentMove();

        // 恢复浮空动画
        AIObj.GetBossController().StartFloatingAnimation();

        // 输出日志
        LogSystem.Info("Boss退出螺旋弹幕攻击");
    }

    /// <summary>
    /// 发射子弹
    /// </summary>
    private void FireBullet()
    {
        var boss = AIObj.GetBossController();

        // 计算子弹方向（基于螺旋角度）
        float angleRad = spiralAngle * Mathf.Deg2Rad;
        Vector2 direction = new Vector2(
            Mathf.Cos(angleRad),
            Mathf.Sin(angleRad)
        );

        // 创建子弹（使用弹幕系统）
        // BulletSystem.Instance.SpawnBullet(
        //     boss.transform.position,
        //     direction,
        //     speed: 6f,
        //     damage: 1
        // );
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
