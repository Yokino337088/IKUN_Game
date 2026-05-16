using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TangmenFramework;
using UnityEngine;

/// <summary>
/// Boss恢复状态类
/// </summary>
public class BossRecoverState : BossBaseState
{
    /// <summary>
    /// 状态类型
    /// </summary>
    public override BossStateType StateType => BossStateType.Recover;

    /// <summary>
    /// 恢复持续时间
    /// </summary>
    private float recoverDuration = 1f;

    /// <summary>
    /// 恢复计时器
    /// </summary>
    private float recoverTimer;

    public BossRecoverState(StateMachine<BossStateType, IBossFSMObj> machine) : base(machine)
    {
    }

    /// <summary>
    /// 进入状态
    /// </summary>
    public override void EnterState()
    {
        base.EnterState();

        // 重置恢复计时器
        recoverTimer = 0f;
        
        // 开始返回初始位置
        AIObj.ReturnToInitialPosition();

        // 恢复浮空动画
        AIObj.StartFloatingAnimation();

        // 播放恢复动画
        

        // 输出日志
        LogSystem.Info("Boss进入恢复状态 - 返回初始位置");
    }

    /// <summary>
    /// 更新状态
    /// </summary>
    public override void UpdateState()
    {
        base.UpdateState();

        // 更新恢复计时器
        recoverTimer += Time.deltaTime;

        // 检查是否需要转换状态
        CheckStateTransition();
    }

    /// <summary>
    /// 退出状态
    /// </summary>
    public override void QuitState()
    {
        base.QuitState();

        // 输出日志
        UnityEngine.Debug.Log("Boss退出恢复状态");
    }

    /// <summary>
    /// 检查状态转换
    /// </summary>
    private void CheckStateTransition()
    {
        // 恢复时间足够后返回待机状态
        if (recoverTimer >= recoverDuration)
        {
            var data = AIObj.GetBossData();
            // 【关键】如果正在进行攻击循环（一轮攻击还没打完），回到Attack继续下一个
            if (data.isInAttackCycle && data.currentAttackIndex < data.attackSequence.Count)
            {
                ChangeState(BossStateType.Attack);
                return;
            }

            // 攻击循环结束或不在循环中，回到Idle
            ChangeState(BossStateType.Idle);
            return;
        }

        // 如果玩家进入攻击范围，提前结束恢复状态进入警戒状态
        var boss = AIObj.GetBossController();
        if (boss.IsPlayerInAttackRange())
        {
            ChangeState(BossStateType.Alert);
        }
    }
}
