using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TangmenFramework;
using UnityEngine;

/// <summary>
/// Boss警戒状态类
/// </summary>
public class BossAlertState : BossBaseState
{
    /// <summary>
    /// 状态类型
    /// </summary>
    public override BossStateType StateType => BossStateType.Alert;

    /// <summary>
    /// 警戒持续时间
    /// </summary>
    private float alertDuration = 1f;

    /// <summary>
    /// 警戒计时器
    /// </summary>
    private float alertTimer;

    public BossAlertState(StateMachine<BossStateType, IBossFSMObj> machine) : base(machine)
    {
    }

    /// <summary>
    /// 进入状态
    /// </summary>
    public override void EnterState()
    {
        base.EnterState();

        // 重置警戒计时器
        alertTimer = 0f;

        // 获取Boss控制器
        var boss = AIObj.GetBossController();

        // 停止所有移动动画
        boss.StopCurrentMove();

       
        LogSystem.Info("Boss进入警戒状态 - 面向玩家");
    }

    /// <summary>
    /// 更新状态
    /// </summary>
    public override void UpdateState()
    {
        base.UpdateState();

        // 更新警戒计时器
        alertTimer += Time.deltaTime;

        // 警戒一段时间后进入攻击状态
        if (alertTimer >= alertDuration)
        {
           ChangeState(BossStateType.Attack);
           return;
        }

        // 如果玩家离开攻击范围，返回待机状态        
        if (!AIObj.IsPlayerInAttackRange())
        {
            ChangeState(BossStateType.Idle);
        }
    }

    /// <summary>
    /// 退出状态
    /// </summary>
    public override void QuitState()
    {
        base.QuitState();

        LogSystem.Info("Boss退出警戒状态");
    }
}
