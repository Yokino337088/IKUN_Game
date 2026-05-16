using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

/// <summary>
/// boss状态枚举
/// </summary>
public enum BossStateType
{
    /// <summary>
    /// 待机状态
    /// </summary>
    Idle,
    /// <summary>
    /// 警戒状态
    /// </summary>
    Alert,


    /// <summary>
    /// 攻击状态 - 分层状态机
    /// </summary>
    Attack,
    /// <summary>
    /// 圆形散射弹幕
    /// </summary>
    BulletPattern1,
    /// <summary>
    /// 螺旋弹幕
    /// </summary>
    BulletPattern2,
    /// <summary>
    /// 直线激光追踪
    /// </summary>
    LaserAttack,
    /// <summary>
    /// 大范围弹幕
    /// </summary>
    ChargeAttack,
    /// <summary>
    /// 瞬移到玩家附近
    /// </summary>
    TeleportAttack,


    /// <summary>
    /// 恢复状态
    /// </summary>
    Recover,
    /// <summary>
    /// 形态切换状态
    /// </summary>
    PhaseChange


}

public enum BossPhaseType
{
    道莉1,
    道莉2,
    陶喆
}