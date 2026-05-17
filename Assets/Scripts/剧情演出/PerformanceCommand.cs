using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 演出命令 —— 剧情演出中的最小执行单元。
/// 
/// 【设计思路】
/// 所有命令共享同一数据结构，通过 type 字段区分具体行为。
/// 不同命令使用不同的参数字段，未使用的字段保持默认值即可。
/// 这样设计的优势是可以放在同一个 List 中序列化到 ScriptableObject。
/// 
/// 【意识流特有的命令设计】
/// - FloatingText：文字在屏幕随机位置浮现后消失，模拟意识碎片
/// - Flash：画面闪白/闪黑，表现心理冲击
/// - ScreenColorFilter：色调滤镜切换，烘托情绪氛围
/// - CameraShake/Zoom：镜头语言增强情感表达
/// </summary>
[Serializable]
public class PerformanceCommand
{
    [Tooltip("命令类型")]
    public EPerformanceCommandType type;

    // ============================================================
    //  通用参数
    // ============================================================

    [Tooltip("持续时间（秒），用于过渡/动画类命令")]
    public float duration;

    [Tooltip("延迟开始时间（秒），从上一命令结束后延迟N秒再执行")]
    public float delay;

    // ============================================================
    //  文字类参数
    // ============================================================

    [Tooltip("说话人名称（Dialogue命令使用）")]
    public string speakerName;

    [Tooltip("对话文本内容（Dialogue/FloatingText命令使用）")]
    [TextArea(3, 6)]
    public string textContent;

    [Tooltip("文字显示位置锚点（0=左 1=中 2=右）")]
    [Range(0, 2)]
    public int textAnchor;

    // ============================================================
    //  角色类参数
    // ============================================================

    [Tooltip("目标角色名称（对应场景中的角色GameObject名）")]
    public string characterName;

    [Tooltip("是否显示角色")]
    public bool characterVisible;

    [Tooltip("角色目标位置（屏幕坐标，0-1归一化）")]
    public Vector2 characterTargetPos;

    [Tooltip("角色新表情/姿态Sprite（资源名）")]
    public string characterSpriteName;

    [Tooltip("角色Sprite所在的AB包名")]
    public string characterSpriteABName;

    [Tooltip("角色震动强度")]
    public float shakeIntensity;

    // ============================================================
    //  场景/背景类参数
    // ============================================================

    [Tooltip("新背景Sprite资源名")]
    public string backgroundSpriteName;

    [Tooltip("背景Sprite所在的AB包名")]
    public string backgroundSpriteABName;

    [Tooltip("背景滚动方向")]
    public Vector2 backgroundScrollDir;

    [Tooltip("背景滚动速度")]
    public float backgroundScrollSpeed;

    // ============================================================
    //  镜头类参数
    // ============================================================

    [Tooltip("摄像机震动强度")]
    public float cameraShakeStrength;

    [Tooltip("摄像机震动频率")]
    public int cameraShakeVibrato;

    [Tooltip("摄像机目标缩放值（1=正常）")]
    public float cameraZoomTarget;

    [Tooltip("摄像机平移目标（世界坐标偏移）")]
    public Vector3 cameraPanOffset;

    [Tooltip("摄像机目标旋转角度（Z轴，表现眩晕/空间扭曲）")]
    public float cameraRotateAngle;

    // ============================================================
    //  特效类参数
    // ============================================================

    [Tooltip("颜色滤镜目标色")]
    public Color screenFilterColor;

    [Tooltip("淡入淡出目标Alpha（0=全透明 1=不透明）")]
    [Range(0f, 1f)]
    public float screenFadeAlpha;

    [Tooltip("模糊强度")]
    [Range(0f, 10f)]
    public float blurStrength;

    [Tooltip("粒子特效预制体资源名")]
    public string particleResName;

    [Tooltip("粒子特效所在AB包名")]
    public string particleABName;

    [Tooltip("粒子特效生成位置（世界坐标）")]
    public Vector3 particleSpawnPos;

    [Tooltip("闪白/闪黑颜色")]
    public Color flashColor;

    // ============================================================
    //  音频类参数
    // ============================================================

    [Tooltip("音频资源名")]
    public string audioResName;

    [Tooltip("音频所在AB包名")]
    public string audioABName;

    [Tooltip("音频音量")]
    [Range(0f, 1f)]
    public float audioVolume;

    // ============================================================
    //  并行组参数
    // ============================================================

    [Tooltip("并行执行的子命令列表（仅Parallel类型使用）")]
    public List<PerformanceCommand> parallelCommands;

    // ============================================================
    //  辅助方法
    // ============================================================

    /// <summary>
    /// 获取实际等待时间（含延迟）。
    /// Wait命令返回duration+delay，其他命令返回delay。
    /// </summary>
    public float GetTotalWaitTime()
    {
        float baseTime = type == EPerformanceCommandType.Wait ? duration : 0f;
        return baseTime + delay;
    }
}
