using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using TangmenFramework;
using UnityEngine;

/// <summary>
/// 演出总导演 —— 剧情演出框架的核心运行时组件。
/// 
/// 【职责】
/// 1. 接收 PerformanceSceneData，按顺序解析并执行命令序列
/// 2. 管理演出生命周期：开始 → 执行中 → 结束/中断
/// 3. 向外部（UI层、关卡逻辑）抛出状态变化事件
/// 4. 调度子模块：角色控制器、摄像机控制器、特效管理器
/// 
/// 【使用方式】
/// 挂载到场景中的空GameObject上，在Inspector中设置好子模块引用。
/// 外部通过 PlayScene(PerformanceSceneData) 启动演出。
/// 
/// 【两种执行模式】
/// - Sequential（顺序模式）：命令按列表顺序逐条执行，执行完即结束
/// - BGMDriven（BGM驱动模式）：以一首BGM为时间轴，命令可绑定时间戳触发
/// 
/// 【意识流演出的执行特点】
/// - 命令支持延迟和并行，实现多层画面元素同时呈现
/// - WaitForInput 命令在意识流中代表"玩家消化完当前信息后推进"
/// - 整个演出可以不间断自动推进，也可以在某些节点等待玩家交互
/// </summary>
public class PerformanceDirector : MonoBehaviour
{
    [Header("========== 子模块引用 ==========")]

    [Tooltip("角色控制器（管理角色Sprite的显示/隐藏/移动/表情切换）")]
    public PerformanceCharacter characterController;

    [Tooltip("摄像机控制器（管理震动/缩放/平移效果）")]
    public PerformanceCameraController cameraController;

    [Tooltip("特效管理器（管理屏幕滤镜/模糊/粒子等视觉效果）")]
    public PerformanceEffectManager effectManager;

    [Header("========== 场景引用 ==========")]

    [Tooltip("演出数据（可直接拖入场景资产，也支持代码动态设置）")]
    public PerformanceSceneData sceneData;

    [Tooltip("背景图SpriteRenderer引用（简单背景模式）")]
    public SpriteRenderer backgroundRenderer;

    [Tooltip("背景根节点（GameObject背景模式，支持Shader动效）")]
    public Transform backgroundRoot;

    [Tooltip("UI画布层（用于显示文字等UI元素）")]
    public Canvas performanceCanvas;

    [Header("========== 全局设置 ==========")]

    [Tooltip("默认文字显示速度（每个字符秒数）")]
    public float defaultTypeSpeed = 0.05f;

    [Tooltip("是否允许跳过（按Escape/Space跳过当前命令）")]
    public bool allowSkip = true;

    [Header("========== 运行时状态（只读）==========")]

    [SerializeField, Tooltip("当前正在执行的场景数据")]
    private PerformanceSceneData currentScene;

    [SerializeField, Tooltip("当前命令索引")]
    private int currentCommandIndex;

    [SerializeField, Tooltip("是否正在执行中")]
    private bool isExecuting;

    [SerializeField, Tooltip("演出已运行的时间（BGMDriven模式用于进度展示）")]
    private float performanceElapsedTime;

    [SerializeField, Tooltip("演出总时长")]
    private float performanceTotalDuration;

    // ============================================================
    //  事件回调
    // ============================================================

    /// <summary>演出开始</summary>
    public Action<PerformanceSceneData> OnPerformanceStarted;

    /// <summary>命令执行前（参数：命令索引，命令数据）。BGMDriven模式中索引可能为-1表示时间戳触发</summary>
    public Action<int, PerformanceCommand> OnCommandExecuting;

    /// <summary>对话文字显示（参数：说话人，内容）</summary>
    public Action<string, string> OnDialogueDisplay;

    /// <summary>浮空文字显示（参数：内容，屏幕位置）</summary>
    public Action<string, Vector2> OnFloatingTextDisplay;

    /// <summary>BGM进度更新（参数：当前秒数，总秒数）。BGMDriven模式专用，可用于进度条显示</summary>
    public Action<float, float> OnBGMProgress;

    /// <summary>演出正常结束</summary>
    public Action OnPerformanceEnded;

    /// <summary>演出被中断</summary>
    public Action OnPerformanceInterrupted;

    // ============================================================
    //  公开属性
    // ============================================================

    public bool IsExecuting => isExecuting;
    public int CurrentCommandIndex => currentCommandIndex;
    public PerformanceSceneData CurrentScene => currentScene;
    public float PerformanceElapsedTime => performanceElapsedTime;
    public float PerformanceTotalDuration => performanceTotalDuration;

    // ============================================================
    //  内部状态
    // ============================================================

    private CancellationTokenSource executionCTS;
    private bool isWaitingForInput;
    private float performanceStartTime; // Time.time at performance start (for progress tracking)

    private void Update()
    {
        // BGM进度追踪（BGMDriven模式）
        if (isExecuting && currentScene != null && currentScene.syncMode == PerformanceSyncMode.BGMDriven)
        {
            performanceElapsedTime = Time.time - performanceStartTime;
            if (performanceTotalDuration > 0f)
            {
                OnBGMProgress?.Invoke(performanceElapsedTime, performanceTotalDuration);
            }
        }

        // 等待用户交互推进
        if (isWaitingForInput)
        {
            if (Input.anyKeyDown || (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began))
            {
                isWaitingForInput = false;
            }
        }

        // 跳过功能：按Escape或Space跳过当前命令
        if (allowSkip && isExecuting && !isWaitingForInput)
        {
            if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.Space))
            {
                SkipCurrentCommand();
            }
        }
    }

    private void OnDisable()
    {
        StopPerformance();
    }

    private void OnDestroy()
    {
        executionCTS?.Cancel();
        executionCTS?.Dispose();
        executionCTS = null;
    }

    // ============================================================
    //  公开 API —— 启动演出
    // ============================================================

    /// <summary>播放指定的演出场景</summary>
    public async void PlayScene(PerformanceSceneData data)
    {
        if (data == null)
        {
            LogSystem.Error("PerformanceDirector: 演出场景数据为空！");
            return;
        }

        if (data.CommandCount == 0)
        {
            LogSystem.Error($"PerformanceDirector: 演出场景 [{data.sceneName}] 没有命令！");
            return;
        }

        // 停止当前演出
        if (isExecuting)
            StopPerformance();

        currentScene = data;
        currentCommandIndex = 0;
        isExecuting = true;

        executionCTS?.Cancel();
        executionCTS?.Dispose();
        executionCTS = new CancellationTokenSource();

        // 初始化场景
        await InitializeScene(data);

        OnPerformanceStarted?.Invoke(data);

        // 根据同步模式选择执行方式
        if (data.syncMode == PerformanceSyncMode.BGMDriven)
        {
            await ExecuteBGMDrivenSequence(data.commands, executionCTS.Token);
        }
        else
        {
            await ExecuteCommandSequence(data.commands, executionCTS.Token);
        }

        // 正常结束
        OnPerformanceCleanup();
    }

    /// <summary>播放Inspector中拖入的演出场景</summary>
    public void Play()
    {
        PlayScene(sceneData);
    }

    // ============================================================
    //  公开 API —— 控制
    // ============================================================

    /// <summary>停止当前演出（中断）</summary>
    public void StopPerformance()
    {
        if (!isExecuting)
            return;

        executionCTS?.Cancel();
        isExecuting = false;
        isWaitingForInput = false;

        // BGMDriven模式：停止演出主BGM
        if (currentScene != null && currentScene.syncMode == PerformanceSyncMode.BGMDriven)
        {
            MusicMgr.Instance.StopBKMusic();
        }

        OnPerformanceInterrupted?.Invoke();
    }

    /// <summary>
    /// 跳过当前命令（仅在非WaitForInput状态有效）
    /// </summary>
    public void SkipCurrentCommand()
    {
        if (!isExecuting || isWaitingForInput)
            return;

        executionCTS?.Cancel();
        executionCTS?.Dispose();
        executionCTS = new CancellationTokenSource();

        currentCommandIndex++;

        if (currentCommandIndex >= currentScene.CommandCount)
        {
            OnPerformanceCleanup();
        }
        else
        {
            ExecuteCommandSequenceFromIndex(currentScene.commands, currentCommandIndex, executionCTS.Token).Forget();
        }
    }

    /// <summary>强制推进（在WaitForInput状态下由外部调用）</summary>
    public void AdvanceInput()
    {
        isWaitingForInput = false;
    }

    // ============================================================
    //  演出收尾
    // ============================================================

    private void OnPerformanceCleanup()
    {
        if (!isExecuting)
            return;

        isExecuting = false;

        // BGMDriven模式：停止演出主BGM
        if (currentScene != null && currentScene.syncMode == PerformanceSyncMode.BGMDriven)
        {
            MusicMgr.Instance.StopBKMusic();
        }

        OnPerformanceEnded?.Invoke();
    }

    // ============================================================
    //  场景初始化
    // ============================================================

    private async UniTask InitializeScene(PerformanceSceneData data)
    {
        // 设置初始背景
        if (!string.IsNullOrEmpty(data.initialBackgroundName) && backgroundRenderer != null)
        {
            await SetBackground(data.initialBackgroundName, data.initialBackgroundABName);
        }

        // BGMDriven模式：播放演出主BGM
        if (data.syncMode == PerformanceSyncMode.BGMDriven && !string.IsNullOrEmpty(data.performanceBGMResName))
        {
            // 设置演出BGM音量（覆盖全局音量）
            if (data.performanceBGMVolume > 0f)
            {
                MusicMgr.Instance.ChangeBKMusicValue(data.performanceBGMVolume);
            }
            
            // 播放BGM，不循环（演出是单曲播放）
            MusicMgr.Instance.PlayBKMusic(data.performanceBGMABName, data.performanceBGMResName, false);
            LogSystem.Info($"PerformanceDirector: BGMDriven模式启动 BGM [{data.performanceBGMResName}]，音量={data.performanceBGMVolume}");

            // 设置总时长（优先使用手动设置，否则为0即不追踪百分比）
            performanceTotalDuration = data.performanceDuration;
            if (performanceTotalDuration <= 0f)
            {
                LogSystem.Debug("PerformanceDirector: performanceDuration未设置，BGM进度百分比不可用（时间戳命令仍正常工作）");
            }
        }
        // Sequential模式：播放初始BGM
        else if (data.syncMode == PerformanceSyncMode.Sequential && !string.IsNullOrEmpty(data.initialBGMResName))
        {
            MusicMgr.Instance.PlayBKMusic(data.initialBGMABName, data.initialBGMResName);
        }

        // 记录演出开始时间
        performanceStartTime = Time.time;
        performanceElapsedTime = 0f;
    }

    // ============================================================
    //  命令序列执行 —— Sequential模式
    // ============================================================

    private async UniTask ExecuteCommandSequence(List<PerformanceCommand> cmds, CancellationToken ct)
    {
        for (int i = 0; i < cmds.Count; i++)
        {
            ct.ThrowIfCancellationRequested();
            currentCommandIndex = i;
            PerformanceCommand cmd = cmds[i];

            OnCommandExecuting?.Invoke(i, cmd);

            if (cmd.delay > 0f)
            {
                await UniTask.WaitForSeconds(cmd.delay, cancellationToken: ct);
            }

            await ExecuteSingleCommand(cmd, ct);
        }
    }

    private async UniTask ExecuteCommandSequenceFromIndex(List<PerformanceCommand> cmds, int startIdx, CancellationToken ct)
    {
        for (int i = startIdx; i < cmds.Count; i++)
        {
            ct.ThrowIfCancellationRequested();
            currentCommandIndex = i;
            PerformanceCommand cmd = cmds[i];

            OnCommandExecuting?.Invoke(i, cmd);

            if (cmd.delay > 0f)
            {
                await UniTask.WaitForSeconds(cmd.delay, cancellationToken: ct);
            }

            await ExecuteSingleCommand(cmd, ct);
        }
    }

    // ============================================================
    //  命令序列执行 —— BGMDriven模式（长剧情演出核心）
    // ============================================================

    /// <summary>
    /// BGMDriven模式执行入口。
    /// 
    /// 【执行逻辑】
    /// 1. 将命令分为两组：设置了timestamp的"时间戳命令" vs 未设置的"顺序命令"
    /// 2. 时间戳命令各自延迟到指定秒数后触发（并行等待）
    /// 3. 顺序命令按列表顺序逐条执行
    /// 4. 两组命令并行运行，演出结束条件：
    ///    - 设置了performanceDuration → 到达时长后结束
    ///    - 未设置 → 等待所有命令执行完毕后结束
    /// </summary>
    private async UniTask ExecuteBGMDrivenSequence(List<PerformanceCommand> cmds, CancellationToken ct)
    {
        // 分离时间戳命令和顺序命令
        List<PerformanceCommand> timestampedCmds = new List<PerformanceCommand>();
        List<PerformanceCommand> sequentialCmds = new List<PerformanceCommand>();

        for (int i = 0; i < cmds.Count; i++)
        {
            PerformanceCommand cmd = cmds[i];
            // 将命令信息拷贝到一个字段，用于日志输出
            if (cmd.timestamp > 0f)
            {
                timestampedCmds.Add(cmd);
            }
            else
            {
                sequentialCmds.Add(cmd);
            }
        }

        LogSystem.Info($"PerformanceDirector: BGMDriven模式 → 时间戳命令 {timestampedCmds.Count} 条, 顺序命令 {sequentialCmds.Count} 条");

        // 构建所有并行Task
        List<UniTask> allTasks = new List<UniTask>();

        // 1. 时间戳命令：各自等待到指定秒数后触发
        for (int i = 0; i < timestampedCmds.Count; i++)
        {
            PerformanceCommand cmd = timestampedCmds[i];
            allTasks.Add(ExecuteTimestampedCommand(cmd, ct));
        }

        // 2. 顺序命令：逐条执行
        if (sequentialCmds.Count > 0)
        {
            allTasks.Add(ExecuteCommandSequence(sequentialCmds, ct));
        }

        // 3. 如果设置了总时长，添加时长限制Task
        if (performanceTotalDuration > 0f)
        {
            allTasks.Add(WaitForDuration(performanceTotalDuration, ct));
        }

        // 并行等待所有任务完成
        await UniTask.WhenAll(allTasks);
    }

    /// <summary>
    /// 执行一条时间戳命令：等待到指定时刻后触发
    /// </summary>
    private async UniTask ExecuteTimestampedCommand(PerformanceCommand cmd, CancellationToken ct)
    {
        float targetTime = cmd.timestamp;

        // 等待到达目标时刻
        float elapsed = 0f;
        while (elapsed < targetTime)
        {
            ct.ThrowIfCancellationRequested();
            elapsed += Time.deltaTime;
            await UniTask.Yield(ct);
        }

        // 触发命令
        LogSystem.Info($"PerformanceDirector: 时间戳命令触发 (t={targetTime:F1}s)");
        OnCommandExecuting?.Invoke(-1, cmd); // -1 表示时间戳触发
        await ExecuteSingleCommand(cmd, ct);
    }

    /// <summary>
    /// 等待演出总时长结束后返回（用于BGMDriven模式下限制总时长）
    /// </summary>
    private async UniTask WaitForDuration(float totalDuration, CancellationToken ct)
    {
        float elapsed = 0f;
        while (elapsed < totalDuration)
        {
            ct.ThrowIfCancellationRequested();
            elapsed += Time.deltaTime;
            await UniTask.Yield(ct);
        }
        LogSystem.Info($"PerformanceDirector: BGMDriven演出时长到达 ({totalDuration:F1}s)，结束演出");
    }

    // ============================================================
    //  单条命令分发与执行
    // ============================================================

    private async UniTask ExecuteSingleCommand(PerformanceCommand cmd, CancellationToken ct)
    {
        switch (cmd.type)
        {
            case EPerformanceCommandType.Dialogue:
                await ExecuteDialogue(cmd, ct);
                break;

            case EPerformanceCommandType.FloatingText:
                ExecuteFloatingText(cmd);
                break;

            case EPerformanceCommandType.CharacterVisibility:
                await ExecuteCharacterVisibility(cmd, ct);
                break;

            case EPerformanceCommandType.CharacterMove:
                await ExecuteCharacterMove(cmd, ct);
                break;

            case EPerformanceCommandType.CharacterExpression:
                await ExecuteCharacterExpression(cmd, ct);
                break;

            case EPerformanceCommandType.CharacterShake:
                await ExecuteCharacterShake(cmd, ct);
                break;

            case EPerformanceCommandType.BackgroundChange:
                await ExecuteBackgroundChange(cmd, ct);
                break;

            case EPerformanceCommandType.BackgroundScroll:
                await ExecuteBackgroundScroll(cmd, ct);
                break;

            case EPerformanceCommandType.CameraShake:
                ExecuteCameraShake(cmd);
                break;

            case EPerformanceCommandType.CameraZoom:
                await ExecuteCameraZoom(cmd, ct);
                break;

            case EPerformanceCommandType.CameraPan:
                await ExecuteCameraPan(cmd, ct);
                break;

            case EPerformanceCommandType.CameraRotate:
                await ExecuteCameraRotate(cmd, ct);
                break;

            case EPerformanceCommandType.CameraReset:
                ExecuteCameraReset();
                break;

            case EPerformanceCommandType.ScreenColorFilter:
                await ExecuteScreenColorFilter(cmd, ct);
                break;

            case EPerformanceCommandType.ScreenFade:
                await ExecuteScreenFade(cmd, ct);
                break;

            case EPerformanceCommandType.ScreenBlur:
                await ExecuteScreenBlur(cmd, ct);
                break;

            case EPerformanceCommandType.ParticleEffect:
                ExecuteParticleEffect(cmd);
                break;

            case EPerformanceCommandType.Flash:
                await ExecuteFlash(cmd, ct);
                break;

            case EPerformanceCommandType.PlayBGM:
                ExecutePlayBGM(cmd);
                break;

            case EPerformanceCommandType.StopBGM:
                ExecuteStopBGM(cmd);
                break;

            case EPerformanceCommandType.PlaySFX:
                ExecutePlaySFX(cmd);
                break;

            case EPerformanceCommandType.Wait:
                await ExecuteWait(cmd, ct);
                break;

            case EPerformanceCommandType.Parallel:
                await ExecuteParallel(cmd, ct);
                break;

            case EPerformanceCommandType.WaitForInput:
                await ExecuteWaitForInput(ct);
                break;
        }
    }

    // ============================================================
    //  文字类实现
    // ============================================================

    private async UniTask ExecuteDialogue(PerformanceCommand cmd, CancellationToken ct)
    {
        OnDialogueDisplay?.Invoke(cmd.speakerName, cmd.textContent);
        float displayTime = (string.IsNullOrEmpty(cmd.textContent) ? 0 : cmd.textContent.Length * defaultTypeSpeed);
        float stayTime = cmd.duration > 0f ? cmd.duration : displayTime + 1.5f;
        await UniTask.WaitForSeconds(stayTime, cancellationToken: ct);
    }

    private void ExecuteFloatingText(PerformanceCommand cmd)
    {
        Vector2 pos = new Vector2(
            UnityEngine.Random.Range(0.15f, 0.85f),
            UnityEngine.Random.Range(0.2f, 0.8f)
        );
        OnFloatingTextDisplay?.Invoke(cmd.textContent, pos);
    }

    // ============================================================
    //  角色类实现
    // ============================================================

    private async UniTask ExecuteCharacterVisibility(PerformanceCommand cmd, CancellationToken ct)
    {
        if (characterController == null)
        {
            LogSystem.Debug("PerformanceDirector: characterController 未设置！");
            return;
        }
        await characterController.SetVisibility(cmd.characterName, cmd.characterVisible, cmd.duration, ct);
    }

    private async UniTask ExecuteCharacterMove(PerformanceCommand cmd, CancellationToken ct)
    {
        if (characterController == null)
        {
            LogSystem.Debug("PerformanceDirector: characterController 未设置！");
            return;
        }
        await characterController.MoveTo(cmd.characterName, cmd.characterTargetPos, cmd.duration, ct);
    }

    private async UniTask ExecuteCharacterExpression(PerformanceCommand cmd, CancellationToken ct)
    {
        if (characterController == null)
        {
            LogSystem.Debug("PerformanceDirector: characterController 未设置！");
            return;
        }
        await characterController.SetExpression(cmd.characterName, cmd.characterSpriteName, cmd.characterSpriteABName, ct);
    }

    private async UniTask ExecuteCharacterShake(PerformanceCommand cmd, CancellationToken ct)
    {
        if (characterController == null)
        {
            LogSystem.Debug("PerformanceDirector: characterController 未设置！");
            return;
        }
        float intensity = cmd.shakeIntensity > 0f ? cmd.shakeIntensity : 5f;
        float time = cmd.duration > 0f ? cmd.duration : 0.5f;
        await characterController.Shake(cmd.characterName, intensity, time, ct);
    }

    // ============================================================
    //  场景/背景类实现
    // ============================================================

    private async UniTask ExecuteBackgroundChange(PerformanceCommand cmd, CancellationToken ct)
    {
        // GameObject背景模式（支持Shader动效）
        if (backgroundRoot != null)
        {
            if (cmd.duration > 0f)
            {
                await effectManager.ScreenFade(1f, cmd.duration * 0.5f, ct);
                SwitchBackgroundGameObject(cmd.backgroundSpriteName);
                await effectManager.ScreenFade(0f, cmd.duration * 0.5f, ct);
            }
            else
            {
                SwitchBackgroundGameObject(cmd.backgroundSpriteName);
            }
            return;
        }

        // SpriteRenderer背景模式（简单图片切换）
        if (backgroundRenderer == null)
        {
            LogSystem.Debug("PerformanceDirector: backgroundRenderer 未设置！");
            return;
        }

        if (cmd.duration > 0f)
        {
            await effectManager.ScreenFade(1f, cmd.duration * 0.5f, ct);
            await SetBackground(cmd.backgroundSpriteName, cmd.backgroundSpriteABName);
            await effectManager.ScreenFade(0f, cmd.duration * 0.5f, ct);
        }
        else
        {
            await SetBackground(cmd.backgroundSpriteName, cmd.backgroundSpriteABName);
        }
    }

    /// <summary>切换GameObject背景（通过显隐控制，支持Shader动效）</summary>
    private void SwitchBackgroundGameObject(string backgroundName)
    {
        if (backgroundRoot == null || string.IsNullOrEmpty(backgroundName))
            return;

        // 隐藏所有背景
        foreach (Transform child in backgroundRoot)
        {
            child.gameObject.SetActive(false);
        }

        // 显示目标背景
        Transform target = backgroundRoot.Find(backgroundName);
        if (target != null)
        {
            target.gameObject.SetActive(true);
            LogSystem.Info($"PerformanceDirector: 切换背景 -> [{backgroundName}]");
        }
        else
        {
            LogSystem.Debug($"PerformanceDirector: 未找到背景 [{backgroundName}]");
        }
    }

    private async UniTask ExecuteBackgroundScroll(PerformanceCommand cmd, CancellationToken ct)
    {
        if (backgroundRenderer == null || characterController == null)
            return;

        float elapsed = 0f;
        Vector3 startPos = backgroundRenderer.transform.localPosition;
        float scrollTime = cmd.duration > 0f ? cmd.duration : 3f;

        while (elapsed < scrollTime)
        {
            ct.ThrowIfCancellationRequested();
            elapsed += Time.deltaTime;
            float t = elapsed / scrollTime;
            backgroundRenderer.transform.localPosition = startPos + (Vector3)(cmd.backgroundScrollDir * cmd.backgroundScrollSpeed * t);
            await UniTask.Yield(ct);
        }
    }

    // ============================================================
    //  镜头类实现
    // ============================================================

    private void ExecuteCameraShake(PerformanceCommand cmd)
    {
        if (cameraController == null) return;
        float strength = cmd.cameraShakeStrength > 0f ? cmd.cameraShakeStrength : 0.5f;
        float time = cmd.duration > 0f ? cmd.duration : 0.3f;
        int vibrato = cmd.cameraShakeVibrato > 0 ? cmd.cameraShakeVibrato : 20;
        cameraController.Shake(strength, time, vibrato);
    }

    private async UniTask ExecuteCameraZoom(PerformanceCommand cmd, CancellationToken ct)
    {
        if (cameraController == null) return;
        float target = cmd.cameraZoomTarget > 0f ? cmd.cameraZoomTarget : 1f;
        float time = cmd.duration > 0f ? cmd.duration : 1f;
        await cameraController.ZoomTo(target, time, ct);
    }

    private async UniTask ExecuteCameraPan(PerformanceCommand cmd, CancellationToken ct)
    {
        if (cameraController == null) return;
        float time = cmd.duration > 0f ? cmd.duration : 2f;
        await cameraController.PanTo(cmd.cameraPanOffset, time, ct);
    }

    private async UniTask ExecuteCameraRotate(PerformanceCommand cmd, CancellationToken ct)
    {
        if (cameraController == null) return;
        float time = cmd.duration > 0f ? cmd.duration : 0.8f;
        await cameraController.RotateTo(cmd.cameraRotateAngle, time, ct);
    }

    private void ExecuteCameraReset()
    {
        if (cameraController == null) return;
        cameraController.ResetCamera();
    }

    // ============================================================
    //  特效类实现
    // ============================================================

    private async UniTask ExecuteScreenColorFilter(PerformanceCommand cmd, CancellationToken ct)
    {
        if (effectManager == null) return;
        float time = cmd.duration > 0f ? cmd.duration : 1f;
        await effectManager.SetColorFilter(cmd.screenFilterColor, time, ct);
    }

    private async UniTask ExecuteScreenFade(PerformanceCommand cmd, CancellationToken ct)
    {
        if (effectManager == null) return;
        float time = cmd.duration > 0f ? cmd.duration : 1f;
        await effectManager.ScreenFade(cmd.screenFadeAlpha, time, ct);
    }

    private async UniTask ExecuteScreenBlur(PerformanceCommand cmd, CancellationToken ct)
    {
        if (effectManager == null) return;
        float time = cmd.duration > 0f ? cmd.duration : 1f;
        await effectManager.SetBlur(cmd.blurStrength, time, ct);
    }

    private void ExecuteParticleEffect(PerformanceCommand cmd)
    {
        if (effectManager == null) return;
        effectManager.SpawnParticle(cmd.particleResName, cmd.particleABName, cmd.particleSpawnPos);
    }

    private async UniTask ExecuteFlash(PerformanceCommand cmd, CancellationToken ct)
    {
        if (effectManager == null) return;
        Color color = cmd.flashColor != default ? cmd.flashColor : Color.white;
        float time = cmd.duration > 0f ? cmd.duration : 0.3f;
        await effectManager.Flash(color, time * 0.3f, time * 0.7f, ct);
    }

    // ============================================================
    //  音频类实现
    // ============================================================

    private void ExecutePlayBGM(PerformanceCommand cmd)
    {
        MusicMgr.Instance.PlayBKMusic(cmd.audioABName, cmd.audioResName);
    }

    private void ExecuteStopBGM(PerformanceCommand cmd)
    {
        MusicMgr.Instance.StopBKMusic();
    }

    private void ExecutePlaySFX(PerformanceCommand cmd)
    {
        float duration = cmd.duration;
        MusicMgr.Instance.PlaySoundSafe(cmd.audioABName, cmd.audioResName, null, duration);
    }

    // ============================================================
    //  控制类实现
    // ============================================================

    private async UniTask ExecuteWait(PerformanceCommand cmd, CancellationToken ct)
    {
        float waitTime = cmd.duration > 0f ? cmd.duration : 0.5f;
        await UniTask.WaitForSeconds(waitTime, cancellationToken: ct);
    }

    private async UniTask ExecuteParallel(PerformanceCommand cmd, CancellationToken ct)
    {
        if (cmd.parallelCommands == null || cmd.parallelCommands.Count == 0)
            return;

        List<UniTask> tasks = new List<UniTask>();
        foreach (var subCmd in cmd.parallelCommands)
        {
            tasks.Add(ExecuteSingleCommand(subCmd, ct));
        }
        await UniTask.WhenAll(tasks);
    }

    private async UniTask ExecuteWaitForInput(CancellationToken ct)
    {
        isWaitingForInput = true;
        while (isWaitingForInput)
        {
            ct.ThrowIfCancellationRequested();
            await UniTask.Yield(ct);
        }
    }

    // ============================================================
    //  内部工具方法
    // ============================================================

    private async UniTask SetBackground(string spriteName, string abName)
    {
        if (backgroundRenderer == null || string.IsNullOrEmpty(spriteName))
            return;

        Sprite bg = await ABResMgr.Instance.LoadResAsync<Sprite>(abName, spriteName);
        if (bg != null)
            backgroundRenderer.sprite = bg;
    }
}
