using UnityEngine;

/// <summary>
/// 演出命令类型枚举 —— 定义剧情演出中所有可用的指令类型。
/// 
/// 【意识流演出的设计理念】
/// 区别于传统线性对白系统，意识流演出强调"实时演算"——画面、音效、文字、镜头
/// 均在运行时动态组合，形成碎片化、情绪化的叙事体验。命令系统是这一理念的核心：
/// 策划通过组合不同类型命令，像搭积木一样编排整段演出。
/// 
/// 【为什么用枚举而非继承】
/// 命令数据使用 [Serializable] 纯数据类而非继承体系，配合枚举区分类型。
/// 这样做的优势：
/// 1. 可以在 ScriptableObject 的 Inspector 中直接编辑，无需自定义 Editor
/// 2. 序列化稳定，不会因重构继承链导致数据丢失
/// 3. 性能友好，switch 分发比虚方法调用更直观
/// </summary>
public enum EPerformanceCommandType
{
    // ======== 文字类 ========
    [InspectorName("💬 对话文字 — Dialogue")]
    Dialogue,

    [InspectorName("💬 浮空文字 — FloatingText")]
    FloatingText,

    // ======== 角色类 ========
    [InspectorName("🧑 角色显隐 — CharacterVisibility")]
    CharacterVisibility,

    [InspectorName("🧑 角色移动 — CharacterMove")]
    CharacterMove,

    [InspectorName("🧑 角色表情 — CharacterExpression")]
    CharacterExpression,

    [InspectorName("🧑 角色震动 — CharacterShake")]
    CharacterShake,

    // ======== 场景/背景类 ========
    [InspectorName("🖼 切换背景 — BackgroundChange")]
    BackgroundChange,

    [InspectorName("🖼 背景滚动 — BackgroundScroll")]
    BackgroundScroll,

    // ======== 镜头类 ========
    [InspectorName("🎥 摄像机震动 — CameraShake")]
    CameraShake,

    [InspectorName("🎥 摄像机缩放 — CameraZoom")]
    CameraZoom,

    [InspectorName("🎥 摄像机平移 — CameraPan")]
    CameraPan,

    [InspectorName("🎥 摄像机旋转 — CameraRotate")]
    CameraRotate,

    [InspectorName("🎥 摄像机复位 — CameraReset")]
    CameraReset,

    // ======== 特效类 ========
    [InspectorName("✨ 颜色滤镜 — ScreenColorFilter")]
    ScreenColorFilter,

    [InspectorName("✨ 淡入淡出 — ScreenFade")]
    ScreenFade,

    [InspectorName("✨ 画面模糊 — ScreenBlur")]
    ScreenBlur,

    [InspectorName("✨ 粒子特效 — ParticleEffect")]
    ParticleEffect,

    [InspectorName("✨ 闪白闪黑 — Flash")]
    Flash,

    // ======== 音频类 ========
    [InspectorName("🎵 播放BGM — PlayBGM")]
    PlayBGM,

    [InspectorName("🎵 停止BGM — StopBGM")]
    StopBGM,

    [InspectorName("🎵 播放音效 — PlaySFX")]
    PlaySFX,

    // ======== 控制类 ========
    [InspectorName("⏱ 等待N秒 — Wait")]
    Wait,

    [InspectorName("⏱ 并行执行 — Parallel")]
    Parallel,

    [InspectorName("⏱ 等待点击 — WaitForInput")]
    WaitForInput,
}
