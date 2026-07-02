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
    public override BossStateType StateType => BossStateType.BulletPattern1;

    private float attackDuration = 6f;
    private float attackTimer;
    private bool isFiring;
    private float fireInterval = 0.1f;
    private float lastFireTime;
    private int bulletCount = 50;
    private int currentBulletIndex;

    // 子弹速度
    private float bulletSpeed = 10f;

    public BulletPattern1State(StateMachine<BossStateType, IBossFSMObj> machine) : base(machine)
    {
    }

    public override void EnterState()
    {
        base.EnterState();

        attackTimer = 0f;
        isFiring = true;
        lastFireTime = 0f;
        currentBulletIndex = 0;

        var boss = AIObj.GetBossController();
        boss.StopCurrentMove();
        boss.StopFloatingAnimation();

        if(AIObj.GetBossData().nowPhase == BossPhaseType.陶喆)
            MusicMgr.Instance.PlaySoundSafe(MyAssetBundleName.第四章音效包, "CB");
        else
            MusicMgr.Instance.PlaySoundSafe(MyAssetBundleName.第四章音效包, "鬼叫1");

        AIObj.DoBullet1Animation();

        LogSystem.Info("Boss进入圆形散射弹幕攻击");
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

        if (isFiring && attackTimer >= lastFireTime + fireInterval)
        {
            FireBullet();
            lastFireTime = attackTimer;
        }
    }

    public override void QuitState()
    {
        base.QuitState();

        AIObj.GetBossController().StartFloatingAnimation();

        LogSystem.Info("Boss退出圆形散射弹幕攻击");
    }

    /// <summary>
    /// 发射子弹：使用BulletFactory从对象池获取子弹
    /// </summary>
    private void FireBullet()
    {
        if (currentBulletIndex >= bulletCount)
        {
            isFiring = false;
            return;
        }
        //获取boss脚本
        var boss = AIObj.GetBossController();

        // 计算子弹角度（360度均匀分布）
        float angle = (360f / bulletCount) * currentBulletIndex;
        Vector2 direction = new Vector2(Mathf.Cos(angle * Mathf.Deg2Rad) , Mathf.Sin(angle * Mathf.Deg2Rad));

        // 通过工厂模式 + 对象池创建子弹，并设置子弹位置、方向和速度 
        AIObj.BulletFactory.SpawnBullet1(boss.transform.position, direction, bulletSpeed);

        currentBulletIndex++;
    }

    private void NotifyAttackComplete()
    {
        AIObj.GetBossData().AddAttackIndex();
        ChangeState(BossStateType.Recover);
    }
}
