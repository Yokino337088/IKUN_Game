using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TangmenFramework;
using UnityEngine;

/// <summary>
/// 圆形散射弹幕攻击状态
/// </summary>
public class BulletPattern1State : BossBaseState
{
    /// <summary>
    /// 状态类型
    /// </summary>
    public override BossStateType StateType => BossStateType.BulletPattern1;

    /// <summary>
    /// 攻击持续时间
    /// </summary>
    private float attackDuration = 2f;

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
    private float fireInterval = 0.25f;

    /// <summary>
    /// 上次发射时间
    /// </summary>
    private float lastFireTime;

    /// <summary>
    /// 子弹数量
    /// </summary>
    private int bulletCount = 40;

    /// <summary>
    /// 当前发射的子弹索引
    /// </summary>
    private int currentBulletIndex;

    public BulletPattern1State(StateMachine<BossStateType, IBossFSMObj> machine) : base(machine)
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
        currentBulletIndex = 0;

        //播放动画
        AIObj.DoBullet1Animation();

        // 获取Boss控制器
        var boss = AIObj.GetBossController();

        // 停止所有移动，保持当前位置稳定
        boss.StopCurrentMove();

        // 停止浮空动画（攻击时保持稳定）
        boss.StopFloatingAnimation();


        // 输出日志
        LogSystem.Info("Boss进入圆形散射弹幕攻击");
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

        // 恢复浮空动画
        AIObj.GetBossController().StartFloatingAnimation();

        // 输出日志
        LogSystem.Info("Boss退出圆形散射弹幕攻击");
    }

    /// <summary>
    /// 发射子弹
    /// </summary>
    private void FireBullet()
    {
        if (currentBulletIndex >= bulletCount)
        {
            isFiring = false;
            return;
        }

        var boss = AIObj.GetBossController();

        // 计算子弹角度（360度均匀分布）
        float angle = (360f / bulletCount) * currentBulletIndex;
        Vector2 direction = new Vector2(
            Mathf.Cos(angle * Mathf.Deg2Rad),
            Mathf.Sin(angle * Mathf.Deg2Rad)
        );

        // 创建子弹（使用弹幕系统）
        // BulletSystem.Instance.SpawnBullet(
        //     boss.transform.position,
        //     direction,
        //     speed: 5f,
        //     damage: 1
        // );

        currentBulletIndex++;
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
