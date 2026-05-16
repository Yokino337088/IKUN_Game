using System.Collections.Generic;                   
using UnityEngine;                                  

/// <summary>
/// 剧情线 —— 代表某一章节、某一模块下的一段线性对话序列。
/// 
/// 【设计思路 —— 为什么是 ScriptableObject】
/// ScriptableObject 是 Unity 提供的轻量级数据容器，具有以下优势：
/// 1. 以 .asset 文件形式存储在项目中，可以被放入 AB 包进行热更新
/// 2. 支持 Inspector 可视化编辑，策划人员无需写代码即可配置剧情
/// 3. 引用关系自动管理，修改数据后所有引用处自动同步
/// 4. 不依赖场景，可在多个场景间共享同一份剧情数据
/// 
/// 【章节 + 模块 两级组织结构】
/// - chapterId（章节）：大的剧情分段，如第一章=序章、第二章=旅途开始
/// - moduleId（模块）：章节内的子段落，如模块1=进入城镇、模块2=遇见NPC
/// 这种 (chapterId, moduleId) 组合键的设计让 StoryDatabase 可以快速查找到任意一段剧情。
/// 同章节内的模块按 moduleId 升序排列，天然形成线性的模块推进顺序。
/// 
/// 【对话列表的线性语义】
/// dialogues 是一个 List<StoryDialogueEntry>，列表中的元素从上到下排列。
/// StoryPlayer 从索引 0 开始，依次播放到列表末尾，形成严格的线性叙事。
/// 这种设计适合大多数剧情游戏的需求——玩家不能跳过或乱序播剧情文字。
/// 
/// 【创建方式】
/// 在 Project 窗口中右键 → Create → 剧情配置 → 剧情线 即可创建资产文件。
/// </summary>
[CreateAssetMenu(fileName = "NewStoryLine", menuName = "剧情配置/剧情线", order = 1)]  
public class StoryLineData : ScriptableObject          
{
    [Header("========== 剧情定位 ==========")]         

    /// <summary>
    /// 所属章节编号。
    /// 用于在 StoryDatabase 中将多条剧情线归类到同一章节下。
    /// [Range(1, 20)] 限制 Inspector 中可输入的值为 1~20。
    /// </summary>
    [Tooltip("所属章节编号")]                          
    [Range(1, 20)]                                      
    public int chapterId = 1;                           

    /// <summary>
    /// 所属模块编号（同章节下必须唯一）。
    /// chapterId + moduleId 组合形成唯一键，StoryDatabase 依此快速查找。
    /// [Range(1, 50)] 限制 Inspector 中可输入的值为 1~50。
    /// </summary>
    [Tooltip("所属模块编号（同章节下唯一）")]            
    [Range(1, 50)]                                     
    public int moduleId = 1;                           

    /// <summary>
    /// 模块名称，仅在编辑器内用于快速识别，不影响运行时逻辑。
    /// 例如："初入迷宫"、"遇见向导"、"BOSS战前对话"。
    /// </summary>
    [Tooltip("模块名称（用于编辑器内识别）")]            
    public string moduleName;                           

    [Header("========== 对话内容 ==========")]          

    /// <summary>
    /// 对话列表，按从上到下的顺序依次播放。
    /// 策划人员在此添加 StoryDialogueEntry，每一条代表一句对话。
    /// </summary>
    [Tooltip("对话列表，按顺序从上到下依次播放")]        // Inspector 鼠标悬停提示
    public List<StoryDialogueEntry> dialogues = new List<StoryDialogueEntry>(); 

    /// <summary>
    /// 获取对话总数。
    /// 使用空条件运算符 (?.) 和空合并运算符 (??)：如果 dialogues 为 null 则返回 0，
    /// 避免因未初始化列表而引发的 NullReferenceException。
    /// 这是一个表达式体属性（=>），等价于只读 getter。
    /// </summary>
    public int DialogueCount => dialogues?.Count ?? 0;  

    /// <summary>
    /// 获取指定索引的对话条目。
    /// 采用防御式编程：对 dialogues 为 null、索引为负数、索引越界三种情况均返回 null，
    /// 调用方（StoryPlayer）在收到 null 时会跳过当前对话，不会崩溃。
    /// </summary>
    /// <param name="index">对话索引，从 0 开始</param>
    /// <returns>对应的 StoryDialogueEntry，越界或数据为空时返回 null</returns>
    public StoryDialogueEntry GetDialogue(int index)    
    {
        // 三重安全检查：列表为null、索引为负数、索引越界
        if (dialogues == null || index < 0 || index >= dialogues.Count) 
            return null;
        // 索引有效则返回对应位置的对话条目
        return dialogues[index];                        
    }
}
