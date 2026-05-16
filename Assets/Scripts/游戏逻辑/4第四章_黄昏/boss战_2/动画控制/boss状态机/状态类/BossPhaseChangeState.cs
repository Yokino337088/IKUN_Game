using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TangmenFramework;
using UnityEngine;

/// <summary>
/// Boss形态切换状态类
/// </summary>
public class BossPhaseChangeState : BossBaseState
{
    /// <summary>
    /// 状态类型
    /// </summary>
    public override BossStateType StateType => BossStateType.PhaseChange;

    /// <summary>
    /// 形态切换持续时间
    /// </summary>
    private float changeDuration = 2f;

    /// <summary>
    /// 切换计时器
    /// </summary>
    private float changeTimer;

    /// <summary>
    /// 是否已瞬移
    /// </summary>
    private bool hasTeleported = false;

    public BossPhaseChangeState(StateMachine<BossStateType, IBossFSMObj> machine) : base(machine)
    {
    }

    /// <summary>
    /// 进入状态
    /// </summary>
    public override void EnterState()
    {
        base.EnterState();

        // 重置计时器
        changeTimer = 0f;
        hasTeleported = false;

        // 获取Boss控制器
        var boss = AIObj.GetBossController();

        // 停止所有动画
        boss.StopCurrentMove();
        boss.StopFloatingAnimation();

        CheckPhasePlayAnimation();

        // 播放形态切换特效
        PlayPhaseChangeEffect();

        // 输出日志
        UnityEngine.Debug.Log("Boss进入形态切换状态");
    }

    /// <summary>
    /// 更新状态
    /// </summary>
    public override void UpdateState()
    {
        base.UpdateState();

        // 更新计时器
        changeTimer += Time.deltaTime;

        // 在切换动画中间进行瞬移
        if (!hasTeleported && changeTimer > changeDuration * 0.5f)
        {
            TeleportToNewPosition();
            hasTeleported = true;
        }

        // 切换完成
        if (changeTimer >= changeDuration)
        {
            // 转换到待机状态
            ChangeState(BossStateType.Idle);
        }
    }

    /// <summary>
    /// 退出状态
    /// </summary>
    public override void QuitState()
    {
        base.QuitState();

        // 输出日志
        UnityEngine.Debug.Log("Boss退出形态切换状态");
    }

    /// <summary>
    /// 播放形态切换特效
    /// </summary>
    private void PlayPhaseChangeEffect()
    {
        // 播放粒子特效、音效等
        // 实现省略...
    }

    /// <summary>
    /// 瞬移到新位置
    /// </summary>
    private void TeleportToNewPosition()
    {
        var boss = AIObj.GetBossController();

        // 随机选择新位置（在初始位置附近）
        Vector3 newPosition = boss.transform.position;
        newPosition.x += UnityEngine.Random.Range(-3f, 3f);
        newPosition.y += UnityEngine.Random.Range(-2f, 2f);

        // 瞬移到新位置
        AIObj.TeleportTo(newPosition);

        // 播放瞬移到达特效
        PlayTeleportArrivalEffect(newPosition);
    }

    /// <summary>
    /// 播放瞬移到达特效
    /// </summary>
    private void PlayTeleportArrivalEffect(Vector3 position)
    {
        // 播放粒子特效
        // 实现省略...
    }
}
