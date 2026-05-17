using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 演出场景数据 —— 定义一幕剧情演出的完整编排。
/// 
/// 【什么是"一幕演出"】
/// 一出完整的意识流演出可以由多个 PerformanceSceneData 串联组成，
/// 每个 Scene 代表一个"情绪段落"或"意识片段"。
/// 例如："回忆闪回" → "内心独白" → "现实回归" 各为一个 Scene。
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

    [Header("========== 初始场景设置 ==========")]

    [Tooltip("初始背景Sprite资源名")]
    public string initialBackgroundName;

    [Tooltip("初始背景所在AB包名")]
    public string initialBackgroundABName;

    [Tooltip("初始背景音乐资源名")]
    public string initialBGMResName;

    [Tooltip("初始背景音乐所在AB包名")]
    public string initialBGMABName;

    [Tooltip("初始BGM音量")]
    [Range(0f, 1f)]
    public float initialBGMVolume = 0.8f;

    [Header("========== 命令序列 ==========")]

    [Tooltip("命令序列，从上到下依次执行（Parallel命令内的子命令可并行）")]
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
