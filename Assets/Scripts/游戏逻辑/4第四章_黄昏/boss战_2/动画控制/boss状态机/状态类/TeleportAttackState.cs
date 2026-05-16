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

        // 播放瞬移准备动画
        //PlayAnimation(BossStateType.TeleportAttack);

        // 播放瞬移特效
        PlayTeleportEffect(boss.transform.position);

        // 输出日志
        UnityEngine.Debug.Log("Boss进入瞬移攻击");
    }

    /// <summary>
    /// 更新状态
    /// </summary>
    public override void UpdateState()
    {
        base.UpdateState();

        // 获取Boss控制器
        var boss = AIObj.GetBossController();

        switch (currentPhase)
        {
            case TeleportPhase.Preparing:
                timer += Time.deltaTime;

                if (timer >= teleportDelay)
                {
                    // 瞬移到玩家附近
                    TeleportToPlayer();
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
                    currentPhase = TeleportPhase.Returning;
                    timer = 0f;
                }
                break;

            case TeleportPhase.Returning:
                timer += Time.deltaTime;

                // 攻击完成后瞬移回原位
                if (timer >= returnDelay)
                {
                    TeleportBack();
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

        // 计算瞬移位置（玩家附近但保持安全距离）
        Vector3 direction = (player - boss.transform.position).normalized;
        Vector3 teleportPos = player - direction * 3f; // 在玩家3个单位外

        // 限制在战斗区域内
        teleportPos.x = Mathf.Clamp(teleportPos.x,
            boss.transform.position.x - 5f,
            boss.transform.position.x + 5f);
        teleportPos.y = Mathf.Clamp(teleportPos.y,
            boss.transform.position.y - 2f,
            boss.transform.position.y + 4f);

        // 隐藏Boss（瞬移特效）
        boss.gameObject.SetActive(false);

        // 播放瞬移特效
        PlayTeleportEffect(boss.transform.position);

        // 瞬移（瞬时移动）
        AIObj.TeleportTo(teleportPos);

        // 显示Boss
        boss.gameObject.SetActive(true);

        // 播放到达特效
        PlayTeleportArrivalEffect(teleportPos);

        
    }

    /// <summary>
    /// 瞬移回初始位置
    /// </summary>
    private void TeleportBack()
    {
        var boss = AIObj.GetBossController();

        // 隐藏Boss
        boss.gameObject.SetActive(false);

        // 播放瞬移特效
        PlayTeleportEffect(boss.transform.position);

        // 瞬移回初始位置
        AIObj.TeleportTo(boss.transform.position);

        // 显示Boss
        boss.gameObject.SetActive(true);

        // 播放到达特效
        PlayTeleportArrivalEffect(boss.transform.position);
    }

    /// <summary>
    /// 执行攻击
    /// </summary>
    private void ExecuteAttack()
    {
        var boss = AIObj.GetBossController();

        // 播放攻击动画
        // boss.animator.SetTrigger("TeleportAttack");

        // 创建攻击子弹
        // 向玩家方向发射子弹
        Vector3 playerPos = FindPlayerPosition();
        Vector3 direction = (playerPos - boss.transform.position).normalized;

        // 创建多个子弹
        for (int i = -2; i <= 2; i++)
        {
            float angleOffset = i * 15f;
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg + angleOffset;
            Vector2 bulletDir = new Vector2(
                Mathf.Cos(angle * Mathf.Deg2Rad),
                Mathf.Sin(angle * Mathf.Deg2Rad)
            );

            // 创建子弹
            // BulletSystem.Instance.SpawnBullet(
            //     boss.transform.position,
            //     bulletDir,
            //     speed: 10f,
            //     damage: 2
            // );
        }
    }

    /// <summary>
    /// 播放瞬移特效
    /// </summary>
    private void PlayTeleportEffect(Vector3 position)
    {
        // 播放粒子特效
        // 实现省略...
    }

    /// <summary>
    /// 播放瞬移到达特效
    /// </summary>
    private void PlayTeleportArrivalEffect(Vector3 position)
    {
        // 播放粒子特效
        // 实现省略...
    }

    /// <summary>
    /// 查找玩家位置
    /// </summary>
    private Vector3 FindPlayerPosition()
    {
        var player = GameObject.FindGameObjectWithTag("Player");
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
        UnityEngine.Debug.Log("Boss退出瞬移攻击");
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
