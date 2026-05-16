using Animancer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using DG.Tweening;

/// <summary>
/// boss行为接口
/// </summary>
public interface IBossFSMObj : IAnimancerFSMObj
{
    /// <summary>
    /// 获取Boss控制器
    /// </summary>
    BossController GetBossController();

    /// <summary>
    /// 获取boss数据
    /// </summary>
    /// <returns></returns>
    BossData GetBossData();

    /// <summary>
    /// 获取当前的阶段
    /// </summary>
    /// <returns></returns>
    BossPhaseType CheckNowPhase();

    /// <summary>
    /// 开始浮空动画
    /// </summary>
    void StartFloatingAnimation();

    /// <summary>
    /// 停止浮空动画
    /// </summary>
    void StopFloatingAnimation();

    /// <summary>
    /// 检测玩家是否在攻击范围内
    /// </summary>
    /// <returns></returns>
    bool IsPlayerInAttackRange();

    /// <summary>
    /// 返回初始位置
    /// </summary>
    void ReturnToInitialPosition();

    /// <summary>
    /// 攻击状态开始时的动画
    /// </summary>
    void DoAttackStartAnimation();

    /// <summary>
    /// 弹幕攻击1开始时的动画
    /// </summary>
    void DoBullet1Animation();

    /// <summary>
    /// 弹幕攻击2的移动动画（向玩家方向往复移动）
    /// </summary>
    void DoBullet2Animation();

    /// <summary>
    /// 蓄力攻击的移动动画（移到边缘位置，完成后执行回调）
    /// </summary>
    void DoChargeAnimation(Action onComplete);

    /// <summary>
    /// 激光攻击的移动动画（移到玩家上方，完成后执行回调）
    /// </summary>
    void DoLaserAnimation(Action onComplete);

    /// <summary>
    /// 激光攻击的水平移动逻辑（在 Update 中每帧调用）
    /// </summary>
    void DoLaserHorizontalMove();

    

    /// <summary>
    /// 蓄力攻击DOTween动画效果（缩放脉冲）
    /// </summary>
    void DoChargeAnimationEffect(float duration);

    /// <summary>
    /// 停止蓄力动画效果并恢复缩放
    /// </summary>
    void StopChargeAnimationEffect();

    /// <summary>
    /// 激光攻击DOTween动画效果（震动）
    /// </summary>
    void DoLaserAnimationEffect();

    /// <summary>
    /// 停止激光动画效果
    /// </summary>
    void StopLaserAnimationEffect();

    /// <summary>
    /// 瞬移到指定位置（使用DOTween快速移动 + 缩放闪现效果）
    /// </summary>
    /// <param name="target">目标位置</param>
    void TeleportTo(Vector3 target);

    /// <summary>
    /// 是否正在移动
    /// </summary>
    bool IsMoving { get; }

    /// <summary>
    /// 获取动画切片
    /// </summary>
    /// <returns></returns>
    AnimationClip GetAnimationClip(BossPhaseType bossPhaseType);


}
