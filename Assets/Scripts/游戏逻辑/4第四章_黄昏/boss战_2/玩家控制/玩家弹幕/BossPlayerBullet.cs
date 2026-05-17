using System;
using UnityEngine;

/// <summary>
/// 玩家普攻子弹（自动追踪Boss）
/// </summary>
public class BossPlayerBullet : BulletBase
{
    private float trackingStrength = 1f;
    private Vector3 bossPosition;
    private bool _firstFrame;

    private int minDamage = 50;
    private int maxDamage = 80;
    private float critChance = 0.35f;
    private float critMultiplier = 1.5f;

    public void Init(float speed, float trackingStr = 0.3f)
    {
        base.Init(speed);
        trackingStrength = Mathf.Clamp01(trackingStr);
        _firstFrame = true;
    }

    protected override void MoveLogic(float deltaTime)
    {
        if (BossLevelMgr.Instance == null)
            return;

        BossController boss = BossLevelMgr.Instance.BossController;
        if (boss == null)
            return;

        bossPosition = boss.transform.position;

        Vector3 currentDir = transform.right;

        if (_firstFrame || currentDir == Vector3.zero)
        {
            currentDir = (bossPosition - transform.position).normalized;
            _firstFrame = false;
        }

        Vector3 targetDir = (bossPosition - transform.position).normalized;
        Vector3 newDir = Vector3.Slerp(currentDir, targetDir, trackingStrength);

        transform.right = newDir;
        transform.position += newDir * moveSpeed * deltaTime;
    }

    /// <summary>
    /// 计算攻击伤害（最低伤害 ~ 最高伤害，暴击时翻倍）
    /// </summary>
    /// <param name="isCrit">输出是否暴击</param>
    public int CalculateDamage(out bool isCrit)
    {
        // 在最小值到最大值之间随机取一个基础伤害值
        int baseDamage = UnityEngine.Random.Range(minDamage, maxDamage + 1);

        // 随机判定是否暴击
        isCrit = UnityEngine.Random.value < critChance;

        if (isCrit)
        {
            int critDamage = Mathf.RoundToInt(baseDamage * critMultiplier);
            return critDamage;
        }

        return baseDamage;
    }

    /// <summary>
    /// 设置伤害参数
    /// </summary>
    public void SetDamageParams(int min, int max, float critRate, float critMult)
    {
        minDamage = min;
        maxDamage = max;
        critChance = critRate;
        critMultiplier = critMult;
    }
}
