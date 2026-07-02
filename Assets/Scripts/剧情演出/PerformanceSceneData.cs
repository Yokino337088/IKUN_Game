using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 演出同步模式
/// </summary>
public enum PerformanceSyncMode
{
    /// <summary>顺序执行模式：命令按列表从上到下依次执行，执行完即结束（默认，适合短演出）</summary>
    Sequential,

    /// <summary>BGM驱动模式：一整首歌作为演出时间轴，命令可在指定时间戳触发（适合长剧情演出）</summary>
    BGMDriven,
}

/// <summary>
/// 演出场景数据 —— 定义一幕剧情演出的完整编排。
/// 
/// 【什么是"一幕演出"】
/// 一出完整的意识流演出可以由多个 PerformanceSceneData 串联组成，
/// 每个 Scene 代表一个"情绪段落"或"意识片段"。
/// 例如："回忆闪回" → "内心独白" → "现实回归" 各为一个 Scene。
/// 
/// 【两种演出模式】
/// - Sequential（顺序模式）：命令从上到下逐条执行，执行完即结束。适合短小精悍的意识流片段。
/// - BGMDriven（BGM驱动模式）：以一首歌为时间轴，命令可以绑定到歌曲的特定秒数触发。
///   演出随歌曲开始而开始、随歌曲结束而结束。适合"一首歌 + 一整段剧情"的长演出。
/// 
/// 【与 StoryLineData 的关系】
/// StoryLineData 是纯文本对白序列，适用于传统对话剧情。
/// PerformanceSceneData 是更丰富的视听演出编排，适用于需要实时演算的意识流段落。
/// 两者可以共存——在关键剧情节点切换到演出模式，日常对话使用对白模式。
/// 
/// 【创建方式】
/// Project 窗口右键 → Create → 剧情演出 → 演出场景
/// </summary>
[CreateAssetMenu(fileName = "NewPerformanceScene", menuName = "剧情演出/演出场景", order = 0)]
public class PerformanceSceneData : ScriptableObject
{
    [Header("========== 场景信息 ==========")]

    [Tooltip("场景名称（用于编辑器识别）")]
    public string sceneName;

    [Tooltip("所属章节编号")]
    [Range(1, 20)]
    public int chapterId = 1;

    [Tooltip("所属模块编号（同章节下唯一）")]
    [Range(1, 50)]
    public int moduleId = 1;

    [Header("========== 演出同步模式 ==========")]

    [Tooltip("演出同步模式：\nSequential = 逐条执行命令（默认）\nBGMDriven = 以一首歌为时间轴驱动演出")]
    public PerformanceSyncMode syncMode = PerformanceSyncMode.Sequential;

    [Tooltip("演出主BGM资源名（BGMDriven模式专用，演出将以此曲的时长为总时长）")]
    public string performanceBGMResName;

    [Tooltip("演出主BGM所在AB包名（BGMDriven模式专用）")]
    public string performanceBGMABName;

    [Tooltip("演出主BGM音量（BGMDriven模式专用）")]
    [Range(0f, 1f)]
    public float performanceBGMVolume = 0.8f;

    [Tooltip("演出总时长（秒）。BGMDriven模式默认为0表示跟随BGM长度，手动设置可覆盖")]
    public float performanceDuration;

    [Header("========== 初始场景设置 ==========")]

    [Tooltip("初始背景Sprite资源名")]
    public string initialBackgroundName;

    [Tooltip("初始背景所在AB包名")]
    public string initialBackgroundABName;

    [Tooltip("初始背景音乐资源名（Sequential模式的背景音乐，BGMDriven模式请使用上面的主BGM）")]
    public string initialBGMResName;

    [Tooltip("初始背景音乐所在AB包名")]
    public string initialBGMABName;

    [Tooltip("初始BGM音量")]
    [Range(0f, 1f)]
    public float initialBGMVolume = 0.8f;

    [Header("========== 命令序列 ==========")]

    [Tooltip("命令序列，从上到下依次执行（Parallel命令内的子命令可并行）\nBGMDriven模式下，设置timestamp的命令将在指定时刻触发")]
    public List<PerformanceCommand> commands = new List<PerformanceCommand>();

    // ============================================================
    //  辅助属性
    // ============================================================

    /// <summary>命令总数</summary>
    public int CommandCount => commands?.Count ?? 0;

    /// <summary>获取指定索引的命令</summary>
    public PerformanceCommand GetCommand(int index)
    {
        if (commands == null || index < 0 || index >= commands.Count)
            return null;
        return commands[index];
    }
}
