using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TangmenFramework;
using Unity.VisualScripting;
using UnityEngine;

/// <summary>
/// Boss攻击状态类（分层状态机父状态）
/// </summary>
public class BossAttackState : BossBaseState
{
    /// <summary>
    /// 状态类型
    /// </summary>
    public override BossStateType StateType => BossStateType.Attack;

   

    public BossAttackState(StateMachine<BossStateType, IBossFSMObj> machine) : base(machine)
    {
    }

    /// <summary>
    /// 进入状态
    /// </summary>
    public override void EnterState()
    {
        base.EnterState();

       

        //播放进入攻击的动画
        AIObj.DoAttackStartAnimation();

        // 停止浮空动画
        AIObj.StopFloatingAnimation();

        BossData data = AIObj.GetBossData();

        // 情况1：首次进入攻击状态（没有进行中的攻击循环）
        if (!data.isInAttackCycle)
        {
            // 根据当前阶段构建攻击序列
            BuildAttackSequence(data);
            data.currentAttackIndex = 0;
            data.isInAttackCycle = true;

            LogSystem.Info($"Boss开始新攻击循环，阶段: {AIObj.CheckNowPhase()}，序列: [{string.Join(", ", data.attackSequence)}]");
        }
        // 情况2：从Recover回来后继续攻击循环（isInAttackCycle=true，currentAttackIndex已经++了）
        // 切换到当前索引对应的攻击模式
        if(data.currentAttackIndex < data.attackSequence.Count)
        {
            //获取状态并切换
            BossStateType bossStateType = data.attackSequence[data.currentAttackIndex];
            ChangeState(bossStateType);
            LogSystem.Info($"Boss攻击 [{data.currentAttackIndex + 1}/{data.attackSequence.Count}]");
        }
        else
        {
            // 序列全部执行完毕
            data.currentAttackIndex = -1;
            data.isInAttackCycle = false;

            LogSystem.Info("Boss攻击循环全部完成");
            ChangeState(BossStateType.Recover);
        }
        LogSystem.Info("Boss进入攻击状态");
    }

    /// <summary>
    /// 更新状态
    /// </summary>
    public override void UpdateState()
    {
        base.UpdateState();

        
    }

    /// <summary>
    /// 退出状态
    /// </summary>
    public override void QuitState()
    {
        base.QuitState();        

        // 恢复浮空动画
        AIObj.StartFloatingAnimation();

        // 输出日志
        LogSystem.Info("Boss退出攻击状态");
    }

    /// <summary>
    /// 根据当前阶段构建攻击序列
    /// </summary>
    private void BuildAttackSequence(BossData data)
    {
        BossPhaseType currentPhase = AIObj.CheckNowPhase();
        data.attackSequence = new List<BossStateType>();
        switch (currentPhase)
        {
            case BossPhaseType.道莉1:
                // Phase1（100%~80%）：圆形散射 + 螺旋弹幕
                data.attackSequence.Add(BossStateType.BulletPattern1);
                data.attackSequence.Add(BossStateType.BulletPattern2);
                break;

            case BossPhaseType.道莉2:
                // Phase2（80%~50%）：螺旋弹幕 + 激光
                data.attackSequence.Add(BossStateType.BulletPattern1);
                data.attackSequence.Add(BossStateType.BulletPattern2);
                data.attackSequence.Add(BossStateType.LaserAttack);
                break;

            case BossPhaseType.陶喆:
                // Phase3（50%~0%）：激光 + 蓄力 + 瞬移
                data.attackSequence.Add(BossStateType.BulletPattern1);
                data.attackSequence.Add(BossStateType.BulletPattern2);
                data.attackSequence.Add(BossStateType.LaserAttack);
                data.attackSequence.Add(BossStateType.ChargeAttack);
                data.attackSequence.Add(BossStateType.TeleportAttack);
                break;
        }
    }


}
