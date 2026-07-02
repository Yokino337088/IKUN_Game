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
    public override BossStateType StateType => BossStateType.BulletPattern2;

    private float attackDuration = 6f;
    private float attackTimer;
    private bool isFiring;
    private float fireInterval = 0.1f;
    private float lastFireTime;
    private float spiralAngle = 60f;
    private float rotationSpeed = 180f;

    // 子弹速度
    private float bulletSpeed = 10f;

    public BulletPattern2State(StateMachine<BossStateType, IBossFSMObj> machine) : base(machine)
    {
    }

    public override void EnterState()
    {
        base.EnterState();

        attackTimer = 0f;
        isFiring = true;
        lastFireTime = 0f;
        spiralAngle = 0f;

        var boss = AIObj.GetBossController();
        boss.StopFloatingAnimation();

        AIObj.DoBullet2Animation();
        if (AIObj.GetBossData().nowPhase == BossPhaseType.陶喆)
            MusicMgr.Instance.PlaySoundSafe(MyAssetBundleName.第四章音效包, "胡彦斌");
        else
            MusicMgr.Instance.PlaySoundSafe(MyAssetBundleName.第四章音效包, "鬼叫2");
        LogSystem.Info("Boss进入螺旋弹幕攻击");
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

        // 更新螺旋角度
        spiralAngle += rotationSpeed * Time.deltaTime;

        // 发射子弹
        if (isFiring && attackTimer >= lastFireTime + fireInterval)
        {
            FireBullet();
            lastFireTime = attackTimer;
        }
    }

    public override void QuitState()
    {
        base.QuitState();

        AIObj.GetBossController().StopCurrentMove();
        AIObj.GetBossController().StartFloatingAnimation();

        LogSystem.Info("Boss退出螺旋弹幕攻击");
    }

    /// <summary>
    /// 发射子弹：螺旋角度持续旋转，使用BulletFactory从对象池获取子弹
    /// </summary>
    private void FireBullet()
    {
        var boss = AIObj.GetBossController();

        // 计算子弹方向（基于螺旋角度）
        float angleRad = spiralAngle * Mathf.Deg2Rad;
        Vector2 direction = new Vector2(Mathf.Cos(angleRad), Mathf.Sin(angleRad));

        // 通过工厂模式 + 对象池创建子弹
        AIObj.BulletFactory.SpawnBullet2(boss.transform.position, direction, bulletSpeed);
    }

    /// <summary>
    /// 状态完成通知：增加攻击索引并切换到恢复状态
    /// </summary>
    private void NotifyAttackComplete()
    {
        AIObj.GetBossData().AddAttackIndex();
        ChangeState(BossStateType.Recover);
    }
}
