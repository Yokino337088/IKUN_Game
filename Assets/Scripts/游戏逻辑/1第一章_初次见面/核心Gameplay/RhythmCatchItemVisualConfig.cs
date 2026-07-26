using System;
using UnityEngine;

/// <summary>
/// 掉落物视觉配置 —— 每种箱子类型映射到对应的 Sprite 图片。
/// 挂在 RhythmCatchFallingItem 的 visualConfig 字段上，所有掉落物共享同一份配置。
/// 美术只需替换这里的 Sprite 即可全局生效，无需逐个修改预制体。
/// </summary>
[CreateAssetMenu(fileName = "掉落物视觉配置_第一章", menuName = "IKUN Game/第一章/掉落物视觉配置")]
public class RhythmCatchItemVisualConfig : ScriptableObject
{
    [Header("箱子图片（按类型映射）")]
    [Tooltip("普通木箱")]
    public Sprite normalSprite;
    [Tooltip("金色宝箱")]
    public Sprite goldSprite;
    [Tooltip("小宝石")]
    public Sprite gemSprite;
    [Tooltip("炸弹箱")]
    public Sprite bombSprite;
    [Tooltip("磁铁箱")]
    public Sprite magnetSprite;
    [Tooltip("护盾箱")]
    public Sprite shieldSprite;

    /// <summary>
    /// 根据掉落物类型返回对应的 Sprite，未配置则返回 null。
    /// </summary>
    public Sprite GetSprite(RhythmCatchItemType type)
    {
        switch (type)
        {
            case RhythmCatchItemType.Normal:  
                return normalSprite;
            case RhythmCatchItemType.Gold:    
                return goldSprite;
            case RhythmCatchItemType.Gem:     
                return gemSprite;
            case RhythmCatchItemType.Bomb:    
                return bombSprite;
            case RhythmCatchItemType.Magnet:  
                return magnetSprite;
            case RhythmCatchItemType.Shield:  
                return shieldSprite;
            default:                          
                return normalSprite;
        }
    }
}
