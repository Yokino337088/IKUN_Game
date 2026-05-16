using System;                                         
using System.Collections;                               
using Cysharp.Threading.Tasks;                         
using TangmenFramework;                                 
using UnityEngine;                                      

/// <summary>
/// 剧情播放器 —— 运行时 MonoBehaviour 组件，负责线性播放 StoryLineData 中的对话序列。
/// 
/// 【功能概述】
/// 1. 挂载到任意 GameObject 上即可使用
/// 2. 支持三种数据获取模式：直接引用、数据库查找、AB包异步加载
/// 3. 按 dialogues 列表的顺序从索引 0 依次播放到末尾（线性不可逆）
/// 4. 支持手动推进（调用 NextDialogue）和自动推进（autoAdvanceTime > 0）
/// 5. 通过 C# Action 委托向 UI 层或其他系统抛出状态变化事件（支持 += / -= 动态注册注销）
/// 
/// 【核心状态机】
/// 播放器的生命周期遵循以下状态转换：
/// 
///   Idle（未播放）
///     │ Play() / PlayStoryLine() / PlayFromAB() ...
///     ▼
///   Playing（播放中，currentStoryLine != null）
///     │ 每句对话触发 OnDialogueUpdated
///     │ 用户点击 → NextDialogue() → index++
///     │ 或 autoAdvanceTime > 0 → 协程倒数 → NextDialogue()
///     │
///     ├── index 未超 → 继续 Playing，显示下一句
///     ├── index 超出 → 触发 OnStoryEnded → 回到 Idle
///     └── Stop() 被调用 → 触发 OnStoryInterrupted → 回到 Idle
/// 
/// 【为什么使用 C# Action 委托而不是 UnityEvent】
/// Action 委托是 C# 原生的事件机制，通过 += 注册、-= 注销，所有绑定在代码中完成。
/// 相比 UnityEvent 的优势：
/// 1. 代码中动态注册/注销，灵活控制事件的生命周期
/// 2. 不存在 Inspector 拖拽绑定丢失的问题（Inspector 引用经常因资产移动而断裂）
/// 3. 性能开销更小，Action 调用比 UnityEvent.Invoke() 轻量
/// 4. 支持 Lambda 表达式内联注册，代码更简洁
/// 
/// 【协程自动播放机制】
/// 当 StoryDialogueEntry.autoAdvanceTime > 0 时：
/// 1. ShowCurrentDialogue() 启动 AutoAdvanceRoutine 协程
/// 2. 协程等待 autoAdvanceTime 秒后自动调用 NextDialogue()
/// 3. 如果用户在等待期间手动调用了 NextDialogue()，StopAutoAdvance() 会取消协程
/// 4. OnDisable() 时也会停止协程，防止对象销毁后协程继续执行导致异常
/// </summary>
public class StoryPlayer : MonoBehaviour                 // 继承 MonoBehaviour，可以挂载到场景中的 GameObject 上
{
    [Header("========== 剧本引用模式 ==========")]       // Inspector 中以加粗样式显示大标题分隔

    /// <summary>
    /// 数据引用模式开关。
    /// - true：使用直接引用模式，将 StoryLineData 资拖拽到 directStoryLine 字段
    /// - false：使用数据库查找模式，通过 storyDatabase + targetChapterId + targetModuleId 查找
    /// 注意：如果启用了 AB 包加载，PlayFromAB() 方法会绕过此开关，直接从 AB 包加载。
    /// </summary>
    [Tooltip("引用模式：勾选则直接用下方剧情线资产；不勾选则通过数据库+章节/模块号查找")]  // Inspector 鼠标悬停提示
    public bool useDirectReference = true;              

    /// <summary>
    /// 直接引用的剧情线资产（ScriptableObject）。
    /// 在 Inspector 中直接拖入一个 .asset 文件即可。
    /// 仅在 useDirectReference = true 且调用 Play() 时使用。
    /// </summary>
    [Tooltip("直接引用的剧情线资产")]                   
    public StoryLineData directStoryLine;               

    /// <summary>
    /// 剧情数据库引用（查找模式使用）。
    /// 数据库中汇总了所有章节模块的剧情线，通过 (chapterId, moduleId) 键来查找。
    /// 仅在 useDirectReference = false 且调用 Play() 时使用。
    /// </summary>
    [Tooltip("剧情数据库（查找模式使用）")]              
    public StoryDatabase storyDatabase;                  

    /// <summary>
    /// 要播放的章节号（查找模式使用）。
    /// 与 targetModuleId 组合形成查找键。
    /// </summary>
    [Tooltip("要播放的章节号（查找模式使用）")]           
    public int targetChapterId = 1;                      

    /// <summary>
    /// 要播放的模块号（查找模式使用）。
    /// 与 targetChapterId 组合形成查找键。
    /// </summary>
    [Tooltip("要播放的模块号（查找模式使用）")]           
    public int targetModuleId = 1;                       

    [Header("========== AB包加载配置（可选）==========")]  

    /// <summary>
    /// 剧情线所在的 AB 包名称。
    /// 对应 ABResMgr 中的 abName 参数，例如 AssetBundleName.第四章物体包。
    /// 调用 PlayStoryLineFromAB(storyLineABName, storyLineResName) 或 PlayFromAB() 时使用。
    /// </summary>
    [Tooltip("剧情线所在的AB包名称")]                    
    public string storyLineABName;                       

    /// <summary>
    /// 剧情线资源在 AB 包中的名称（即 .asset 文件的文件名，不含扩展名）。
    /// 对应 ABResMgr 中的 resName 参数，例如 "Chapter1_Module1"。
    /// </summary>
    [Tooltip("剧情线资源在AB包中的名称")]                 
    public string storyLineResName;                      

    /// <summary>
    /// 剧情数据库所在的 AB 包名称。
    /// 调用 PlayFromABWithDatabase(databaseABName, databaseResName, ...) 时使用。
    /// </summary>
    [Tooltip("剧情数据库所在的AB包名称（数据库模式使用）")]  
    public string databaseABName;                        

    /// <summary>
    /// 剧情数据库资源在 AB 包中的名称。
    /// </summary>
    [Tooltip("剧情数据库资源在AB包中的名称")]             
    public string databaseResName;                       

    [Header("========== 播放状态（只读，运行时查看）==========")]  

    /// <summary>
    /// 当前正在播放的剧情线引用。
    /// null 表示未在播放任何剧情。
    /// [SerializeField] + private 使其在 Inspector 中可见但不可被外部代码修改。
    /// </summary>
    [Tooltip("当前正在播放的剧情线")]                     
    [SerializeField] 
    private StoryLineData currentStoryLine;  

    /// <summary>
    /// 当前播放到的对话索引（从 0 开始）。
    /// 指向 currentStoryLine.dialogues 中的位置。
    /// 当 index >= DialogueCount 时表示播放完毕。
    /// </summary>
    [Tooltip("当前对话索引")]                             
    [SerializeField] 
    private int currentIndex;           

    /// <summary>
    /// 是否正处于自动播放倒计时中。
    /// 当 autoAdvanceTime > 0 的对话正在等待时设为 true，
    /// 倒计时结束或用户手动跳过后设为 false。
    /// </summary>
    [Tooltip("是否正在等待自动播放倒计时")]               
    [SerializeField] 
    private bool isWaitingAutoAdvance;  

    [Header("========== 事件回调 ==========")]           

    /// <summary>
    /// 当切换到新的一句对话时触发。
    /// 参数1：int —— 当前对话索引（从 0 开始）
    /// 参数2：string —— 说话人名称（可能为空）
    /// 参数3：string —— 对话文本内容
    /// 
    /// 使用方式（外部代码中动态注册/注销）：
    /// <code>
    /// storyPlayer.OnDialogueUpdated += OnDialogueChanged;   // 注册
    /// storyPlayer.OnDialogueUpdated -= OnDialogueChanged;   // 注销
    /// storyPlayer.OnDialogueUpdated += (idx, speaker, text) => { ... };  // Lambda 注册
    /// </code>
    /// </summary>
    public Action<int, string, string> OnDialogueUpdated;  

    /// <summary>
    /// 当剧情线自然播放完毕（播完最后一条对话）时触发。
    /// 注意：Stop() 中断播放不会触发此事件，只有正常播完才会触发。
    /// 
    /// 使用方式：
    /// <code>
    /// storyPlayer.OnStoryEnded += HandleStoryEnd;
    /// storyPlayer.OnStoryEnded -= HandleStoryEnd;
    /// </code>
    /// </summary>
    public Action OnStoryEnded;                          

    /// <summary>
    /// 当剧情开始播放时触发。
    /// 参数为即将播放的 StoryLineData，可用于记录当前剧情线信息。
    /// 
    /// 使用方式：
    /// <code>
    /// storyPlayer.OnStoryStarted += (line) => Debug.Log($"开始播放: {line.moduleName}");
    /// storyPlayer.OnStoryStarted -= ...;
    /// </code>
    /// </summary>
    public Action<StoryLineData> OnStoryStarted;         

    /// <summary>
    /// 当播放被手动中断（调用 Stop()）时触发。
    /// 与 OnStoryEnded 互斥——一条剧情线不会同时触发两者。
    /// 
    /// 使用方式：
    /// <code>
    /// storyPlayer.OnStoryInterrupted += HandleInterrupted;
    /// storyPlayer.OnStoryInterrupted -= HandleInterrupted;
    /// </code>
    /// </summary>
    public Action OnStoryInterrupted;                    

    /// <summary>
    /// 当前是否正在播放剧情。
    /// IsPlaying 为 true 时，currentStoryLine 不为 null，且 currentIndex 在有效范围内或刚好越界。
    /// </summary>
    public bool IsPlaying => currentStoryLine != null;   

    /// <summary>
    /// 当前剧情是否已播放完毕。
    /// 判定条件：有剧情线在播 且 当前索引已到达或超过对话总数。
    /// </summary>
    public bool IsFinished => currentStoryLine != null && currentIndex >= currentStoryLine.DialogueCount;  

    /// <summary>
    /// 当前对话索引（从 0 开始）。
    /// 外部可通过此属性获取当前播放进度。
    /// </summary>
    public int CurrentIndex => currentIndex;             

    /// <summary>
    /// 当前剧情线的对话总数。
    /// 无剧情线时返回 0，可用于计算播放进度百分比。
    /// </summary>
    public int TotalDialogueCount => currentStoryLine != null ? currentStoryLine.DialogueCount : 0;  

    /// <summary>
    /// 自动播放协程引用。
    /// 存储当前正在运行的 AutoAdvanceRoutine，用于在需要时取消（StopCoroutine）。
    /// 同一时间最多只有一个自动播放协程在运行。
    /// </summary>
    private Coroutine autoAdvanceRoutine;               

    /// <summary>
    /// 当挂载本组件的 GameObject 被禁用时，自动停止自动播放协程。
    /// 防止对象被禁用/销毁后协程继续运行导致的 MissingReferenceException。
    /// </summary>
    private void OnDisable()                             
    {
        StopAutoAdvance();                               
    }

    
    // ============================================================
    //  同步播放 API（Inspector 直接引用 + 数据库查找模式）
    //  这些方法不涉及异步加载，适用于剧情数据已在内存中的场景
    // ============================================================

    /// <summary>
    /// 按 Inspector 中配置的引用模式开始播放。  
    /// </summary>
    public void Play()                                   
    {
        // 判断是否使用直接引用模式
        if (useDirectReference)                          
        {
            // 直接引用模式：传入 Inspector 中拖入的剧情线资产开始播放
            PlayStoryLine(directStoryLine);              
        }
        else// 数据库查找模式                                             
        {
            // 数据库查找模式：通过章节号和模块号从数据库查找并播放
            PlayByLookup(targetChapterId, targetModuleId); 
        }
    }

    /// <summary>
    /// 通过章节号和模块号从数据库查找并播放剧情线。   
    /// </summary>
    /// <param name="chapterId">章节编号</param>
    /// <param name="moduleId">模块编号</param>
    public void PlayByLookup(int chapterId, int moduleId) 
    {
        // 检查数据库引用是否为空（未在 Inspector 中设置）
        if (storyDatabase == null)                       
        {
            LogSystem.Debug("StoryPlayer: storyDatabase 未设置！");  
            return;                                      
        }

        // 调用数据库的查找方法，按 (章节, 模块) 查找剧情线
        StoryLineData line = storyDatabase.GetStoryLine(chapterId, moduleId);
        // 如果数据库未找到对应的剧情线，返回了 null
        if (line == null)                                
        {
            LogSystem.Error($"StoryPlayer: 未找到 章节{chapterId} 模块{moduleId} 的剧情线！");  
            return;                                      
        }

        // 查找成功，开始播放找到的剧情线
        PlayStoryLine(line);                             
    }

    /// <summary>
    /// 播放指定的剧情线（核心启动方法，所有播放入口最终都会调用此方法）。    
    /// </summary>
    /// <param name="storyLine">要播放的剧情线资产</param>
    public void PlayStoryLine(StoryLineData storyLine)  
    {
        // 防御性检查：传入的剧情线为空
        if (storyLine == null)                           
        {
            LogSystem.Error("StoryPlayer: 剧情线为空！");  
            return;                                      
        }

        // 防御性检查：剧情线中的对话条数为 0（空剧情线）
        if (storyLine.DialogueCount == 0)                
        {
            LogSystem.Error("StoryPlayer: 剧情线中没有对话条目！");  
            return;                                      
        }

        // 停止上一个剧情的自动播放协程（如果正在运行）
        StopAutoAdvance();
        // 设置当前剧情线为传入的参数，进入 Playing 状态
        currentStoryLine = storyLine;
        // 重置对话索引为 0，从第一条对话开始播放
        currentIndex = 0;
        // 触发剧情开始事件，使用 null 条件运算符确保事件不为 null 时才调用
        OnStoryStarted?.Invoke(currentStoryLine);
        // 显示索引 0 处的第一条对话
        ShowCurrentDialogue();                           
    }

    /// <summary>
    /// 播放下一条对话。    
    /// 通常由 UI 按钮的 onClick 事件调用，或由自动播放协程在倒计时结束后调用。
    /// </summary>
    public void NextDialogue()                           
    {
        // 如果未在播放状态 或 已播放完毕
        if (!IsPlaying || IsFinished)                    
            return;

        // 停止当前自动播放协程（用户手动跳过/协程触发时取消现有协程）
        StopAutoAdvance();
        // 对话索引自增 1，指向下一条对话
        currentIndex++;

        // 判断是否已播放到最后一条之后
        // 如果当前索引已大于等于对话总数（越界了）
        if (currentIndex >= currentStoryLine.DialogueCount)  
        {
            // 清空当前剧情线引用，回到 Idle 状态
            currentStoryLine = null;
            // 触发剧情结束事件（null 条件运算符安全调用）
            OnStoryEnded?.Invoke();                     
            return;                                      
        }

        // 索引在有效范围内，显示新的当前对话
        ShowCurrentDialogue();                           
    }

    /// <summary>
    /// 跳转到指定索引的对话。    
    /// 适用于快进、回溯等特殊需求。
    /// </summary>
    /// <param name="index">目标对话索引（从 0 开始）</param>
    public void SkipTo(int index)                        
    {
        // 如果未在播放状态
        if (!IsPlaying)                                 
            return;

        // 如果传入的索引为负数（无效输入）自动修正为 0，跳转到第一条对话
        if (index < 0)                                   
            index = 0;

        // 如果传入的索引超出对话总数（跳过了最后一条）
        if (index >= currentStoryLine.DialogueCount)    
        {
            // 清空当前剧情线，视为播放完毕
            currentStoryLine = null;
            // 触发剧情结束事件
            OnStoryEnded?.Invoke();                      
            return;                                     
        }

        // 停止自动播放协程（防止跳转后旧协程还在运行）
        StopAutoAdvance();
        // 将当前索引设为跳转目标索引
        currentIndex = index;
        // 显示新索引位置的对话
        ShowCurrentDialogue();                           
    }

    /// <summary>
    /// 获取当前正在显示的对话条目。
    /// 
    /// 供外部（如 UI 层）直接读取当前对话数据，而不依赖事件回调。
    /// 当需要在 Start() 中主动查询当前对话而非被动等待事件时使用。
    /// </summary>
    /// <returns>当前对话条目，无剧情线时返回 null</returns>
    public StoryDialogueEntry GetCurrentDialogue()       
    {
        // 如果当前没有在播放任何剧情线
        if (currentStoryLine == null)                   
            return null;
        // 委托 StoryLineData 的 GetDialogue 方法获取当前索引的对话条目
        return currentStoryLine.GetDialogue(currentIndex);  
    }

    /// <summary>
    /// 停止当前播放（手动中断）。
    /// 
    /// 与自然播完不同：Stop() 触发 OnStoryInterrupted 事件而非 OnStoryEnded。
    /// 适用于场景切换、玩家主动跳过剧情等场景。
    /// 
    /// 调用后会：
    /// 1. 停止自动播放协程
    /// 2. 触发中断事件
    /// 3. 将 currentStoryLine 置为 null（回到 Idle 状态）
    /// </summary>
    public void Stop()                                   
    {
        // 如果未在播放状态
        if (!IsPlaying)                                  
            return;

        // 停止自动播放协程
        StopAutoAdvance();
        // 触发中断事件（null 条件运算符安全调用），与 OnStoryEnded 互斥
        OnStoryInterrupted?.Invoke();
        // 清空当前剧情线引用，回到 Idle 状态
        currentStoryLine = null;                         
    }

    // ============================================================
    //  内部播放逻辑
    // ============================================================

    /// <summary>
    /// 显示当前索引对应的对话条目（核心显示方法）。
    /// 
    /// 执行步骤：
    /// 1. 从 currentStoryLine 获取 currentIndex 位置的对话条目
    /// 2. 通过 OnDialogueUpdated 事件将对话数据发送给 UI 层
    /// 3. 如果该条目的 autoAdvanceTime > 0，启动自动播放协程
    /// 
    /// 这是一个私有方法，只由 PlayStoryLine、NextDialogue、SkipTo 内部调用。
    /// 这样设计保证了数据流转的单向性——外部无法直接"显示"对话，只能通过推进索引来触发。
    /// </summary>
    private void ShowCurrentDialogue()                   
    {
        // 获取当前索引位置的对话条目数据
        StoryDialogueEntry entry = currentStoryLine.GetDialogue(currentIndex);
        // 防御性检查：如果条目为 null（数据异常或越界）
        if (entry == null)                               
            return;

        // 将 (索引, 说话人, 内容) 递给 UI 层
        // 触发对话更新事件，传递索引、说话人、内容三个参数
        OnDialogueUpdated?.Invoke(currentIndex, entry.speakerName, entry.content);

        // 如果配置了自动播放时间，启动协程倒计时
        // 判断 autoAdvanceTime 是否大于 0（是否需要自动推进）
        if (entry.autoAdvanceTime > 0f)                  
        {
            // 启动自动播放协程并保存引用，用于后续取消
            autoAdvanceRoutine = StartCoroutine(AutoAdvanceRoutine(entry.autoAdvanceTime));  
        }
    }

    /// <summary>
    /// 自动播放倒计时协程。        
    /// 注意：WaitForSeconds 受 Time.timeScale 影响。如果游戏暂停（Time.timeScale=0），
    /// 协程也会暂停等待。如需无视 timeScale 的等待，可改用 WaitForSecondsRealtime。
    /// </summary>
    /// <param name="delay">等待秒数</param>
    private IEnumerator AutoAdvanceRoutine(float delay)  // 协程方法：等待指定秒数后自动推进到下一句对话，返回 IEnumerator
    {
        // 标记进入自动播放等待状态
        isWaitingAutoAdvance = true;
        // 协程挂起，等待 delay 秒后继续执行后续代码
        yield return new WaitForSeconds(delay);
        // 等待结束，标记退出自动播放等待状态
        isWaitingAutoAdvance = false;
        // 倒计时结束，自动推进到下一句对话
        NextDialogue();                                 
    }

    /// <summary>
    /// 停止自动播放协程。       
    /// </summary>
    private void StopAutoAdvance()                      
    {
        // 私有方法：安全停止自动播放协程
        if (autoAdvanceRoutine != null)                  
        {
            // 取消协程的执行，yield return 之后的代码不会再执行
            StopCoroutine(autoAdvanceRoutine);
            // 将协程引用置为 null，防止重复取消同一个协程
            autoAdvanceRoutine = null;                   
        }
        // 将等待标记置为 false，确保状态一致
        isWaitingAutoAdvance = false;                    
    }

    // ============================================================
    //  AB 包异步加载 API
    //  通过 ABResMgr 从 AssetBundle 中加载剧情配置数据
    //  支持热更新——策划修改剧情后更新 AB 包即可，无需重新打包游戏主体
    // ============================================================

    /// <summary>
    /// 从 AB 包异步加载一条剧情线资产。
    /// 
    /// 底层调用 ABResMgr.Instance.LoadResAsync<StoryLineData>(abName, resName)，
    /// 该方法在编辑器模式下通过 AssetDatabase 加载，在真机模式下通过 AB 包加载。
    /// 
    /// 使用示例：
    /// <code>
    /// var line = await player.LoadStoryLineFromAB(AssetBundleName.第四章物体包, "Chapter1_Module1");
    /// if (line != null) player.PlayStoryLine(line);
    /// </code>
    /// </summary>
    /// <param name="abName">AB 包名称，如 AssetBundleName.第四章物体包</param>
    /// <param name="resName">资源名称（.asset 文件名），如 "Chapter1_Module1"</param>
    /// <returns>加载成功返回剧情线资产，失败返回 null</returns>
    public async UniTask<StoryLineData> LoadStoryLineFromAB(string abName, string resName)  
    {
        // 参数校验：检查 AB 包名或资源名是否为空或 null
        if (string.IsNullOrEmpty(abName) || string.IsNullOrEmpty(resName))  
        {
            LogSystem.Error("StoryPlayer: AB包名或资源名为空！");  
            return null;                                 
        }

        // 调用 ABResMgr 单例的异步加载方法，await 等待加载完成
        StoryLineData storyLine = await ABResMgr.Instance.LoadResAsync<StoryLineData>(abName, resName);  
        if (storyLine == null)                           
        {
            LogSystem.Error($"StoryPlayer: 从AB包加载剧情线失败！AB包:{abName} 资源:{resName}");  
        }
        
        return storyLine;
    }

    /// <summary>
    /// 从 AB 包异步加载剧情数据库资产。
    /// 
    /// 数据库内部引用了多条剧情线，这些剧情线如果配置在同一 AB 包中（或正确设置了依赖关系），
    /// Unity 的 AB 包依赖系统会自动加载它们，无需手动逐条加载。
    /// 
    /// 使用示例：
    /// <code>
    /// var db = await player.LoadDatabaseFromAB(AssetBundleName.第四章物体包, "StoryDatabase");
    /// if (db != null) { player.storyDatabase = db; player.PlayByLookup(1, 1); }
    /// </code>
    /// </summary>
    /// <param name="abName">AB 包名称</param>
    /// <param name="resName">资源名称（.asset 文件名），如 "StoryDatabase"</param>
    /// <returns>加载成功返回数据库资产，失败返回 null</returns>
    public async UniTask<StoryDatabase> LoadDatabaseFromAB(string abName, string resName)  
    {
        // 参数校验：检查 AB 包名或资源名是否为空
        if (string.IsNullOrEmpty(abName) || string.IsNullOrEmpty(resName))  
        {
            LogSystem.Error("StoryPlayer: AB包名或资源名为空！");  
            return null;                                 
        }

        // 调用 ABResMgr 单例异步加载数据库资产
        StoryDatabase database = await ABResMgr.Instance.LoadResAsync<StoryDatabase>(abName, resName);  
        if (database == null)                            
        {
            LogSystem.Error($"StoryPlayer: 从AB包加载剧情数据库失败！AB包:{abName} 资源:{resName}");  
        }
        return database;                                 
    }

    /// <summary>
    /// 从 AB 包异步加载剧情线并直接开始播放。
    /// 
    /// 这是一个便捷方法，将"加载"和"播放"合并为一步。
    /// 等价于：await LoadStoryLineFromAB(abName, resName) 然后调用 PlayStoryLine()。
    /// 
    /// 使用示例：
    /// <code>
    /// await player.PlayStoryLineFromAB(AssetBundleName.第四章物体包, "Chapter2_Module3");
    /// </code>
    /// </summary>
    /// <param name="abName">AB 包名称</param>
    /// <param name="resName">资源名称</param>
    public async UniTask PlayStoryLineFromAB(string abName, string resName)  // 异步方法：加载剧情线并立即播放，一步完成
    {
        // 先异步加载剧情线资产
        StoryLineData storyLine = await LoadStoryLineFromAB(abName, resName);  
        if (storyLine != null)                           
        {
            PlayStoryLine(storyLine);                    
        }
    }

    /// <summary>
    /// 使用 Inspector 中配置的 AB 包参数异步加载并播放剧情线。
    /// 
    /// 直接读取 storyLineABName 和 storyLineResName 字段的值，
    /// 适用于已经将这些值配置在 Inspector 中的场景。
    /// 
    /// 使用示例：
    /// <code>
    /// await player.PlayFromAB();
    /// </code>
    /// </summary>
    public async UniTask PlayFromAB()                    
    {
        await PlayStoryLineFromAB(storyLineABName, storyLineResName);  
    }

    /// <summary>
    /// 从 AB 包异步加载剧情数据库，并按章节模块查找并播放剧情线。
    /// 
    /// 这是最完整的 AB 包播放流程：
    /// 1. 从 AB 包加载 StoryDatabase
    /// 2. 在数据库中按 (chapterId, moduleId) 查找目标剧情线
    /// 3. 播放找到的剧情线
    /// 
    /// 适用于剧情线数量多、需要统一管理索引的场景。
    /// 数据库和剧情线可以打包在同一个 AB 包中，Unity 会自动处理依赖加载。
    /// 
    /// 使用示例：
    /// <code>
    /// await player.PlayFromABWithDatabase(
    ///     AssetBundleName.第四章物体包, "StoryDatabase", chapterId: 2, moduleId: 5);
    /// </code>
    /// </summary>
    /// <param name="dbABName">数据库所在 AB 包名称</param>
    /// <param name="dbResName">数据库资源名称</param>
    /// <param name="chapterId">目标章节编号</param>
    /// <param name="moduleId">目标模块编号</param>
    public async UniTask PlayFromABWithDatabase(string dbABName, string dbResName, int chapterId, int moduleId)  
    {
        // 第一步：从 AB 包异步加载数据库资产
        StoryDatabase database = await LoadDatabaseFromAB(dbABName, dbResName);  

        if (database == null)                            
            return;

        // 第二步：在数据库中按 (章节, 模块) 查找目标剧情线
        StoryLineData line = database.GetStoryLine(chapterId, moduleId);  

        if (line == null)                                
        {
            LogSystem.Error($"StoryPlayer: 在AB包加载的数据库中未找到 章节{chapterId} 模块{moduleId} 的剧情线！"); 
            return;                                      
        }

        PlayStoryLine(line);                             
    }

    /// <summary>
    /// 使用 Inspector 中配置的 AB 包参数异步加载数据库并播放。
    /// 
    /// 直接读取 databaseABName、databaseResName、targetChapterId、targetModuleId 字段的值。
    /// 
    /// 使用示例：
    /// <code>
    /// await player.PlayFromABDatabase();
    /// </code>
    /// </summary>
    public async UniTask PlayFromABDatabase()            
    {
        await PlayFromABWithDatabase(databaseABName, databaseResName, targetChapterId, targetModuleId);  // 读取 Inspector 字段值，委托给完整流程方法
    }
}
