using System;
using System.Collections.Generic;
using System.Text;
using TangmenFramework;

public class BossPlayerData
{
    private int maxHp = 10;

    private int nowHp;

    public int NowHp => nowHp;

    public BossPlayerData()
    {
        nowHp = maxHp;
    }

    /// <summary>
    /// 玩家受到伤害，扣血
    /// </summary>
    public void GetDamage()
    {
        nowHp--;
        if (nowHp <= 1)
            nowHp = 1;

        // 触发玩家受伤事件，传递当前血量
        EventCenter.Instance.EventTrigger(MyEventTypeString.玩家受伤事件, nowHp);
    }
}
