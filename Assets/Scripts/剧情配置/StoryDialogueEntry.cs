using System;                                     
using UnityEngine;                              

/// <summary>
/// 剧情对话条目 —— 剧情线中的最小单元，代表一句对话。
/// 
/// 【数据结构定位】
/// 这是剧情配置系统的最底层数据结构。一条 StoryLineData（剧情线）由多个 StoryDialogueEntry
/// 按顺序组成，它们之间存在严格的先后关系——播放完上一条，才会播放下一条，形成纯线性叙事流。
/// 
/// 【为什么使用 [Serializable] 而不是 ScriptableObject】
/// StoryDialogueEntry 只是数据载体，不需要作为独立资产存在。标记 [Serializable] 后，
/// 它可以嵌套在 StoryLineData（ScriptableObject）的 List 中，直接在 Inspector 面板中编辑。
/// 这样策划人员在一个窗口中就能看到整条剧情线的所有对话，无需在多个资产文件之间跳转。
/// 
/// 【与 StoryPlayer 的协作方式】
/// StoryPlayer 通过 StoryLineData.GetDialogue(index) 获取当前条目，然后：
/// 1. 将 speakerName + content 通过 OnDialogueUpdated 事件抛给 UI 层显示
/// 2. 检查 autoAdvanceTime：> 0 则启动协程自动推进，= 0 则等待手动调用 NextDialogue()
/// </summary>
[Serializable]                                  
public class StoryDialogueEntry                 
{
    /// <summary>
    /// 说话人名称。
    /// 例如："旁白"、"小明"、"系统提示"。
    /// 如果为空字符串，UI 层可以据此隐藏说话人标签，适合用于纯旁白或场景环境描述。
    /// </summary>
    [Tooltip("说话人名称（留空则无说话人）")]    
    public string speakerName;                  

    /// <summary>
    /// 对话文本内容。
    /// [TextArea(3, 8)] 让 Inspector 中显示一个最小3行、最大8行的多行文本编辑区域，
    /// 便于策划人员编辑较长的对话段落，避免单行输入框带来的阅读困难。
    /// </summary>
    [Tooltip("对话文本内容")]                    
    [TextArea(3, 8)]                            
    public string content;                    

    /// <summary>
    /// 自动播放时间（秒）。
    /// - 设为 0：需要外部手动调用 StoryPlayer.NextDialogue() 才能推进到下一句（交互模式）。
    /// - 设为正数：StoryPlayer 会在显示当前对话后启动协程，等待指定秒数后自动调用 NextDialogue()。
    /// [Range(0, 30)] 将 Inspector 中的滑条/输入限制在 0~30 秒范围内，防止误输入过大数值。
    /// </summary>
    [Tooltip("自动播放时间（秒），0 表示需要手动点击下一句）")] 
    [Range(0f, 30f)]                             
    public float autoAdvanceTime;                 
}
