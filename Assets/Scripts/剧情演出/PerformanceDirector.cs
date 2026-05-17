using System;
using System.Collections;
using System.Collections.Generic;
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
/// 外部通过 PlayScene(PerformanceSceneData) 或 PlaySceneByLookup(chapter, module) 启动演出。
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

    [Tooltip("背景图SpriteRenderer引用")]
    public SpriteRenderer backgroundRenderer;

    [Tooltip("UI画布层（用于显示文字等UI元素）")]
    public Canvas performanceCanvas;

    [Header("========== 全局设置 ==========")]

    [Tooltip("默认文字显示速度（每个字符秒数）")]
    public float defaultTypeSpeed = 0.05f;

    [Tooltip("是否允许跳过（按任意键跳过当前命令）")]
    public bool allowSkip = true;

    [Header("========== 运行时状态（只读）==========")]

    [SerializeField, Tooltip("当前正在执行的场景数据")]
    private PerformanceSceneData currentScene;

    [SerializeField, Tooltip("当前命令索引")]
    private int currentCommandIndex;

    [SerializeField, Tooltip("是否正在执行中")]
    private bool isExecuting;

    // ============================================================
    //  事件回调
    // ============================================================

    /// <summary>演出开始</summary>
    public Action<PerformanceSceneData> OnPerformanceStarted;

    /// <summary>命令执行前（参数：命令索引，命令数据）</summary>
    public Action<int, PerformanceCommand> OnCommandExecuting;

    /// <summary>对话文字显示（参数：说话人，内容）</summary>
    public Action<string, string> OnDialogueDisplay;

    /// <summary>浮空文字显示（参数：内容，屏幕位置）</summary>
    public Action<string, Vector2> OnFloatingTextDisplay;

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

    // ============================================================
    //  内部状态
    // ============================================================

    private CancellationTokenSource executionCTS;
    private bool isWaitingForInput;

    private void Update()
    {
        // 等待用户交互推进
        if (isWaitingForInput)
        {
            if (Input.anyKeyDown || (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began))
            {
                isWaitingForInput = false;
            }
        }

        // 跳过功能：按任意键跳过当前命令
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

        // 执行命令序列
        await ExecuteCommandSequence(data.commands, executionCTS.Token);

        // 正常结束
        if (isExecuting)
        {
            isExecuting = false;
            OnPerformanceEnded?.Invoke();
        }
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
        OnPerformanceInterrupted?.Invoke();
    }

    /// <summary>
    /// 跳过当前命令（仅在非WaitForInput状态有效）
    /// 会取消当前正在执行的命令，推进到下一个
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
            isExecuting = false;
            OnPerformanceEnded?.Invoke();
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
    //  场景初始化
    // ============================================================

    private async UniTask InitializeScene(PerformanceSceneData data)
    {
        // 设置初始背景
        if (!string.IsNullOrEmpty(data.initialBackgroundName) && backgroundRenderer != null)
        {
            await SetBackground(data.initialBackgroundName, data.initialBackgroundABName);
        }

        // 播放初始BGM
        if (!string.IsNullOrEmpty(data.initialBGMResName))
        {
            MusicMgr.Instance.PlayBKMusic(data.initialBGMABName, data.initialBGMResName);
        }
    }

    // ============================================================
    //  命令序列执行（核心）
    // ============================================================

    private async UniTask ExecuteCommandSequence(List<PerformanceCommand> cmds, CancellationToken ct)
    {
        for (int i = 0; i < cmds.Count; i++)
        {
            ct.ThrowIfCancellationRequested();
            currentCommandIndex = i;
            PerformanceCommand cmd = cmds[i];

            OnCommandExecuting?.Invoke(i, cmd);

            // 延迟
            if (cmd.delay > 0f)
            {
                await UniTask.WaitForSeconds(cmd.delay, cancellationToken: ct);
            }

            // 执行命令
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
        // 文字显示持续时间 = 字数 * 打字速度 + 额外停留时间
        float displayTime = (string.IsNullOrEmpty(cmd.textContent) ? 0 : cmd.textContent.Length * defaultTypeSpeed);
        float stayTime = cmd.duration > 0f ? cmd.duration : displayTime + 1.5f;
        await UniTask.WaitForSeconds(stayTime, cancellationToken: ct);
    }

    private void ExecuteFloatingText(PerformanceCommand cmd)
    {
        // 随机屏幕位置（0.1-0.9归一化范围，避边缘）
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
        if (backgroundRenderer == null)
        {
            LogSystem.Debug("PerformanceDirector: backgroundRenderer 未设置！");
            return;
        }

        if (cmd.duration > 0f)
        {
            // 有过渡时间：淡出 → 换图 → 淡入
            await effectManager.ScreenFade(1f, cmd.duration * 0.5f, ct);
            await SetBackground(cmd.backgroundSpriteName, cmd.backgroundSpriteABName);
            await effectManager.ScreenFade(0f, cmd.duration * 0.5f, ct);
        }
        else
        {
            await SetBackground(cmd.backgroundSpriteName, cmd.backgroundSpriteABName);
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
        if (cameraController == null)
        {
            LogSystem.Debug("PerformanceDirector: cameraController 未设置！");
            return;
        }
        float strength = cmd.cameraShakeStrength > 0f ? cmd.cameraShakeStrength : 0.5f;
        float time = cmd.duration > 0f ? cmd.duration : 0.3f;
        int vibrato = cmd.cameraShakeVibrato > 0 ? cmd.cameraShakeVibrato : 20;
        cameraController.Shake(strength, time, vibrato);
    }

    private async UniTask ExecuteCameraZoom(PerformanceCommand cmd, CancellationToken ct)
    {
        if (cameraController == null)
        {
            LogSystem.Debug("PerformanceDirector: cameraController 未设置！");
            return;
        }
        float target = cmd.cameraZoomTarget > 0f ? cmd.cameraZoomTarget : 1f;
        float time = cmd.duration > 0f ? cmd.duration : 1f;
        await cameraController.ZoomTo(target, time, ct);
    }

    private async UniTask ExecuteCameraPan(PerformanceCommand cmd, CancellationToken ct)
    {
        if (cameraController == null)
        {
            LogSystem.Debug("PerformanceDirector: cameraController 未设置！");
            return;
        }
        float time = cmd.duration > 0f ? cmd.duration : 2f;
        await cameraController.PanTo(cmd.cameraPanOffset, time, ct);
    }

    private async UniTask ExecuteCameraRotate(PerformanceCommand cmd, CancellationToken ct)
    {
        if (cameraController == null)
        {
            LogSystem.Debug("PerformanceDirector: cameraController 未设置！");
            return;
        }
        float time = cmd.duration > 0f ? cmd.duration : 0.8f;
        await cameraController.RotateTo(cmd.cameraRotateAngle, time, ct);
    }

    private void ExecuteCameraReset()
    {
        if (cameraController == null)
        {
            LogSystem.Debug("PerformanceDirector: cameraController 未设置！");
            return;
        }
        cameraController.ResetCamera();
    }

    // ============================================================
    //  特效类实现
    // ============================================================

    private async UniTask ExecuteScreenColorFilter(PerformanceCommand cmd, CancellationToken ct)
    {
        if (effectManager == null)
        {
            LogSystem.Debug("PerformanceDirector: effectManager 未设置！");
            return;
        }
        float time = cmd.duration > 0f ? cmd.duration : 1f;
        await effectManager.SetColorFilter(cmd.screenFilterColor, time, ct);
    }

    private async UniTask ExecuteScreenFade(PerformanceCommand cmd, CancellationToken ct)
    {
        if (effectManager == null)
        {
            LogSystem.Debug("PerformanceDirector: effectManager 未设置！");
            return;
        }
        float time = cmd.duration > 0f ? cmd.duration : 1f;
        await effectManager.ScreenFade(cmd.screenFadeAlpha, time, ct);
    }

    private async UniTask ExecuteScreenBlur(PerformanceCommand cmd, CancellationToken ct)
    {
        if (effectManager == null)
        {
            LogSystem.Debug("PerformanceDirector: effectManager 未设置！");
            return;
        }
        float time = cmd.duration > 0f ? cmd.duration : 1f;
        await effectManager.SetBlur(cmd.blurStrength, time, ct);
    }

    private void ExecuteParticleEffect(PerformanceCommand cmd)
    {
        if (effectManager == null)
        {
            LogSystem.Debug("PerformanceDirector: effectManager 未设置！");
            return;
        }
        effectManager.SpawnParticle(cmd.particleResName, cmd.particleABName, cmd.particleSpawnPos);
    }

    private async UniTask ExecuteFlash(PerformanceCommand cmd, CancellationToken ct)
    {
        if (effectManager == null)
        {
            LogSystem.Debug("PerformanceDirector: effectManager 未设置！");
            return;
        }
        Color color = cmd.flashColor != default ? cmd.flashColor : Color.white;
        float time = cmd.duration > 0f ? cmd.duration : 0.3f;
        // 闪白：瞬间变亮 → 渐隐
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

        // 将所有子命令并行执行
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
