using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TangmenFramework;

/// <summary>
/// Boss待机状态类,逻辑就是检测玩家是否进入了攻击范围，如果进入，那就切换到警惕状态
/// </summary>
public class BossIdleState : BossBaseState
{
    /// <summary>
    /// 状态类型
    /// </summary>
    public override BossStateType StateType => BossStateType.Idle;

    /// <summary>
    /// 攻击范围检测间隔
    /// </summary>
    private float checkInterval = 0.2f;

    /// <summary>
    /// 检测计时器
    /// </summary>
    private float checkTimer;

    public BossIdleState(StateMachine<BossStateType, IBossFSMObj> machine) : base(machine)
    {
    }

    /// <summary>
    /// 进入状态
    /// </summary>
    public override void EnterState()
    {
        base.EnterState();

        // 重置检测计时器
        checkTimer = 0f;        

        // 确保浮空动画运行
        AIObj.StartFloatingAnimation();

        //播放对应阶段的动画
        CheckPhasePlayAnimation();

        // 如果不在初始位置，平滑返回
        if (!AIObj.IsMoving)
        {
            AIObj.ReturnToInitialPosition();
        }

        LogSystem.Info("Boss进入待机状态 - 开始浮空");
    }

    /// <summary>
    /// 更新状态
    /// </summary>
    public override void UpdateState()
    {
        base.UpdateState();

        // 更新检测计时器
        checkTimer += UnityEngine.Time.deltaTime;

        // 定期检测玩家是否进入攻击范围
        if (checkTimer >= checkInterval)
        {
            checkTimer = 0f;
            CheckPlayerInRange();
        }
    }

    /// <summary>
    /// 退出状态
    /// </summary>
    public override void QuitState()
    {
        base.QuitState();

        LogSystem.Info("Boss退出待机状态");
    }

    /// <summary>
    /// 检测玩家是否进入攻击范围
    /// </summary>
    private void CheckPlayerInRange()
    {        
        // 检测玩家是否进入攻击范围
        if (AIObj.IsPlayerInAttackRange())
        {
            // 转换到警戒状态
            ChangeState(BossStateType.Alert);
        }
    }
}
