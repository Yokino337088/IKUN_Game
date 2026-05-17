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
    /// <summary>显示对话文字（支持打字机效果、位置、风格）</summary>
    Dialogue,

    /// <summary>显示浮空文字（飘散、短暂停留后消失，意识流特征）</summary>
    FloatingText,

    // ======== 角色类 ========
    /// <summary>显示/隐藏角色（支持淡入淡出等过渡）</summary>
    CharacterVisibility,

    /// <summary>移动角色到指定位置</summary>
    CharacterMove,

    /// <summary>角色表情/姿态切换（更换Sprite）</summary>
    CharacterExpression,

    /// <summary>角色震动效果（表现情绪激动等）</summary>
    CharacterShake,

    // ======== 场景/背景类 ========
    /// <summary>切换背景图</summary>
    BackgroundChange,

    /// <summary>背景滚动/平移</summary>
    BackgroundScroll,

    // ======== 镜头类 ========
    /// <summary>摄像机震动</summary>
    CameraShake,

    /// <summary>摄像机缩放（拉近拉远）</summary>
    CameraZoom,

    /// <summary>摄像机平移</summary>
    CameraPan,

    /// <summary>摄像机旋转（表现眩晕/意识模糊/空间扭曲）</summary>
    CameraRotate,

    /// <summary>摄像机重置（恢复到演出初始状态）</summary>
    CameraReset,

    // ======== 特效类 ========
    /// <summary>全屏颜色滤镜（情绪色调切换）</summary>
    ScreenColorFilter,

    /// <summary>屏幕淡入/淡出</summary>
    ScreenFade,

    /// <summary>屏幕模糊效果</summary>
    ScreenBlur,

    /// <summary>生成粒子特效</summary>
    ParticleEffect,

    /// <summary>画面闪白/闪黑（冲击效果）</summary>
    Flash,

    // ======== 音频类 ========
    /// <summary>播放背景音乐</summary>
    PlayBGM,

    /// <summary>停止背景音乐</summary>
    StopBGM,

    /// <summary>播放音效</summary>
    PlaySFX,

    // ======== 控制类 ========
    /// <summary>等待指定秒数</summary>
    Wait,

    /// <summary>并行执行组（组内命令同时执行）</summary>
    Parallel,

    /// <summary>用户交互等待（等待点击/按键后继续）</summary>
    WaitForInput,
}
