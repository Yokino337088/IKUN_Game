using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.VisualScripting;
using TangmenFramework;

/// <summary>
/// boss的运行时数据
/// </summary>
public class BossData
{
    /// <summary>
    /// 当前阶段
    /// </summary>
    public BossPhaseType nowPhase;

    //当前血量
    private int nowHp;

    //最大血量
    private const int maxHp = 10000;

    /// <summary>>
    /// 当前攻击序列（按阶段构建的攻击模式列表）
    /// 持久化存储，不因状态切换而丢失
    /// </summary>
    public List<BossStateType> attackSequence;

    /// <summary>
    /// 当前攻击在序列中的索引位置
    /// -1 表示未开始或已结束一轮
    /// </summary>
    public int currentAttackIndex = -1;

    /// <summary>
    /// 是否正在进行一轮攻击循环
    /// true 表示一轮攻击还没打完，Recover后应回到Attack继续
    /// false 表示一轮攻击已全部完成，Recover后应回到Idle
    /// </summary>
    public bool isInAttackCycle;

    public void Update()
    {

    }

    public BossData()
    {
        nowHp = maxHp;
    }

    /// <summary>
    /// 攻击索引+1
    /// </summary>
    public void AddAttackIndex()
    {
        currentAttackIndex++;
    }

    /// <summary>
    /// 重置攻击索引
    /// </summary>
    public void ResetAttackIndex()
    {
        currentAttackIndex = -1;
    }

    /// <summary>
    /// boss扣血方法
    /// </summary>
    /// <param name="dam"></param>
    public void GetDamage(int dam)
    {
        //扣血
        nowHp -= dam;
        //计算血量比例
        float rate = nowHp / maxHp;
        //如果血量为0，就触发死亡事件
        if (nowHp <= 0)
        {
            nowHp = 0;
            EventCenter.Instance.EventTrigger(MyEventTypeString.boss死亡事件);
        }
        //触发扣血事件
        EventCenter.Instance.EventTrigger(MyEventTypeString.boss扣血事件, nowHp);

        //根据比例设置对应的枚举
        if (rate >= 0.8f && rate <= 1)
            nowPhase = BossPhaseType.道莉1;
        else if (rate >= 0.5f && rate < 0.8f)
            nowPhase = BossPhaseType.道莉2;
        else if (rate >= 0 && rate < 0.5f)
            nowPhase = BossPhaseType.陶喆;
    }
}