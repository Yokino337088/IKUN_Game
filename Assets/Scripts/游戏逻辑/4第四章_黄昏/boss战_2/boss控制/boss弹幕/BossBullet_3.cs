using System;
using System.Collections.Generic;
using TangmenFramework;
using UnityEngine;

/// <summary>
/// Boss大火球弹幕
/// 自动追踪玩家，到达一定距离后播放爆炸特效并回收
/// </summary>
public class BossBullet_3 : BulletBase
{
    /// <summary>
    /// 追踪强度：0=直线飞行，1=完美追踪（推荐0.5~0.8）
    /// </summary>
    private float trackingStrength = 0.6f;

    /// <summary>
    /// 爆炸触发距离（子弹离玩家小于此距离时爆炸）
    /// </summary>
    private float explosionDistance = 2.5f;

    

    /// <summary>
    /// 追踪的目标Transform（玩家）
    /// </summary>
    private Transform target;

    /// <summary>
    /// 玩家的最后已知位置
    /// </summary>
    private Vector3 lastTargetPos;

    /// <summary>
    /// 是否已爆炸（防止重复爆炸）
    /// </summary>
    private bool hasExploded;

    /// <summary>
    /// 当前移动方向
    /// </summary>
    private Vector3 currentDirection;

    private Animator animator;

    public void Start()
    {
        animator = GetComponent<Animator>();   
    }

    /// <summary>
    /// 初始化大火球
    /// </summary>
    /// <param name="speed">移动速度</param>
    /// <param name="trackingStr">追踪强度（0~1）</param>
    /// <param name="explosionDist">爆炸触发距离</param>
    public override void Init(float speed)
    {
        base.Init(speed);
        
        hasExploded = false;

        // 获取玩家Transform
        if (BossLevelMgr.Instance != null && BossLevelMgr.Instance.PlayerControl != null)
        {
            target = BossLevelMgr.Instance.PlayerControl.transform;
            lastTargetPos = target.position;
            currentDirection = (lastTargetPos - transform.position).normalized;
        }
        else
        {
            currentDirection = Vector3.right;
        }

        lifetime = 10f;
    }

    /// <summary>
    /// 追踪运动逻辑
    /// 每帧计算指向玩家的方向，使用Slerp平滑转向，到达爆炸距离后引爆
    /// </summary>
    protected override void MoveLogic(float deltaTime)
    {
        if (hasExploded)
            return;

        // 获取玩家当前位置
        if (target != null)
        {
            lastTargetPos = target.position;
        }

        float distanceToPlayer = Vector3.Distance(transform.position, lastTargetPos);

        // 到达爆炸距离 → 引爆
        if (distanceToPlayer <= explosionDistance)
        {
            Explode();
            return;
        }

        // 计算理想方向：指向玩家
        Vector3 targetDir = (lastTargetPos - transform.position).normalized;

        // Slerp平滑转向，trackingStrength越大追踪越强
        currentDirection = Vector3.Slerp(currentDirection, targetDir, trackingStrength * deltaTime * 5f);
        currentDirection.Normalize();

        // 更新朝向（让火球的视觉方向与运动方向一致）
        float angle = Mathf.Atan2(currentDirection.y, currentDirection.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, angle);

        // 沿当前方向移动
        transform.position += currentDirection * moveSpeed * deltaTime;
    }

    /// <summary>
    /// 爆炸：生成爆炸特效，然后回收子弹
    /// </summary>
    private void Explode()
    {
        if (hasExploded)
            return;

        hasExploded = true;
        isActive = false;

        //播放爆炸动画
        animator.SetTrigger("爆炸");

        
    }

    /// 爆炸动画结束时的回调（通过动画事件调用）
    public void OnExplosionAnimEnd()
    {
        // 回收子弹到对象池
        GOPoolMgr.Instance.PushObj(gameObject);
    }
}
