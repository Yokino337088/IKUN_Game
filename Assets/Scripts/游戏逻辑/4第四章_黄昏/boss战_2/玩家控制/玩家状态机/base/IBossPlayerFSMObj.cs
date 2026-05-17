using System;
using TangmenFramework;
using UnityEngine;

public interface IBossPlayerFSMObj:IAnimancerFSMObj
{
    /// <summary>
    /// 返回与指定 E_BossPlayerStateType 对应的 AnimationClip。
    /// </summary>
    /// <param name="stateType">要检索其动画剪辑的状态类型。</param>
    /// <returns>对应的 AnimationClip；若找不到则返回 null。</returns>
    AnimationClip GetAnimationClip(E_BossPlayerStateType stateType);
    
    /// <summary>
    /// 表示玩家当前的移动方向的二维向量。
    /// </summary>
    Vector2 PlayerMoveDir { get; }

    /// <summary>
    /// 玩家普攻发射子弹
    /// </summary>
    void LaunchBullet();

    /// <summary>
    /// 玩家技能发射导弹
    /// </summary>
    void LaunchMissile();

    /// <summary>
    /// 玩家大招发射弹幕
    /// </summary>
    void SummoningBulletComments();

    /// <summary>
    /// 玩家跳跃
    /// </summary>
    void DoPlayerJump();

    /// <summary>
    /// 开始普攻冷却
    /// </summary>
    void StartAttackCooldown();

    /// <summary>
    /// 开始技能冷却
    /// </summary>
    void StartSkillCooldown();

    /// <summary>
    /// 开始大招冷却
    /// </summary>
    void StartUltimateCooldown();

}
