using System.Collections.Generic;                       
using UnityEngine;
using TangmenFramework;

/// <summary>
/// 剧情数据库 —— 汇总所有章节模块的剧情线，提供按 (chapterId, moduleId) 组合键的快速查找。
/// 
/// 【为什么需要数据库层】
/// 如果只有 StoryLineData，每段剧情都是一个独立的 .asset 文件。当项目剧情线数量增多时，
/// 需要一种方式将它们组织起来，用一个入口就能找到任意章节、任意模块的剧情线。
/// StoryDatabase 就是这个"总目录"——它持有所有剧情线的引用列表，并提供 O(1) 的查找效率。
/// 
/// 【查找缓存机制】
/// 内部维护一个 Dictionary<(int chapter, int module), StoryLineData> 作为查找缓存（lookupCache）。
/// 采用惰性构建策略（Lazy Initialization）：
/// - 首次调用 GetStoryLine() 或 GetChapterStoryLines() 时自动调用 EnsureCacheBuilt() 构建缓存
/// - 避免在 OnEnable/OnValidate 等不确定时机构建，减少不必要的性能开销
/// - 缓存一旦构建就不会重复构建，除非手动调用 InvalidateCache() 强制刷新
/// 
/// 【(chapterId, moduleId) 组合键的设计】
/// C# 7.0 的 ValueTuple 语法 (int, int) 作为字典键非常合适：
/// - 两个 int 组成的键天然形成两级索引，无需字符串拼接或嵌套字典
/// - ValueTuple 是值类型，字典内部使用 StructuralComparisons，比较效率高
/// - 代码可读性强：(1, 3) 一目了然表示"第1章第3模块"
/// 
/// 【AB包热更新支持】
/// 作为 ScriptableObject，StoryDatabase 可以直接打入 AB 包。运行时通过 ABResMgr 加载后，
/// 它内部引用的所有 StoryLineData 也会因依赖关系被自动加载（如果它们在同一 AB 包或正确配置了依赖）。
/// 
/// 
/// 【使用方式】
/// 1. 在 Project 窗口右键 → Create → 剧情配置 → 剧情数据库 创建资产
/// 2. 将所有剧情线资产拖入 allStoryLines 列表
/// 3. 场景中或代码中通过 GetStoryLine(chapter, module) 查找
/// 推荐项目中只保留一个 StoryDatabase 实例，作为全局剧情数据入口。
/// </summary>
[CreateAssetMenu(fileName = "StoryDatabase", menuName = "剧情配置/剧情数据库", order = 0)]  // 注册右键菜单"剧情配置/剧情数据库"，order=0 排在最前面
public class StoryDatabase : ScriptableObject           
{
    [Header("========== 剧情总表 ==========")]            
    [Tooltip("所有剧情线资产，拖入此列表即可注册")]        
    public List<StoryLineData> allStoryLines = new List<StoryLineData>();  

    /// <summary>
    /// 查找缓存字典。
    /// 键为 (chapterId, moduleId) 元组，值为对应的剧情线引用。
    /// 使用 Lazy Initialization 模式：初始为 null，首次访问时由 EnsureCacheBuilt() 构建。
    /// </summary>
    private Dictionary<(int chapter, int module), StoryLineData> lookupCache;  

    /// <summary>
    /// 确保查找缓存已构建（惰性初始化）。    
    /// </summary>
    private void EnsureCacheBuilt()                     
    {
        // 如果缓存字典已经存在（之前已构建过）直接返回，避免重复构建浪费性能
        if (lookupCache != null)                         
            return;

        // 创建新的缓存字典实例，键为ValueTuple(int章节, int模块)
        lookupCache = new Dictionary<(int, int), StoryLineData>();
        // 遍历 allStoryLines 列表中的每一条剧情线
        foreach (var line in allStoryLines)              
        {
            // 跳过 null 元素（可能因删除资产产生的空槽位）
            if (line == null)                            
                continue;

            // 用剧情线的 chapterId 和 moduleId 构造 ValueTuple 查找键
            var key = (line.chapterId, line.moduleId);
            // 检查字典中是否已存在相同的键（重复的章节+模块组合）
            if (lookupCache.ContainsKey(key))            
            {
                LogSystem.Error($"StoryDatabase: 章节{line.chapterId} 模块{line.moduleId} 存在重复的剧情线，后者将覆盖前者");
                // 用新值覆盖旧值，确保字典中存储的是最新的剧情线
                lookupCache[key] = line;                 
            }
            else// 键不重复的情况                                         
            {
                // 将 (章节,模块) → 剧情线 映射添加到字典中
                lookupCache.Add(key, line);             
            }
        }
    }

    /// <summary>
    /// 通过章节编号和模块编号查找剧情线。
    /// 
    /// O(1) 时间复杂度。内部首次调用时自动构建缓存。
    /// 如果未找到对应的剧情线，会输出 Warning 日志并返回 null，
    /// 调用方（StoryPlayer）检查 null 后跳过，不会崩溃。
    /// </summary>
    /// <param name="chapterId">章节编号</param>
    /// <param name="moduleId">模块编号</param>
    /// <returns>找到的剧情线，未找到返回 null</returns>
    public StoryLineData GetStoryLine(int chapterId, int moduleId)  
    {
        // 首次调用时自动构建缓存字典（惰性初始化）
        EnsureCacheBuilt();

        // 用入参构造 ValueTuple 查找键
        var key = (chapterId, moduleId);
        // TryGetValue 安全尝试获取值，不会因键不存在而抛异常,找到则返回对应的剧情线引用
        if (lookupCache.TryGetValue(key, out var line))  
            return line;

        LogSystem.Error($"StoryDatabase: 未找到 章节{chapterId} 模块{moduleId} 的剧情线");  
        return null;                                     // 返回 null，调用方需检查此返回值
    }

    /// <summary>
    /// 获取指定章节下的所有模块剧情线，按 moduleId 升序排列。
    /// 
    /// 遍历整个缓存字典筛选出属于目标 chapterId 的所有条目，
    /// 然后按 moduleId 升序排序——这符合剧情推进的自然顺序（模块1 → 模块2 → 模块3...）。
    /// 
    /// 返回的是新创建的 List，修改返回值不会影响数据库内部缓存。
    /// </summary>
    /// <param name="chapterId">章节编号</param>
    /// <returns>该章节下所有剧情线（按 moduleId 升序），无数据时返回空列表</returns>
    public List<StoryLineData> GetChapterStoryLines(int chapterId)  
    {
        // 首次调用时自动构建缓存字典（惰性初始化）
        EnsureCacheBuilt();

        // 创建新列表用于存放筛选结果，不影响缓存内部数据
        var result = new List<StoryLineData>();
        // 遍历缓存字典中的每一个键值对
        foreach (var kvp in lookupCache)                 
        {
            // 判断当前键值对的章节号是否匹配目标章节号,匹配则将该剧情线引用添加到结果列表中
            if (kvp.Key.chapter == chapterId)            
                result.Add(kvp.Value);                   
        }
        // 按 moduleId 升序排序，确保剧情线按模块编号从小到大排列
        result.Sort((a, b) => a.moduleId.CompareTo(b.moduleId));
        // 返回排序后的结果列表
        return result;                                   
    }

    /// <summary>
    /// 清空查找缓存。
    /// 
    /// 当运行时通过代码动态向 allStoryLines 添加或移除剧情线后，
    /// 必须调用此方法使缓存失效，下次查找时会自动重新构建。
    /// 典型场景：从 AB 包热更新加载了新的剧情线，并动态注册到数据库中。
    /// </summary>
    public void InvalidateCache()                        
    {
        // 将缓存引用置为 null，下次查找时 EnsureCacheBuilt() 会重新构建
        lookupCache = null;                              
    }
}
