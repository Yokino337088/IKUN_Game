using System.Collections.Generic;
using System;
using System.Threading; // 引入 CancellationToken，使渐变任务能够在面板对象销毁时安全结束，避免异步任务继续访问已销毁的 Unity 对象。
using Cysharp.Threading.Tasks; // 引入 UniTask、PlayerLoopTiming 和 Forget 等 API，用更低分配的异步方式逐帧驱动图片渐变。
using TangmenFramework;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 关卡选择面板。
/// 负责处理五个章节之间的切换、章节图片和标题的刷新、进入当前章节以及返回开始面板。
/// 按钮事件由 <see cref="BasePanel"/> 根据控件名称自动注册，因此场景中的按钮名称必须与
/// <see cref="ClickBtn"/> 方法中的名称保持一致。
/// </summary>
public class SelectLevelPanel : BasePanel
{
    [SerializeField]
    private Image btn确定;

    [SerializeField]
    private Image btn返回;


    /// <summary>
    /// 游戏设计中固定存在的章节数量。
    /// 当前需求为五章，因此 Inspector 中的章节配置数量也必须为五项。
    /// </summary>
    private const int ChapterCount = 5;

    /// <summary>
    /// 单个章节所需要的显示和场景数据。
    /// 每一个列表元素分别对应第一章至第五章。
    /// </summary>
    [Serializable]
    private sealed class ChapterData
    {
        /// <summary>
        /// 显示在关卡选择界面顶部的章节名称。
        /// 如果没有填写，程序会自动显示“第X章”。
        /// </summary>
        [SerializeField]
        [Tooltip("章节显示名称；留空时自动显示第X章")]
        private string chapterName;

        /// <summary>
        /// 当前章节在关卡选择界面中展示的图片。
        /// </summary>
        [SerializeField]
        [Tooltip("切换到该章节时显示在关卡图片区域中的图片")]
        private Sprite chapterSprite;

        /// <summary>
        /// 点击进入章节时需要加载的 Unity 场景名称。
        /// 该名称必须与 Build Settings 中的场景名称完全一致。
        /// </summary>
        [SerializeField]
        [Tooltip("需要加载的场景名称，必须已经加入 Build Settings")]
        private string sceneName;

        /// <summary>获取章节显示名称。</summary>
        public string ChapterName => chapterName;

        /// <summary>获取章节展示图片。</summary>
        public Sprite ChapterSprite => chapterSprite;

        /// <summary>获取章节对应的场景名称。</summary>
        public string SceneName => sceneName;
    }

    /// <summary>
    /// 用于显示当前章节图片的 Image 组件。
    /// 如果 Inspector 中没有手动赋值，Awake 时会尝试查找名为“img关卡显示图”的控件。
    /// </summary>
    [Header("章节显示组件")]
    [SerializeField]
    [Tooltip("显示当前选中章节图片的 Image 组件")]
    private Image chapterInfoImg;

    /// <summary>
    /// 用于显示当前章节名称的文本组件。
    /// 该组件为可选项，没有赋值时不会影响图片切换和场景加载。
    /// </summary>
    [SerializeField]
    [Tooltip("显示当前章节名称的 Text 组件，可不配置")]
    private Text chapterDescriptionText;

    /// <summary>
    /// 章节图片交叉渐变所需的时间。
    /// 设置为零时会立即切换图片。
    /// </summary>
    [Header("章节切换动画")] // 在 Inspector 中单独显示渐变动画配置区域，方便策划快速找到并调整相关参数。
    [SerializeField] // 将私有渐变时长序列化到 Prefab，使不同面板实例可以在 Inspector 中保存自己的配置值。
    [Min(0f)] // 限制 Inspector 输入值不能小于零，防止负时长导致渐变进度计算异常。
    [Tooltip("章节图片交叉渐变时长，设置为0时立即切换")] // 在 Inspector 悬停时说明该参数的具体用途以及零值代表的行为。
    private float chapterImageFadeDuration = 0.35f; // 保存一次章节图片交叉渐变的持续秒数，默认使用较自然的 0.35 秒。

    /// <summary>
    /// 第一章至第五章的数据列表。
    /// 默认创建五个元素，Inspector 中需要依次配置每一章的图片和场景名称。
    /// </summary>
    [Header("五章配置")]
    [SerializeField]
    [Tooltip("必须按第一章到第五章的顺序配置五个章节")]
    private List<ChapterData> chapters = new List<ChapterData>
    {
        new ChapterData(),
        new ChapterData(),
        new ChapterData(),
        new ChapterData(),
        new ChapterData()
    };

    /// <summary>
    /// 面板第一次初始化时默认选中的章节索引。
    /// 索引从零开始：0 表示第一章，4 表示第五章。
    /// </summary>
    [SerializeField]
    [Range(0, ChapterCount - 1)]
    [Tooltip("默认选中的章节：0为第一章，4为第五章")]
    private int defaultChapterIndex;

    /// <summary>
    /// 当前选中的章节索引，使用从零开始的索引规则。
    /// </summary>
    private int currentChapterIndex;

    /// <summary>
    /// 是否正在加载章节场景，用于防止玩家连续点击进入按钮导致重复加载场景。
    /// </summary>
    private bool isLoadingChapter;

    /// <summary>
    /// 叠加在章节图片上方的临时 Image，用于显示渐变进入的新图片。
    /// </summary>
    private Image chapterTransitionImg; // 保存运行时创建的上层 Image；它负责淡入目标 Sprite，动画完成后会被清空并禁用。

    /// <summary>
    /// 物体销毁时自动取消异步渐变任务的令牌。
    /// </summary>
    private CancellationToken chapterFadeDestroyToken; // 缓存当前面板的销毁令牌，保证 UniTask 在对象销毁后立即停止等待下一帧。

    /// <summary>
    /// 当前渐变任务版本号。开始或停止切换时递增，使旧任务自动退出。
    /// </summary>
    private int chapterImageFadeVersion; // 每次开始或终止渐变都递增该版本号，旧任务发现版本不一致后会自行退出。

    /// <summary>
    /// 章节图片原本的颜色，渐变时只修改透明度并在结束后恢复。
    /// </summary>
    private Color chapterImageBaseColor = Color.white; // 记录主 Image 的原始颜色，渐变过程中只改变 Alpha，结束后完整恢复该颜色。

    /// <summary>
    /// 当前渐变最终需要显示的图片，用于连续点击时正确完成上一次切换。
    /// </summary>
    private Sprite chapterImageFadeTarget; // 记录当前异步任务最终要显示的 Sprite，连续点击时可立即完成旧目标后再开始新切换。

    /// <summary>
    /// 标记当前是否处于章节图片渐变过程中。
    /// </summary>
    private bool isChapterImageFading; // 标记当前是否存在有效渐变任务，供停止逻辑判断是否需要应用尚未完成的目标图片。

    /// <summary>
    /// 初始化面板和子控件引用，并显示默认章节的数据。
    /// </summary>
    protected override void Awake()
    {
        base.Awake();

        //安卓端的AB包加载有点问题，如果直接显示面板的话，那么按钮的材质就会丢失，所以必须先把材质从AB包当中加载出来
        //然后再把材质的shader给设置好，这样才能正常显示
        ABResMgr.Instance.LoadResAsync<Material>(MyAssetBundleName.开始场景材质包, "按钮材质", (mat) =>
        {
            mat.shader = Shader.Find("Custom/ButtonSelectEffect");
            btn确定.material = mat;
            btn返回.material = mat;
        });

        // 允许通过 Inspector 直接赋值；未赋值时再通过 BasePanel 收集的控件名称自动查找。
        if (chapterInfoImg == null)
            chapterInfoImg = GetControl<Image>("img关卡显示图");

        if (chapterInfoImg != null) // 只有成功获得主章节 Image 后，才具备创建叠加层和执行图片渐变的条件。
        { // 进入章节图片渐变组件的初始化作用域。
            chapterImageBaseColor = chapterInfoImg.color; // 缓存 Inspector 中配置的完整颜色，避免动画结束后错误地固定恢复成纯白色。
            CreateChapterTransitionImage(); // 提前创建与主图完全重合的上层 Image，避免玩家第一次切换时才产生初始化开销。
        } // 完成章节图片渐变组件的初始化。

        chapterFadeDestroyToken = destroyCancellationToken; // 缓存 Unity 提供的对象销毁令牌，供每一帧的 UniTask.Yield 安全监听生命周期。

        // 章节标题不是核心功能，因此查找不到时只是不显示文字，不影响关卡切换。
        if (chapterDescriptionText == null)
            chapterDescriptionText = GetControl<Text>("txt章节描述");

        // 防止 Inspector 中保存了超出列表范围的默认索引。
        currentChapterIndex = GetValidChapterIndex(defaultChapterIndex);
        RefreshChapterView(false); // Awake 首次显示默认章节时直接设置图片，避免面板刚创建就播放一次没有必要的渐变。
    }

    /// <summary>
    /// 每次显示面板时重新刷新章节内容，并重置场景加载状态。
    /// </summary>
    public override void ShowMe()
    {
        base.ShowMe();
        isLoadingChapter = false;
        RefreshChapterView(false); // 面板重新显示时立即恢复稳定图片状态，并清理上次隐藏前可能残留的中间透明度。
    }

    /// <summary>
    /// 面板停用时结束未完成的图片切换，避免下次显示时保留中间透明度。
    /// </summary>
    private void OnDisable() // Unity 在面板 GameObject 被停用时自动调用该生命周期方法，用于终止尚未完成的异步渐变。
    { // 打开面板停用清理逻辑的作用域。
        StopChapterImageFade(true); // 让当前渐变目标立即成为稳定主图，并使仍在等待下一帧的旧 UniTask 因版本变化而退出。
    } // 完成面板停用时的渐变清理。

    /// <summary>
    /// 接收 BasePanel 自动注册的按钮点击事件。
    /// 按钮名称必须分别为：btn左、btn右、btn进入章节、btn返回。
    /// </summary>
    /// <param name="btnName">被点击按钮的 GameObject 名称。</param>
    protected override void ClickBtn(string btnName)
    {
        base.ClickBtn(btnName);

        switch (btnName)
        {
            case "btn左":
                SwitchChapter(-1);
                break;
            case "btn右":
                SwitchChapter(1);
                break;
            case "btn进入章节":
                EnterCurrentChapter();
                break;
            case "btn返回":
                ReturnToBeginPanel();
                break;
        }
    }

    /// <summary>
    /// 根据偏移量切换当前章节。
    /// 使用循环切换规则：第一章向左会进入第五章，第五章向右会回到第一章。
    /// </summary>
    /// <param name="offset">切换方向，-1 表示上一章，1 表示下一章。</param>
    private void SwitchChapter(int offset)
    {
        if (!HasChapterData())
            return;

        // 先加上列表数量再取余，可以避免向左切换时出现负数索引。
        currentChapterIndex = (currentChapterIndex + offset + chapters.Count) % chapters.Count;
        RefreshChapterView(true); // 玩家主动点击左右按钮时启用交叉渐变，让当前章节图片平滑过渡到新章节图片。
    }

    /// <summary>
    /// 根据当前章节索引刷新章节图片和章节名称。
    /// </summary>
    /// <param name="useImageFade">是否使用交叉渐变切换章节图片。</param>
    private void RefreshChapterView(bool useImageFade) // 通过 useImageFade 区分首次刷新和玩家主动切换，从而决定是否播放图片渐变。
    {
        if (!HasChapterData())
            return;

        // 每次读取列表前都校正索引，避免运行时修改列表后产生越界异常。
        currentChapterIndex = GetValidChapterIndex(currentChapterIndex);
        ChapterData chapter = chapters[currentChapterIndex];

        if (chapter == null)
        {
            LogSystem.Error($"SelectLevelPanel: 第 {currentChapterIndex + 1} 个章节配置为空");
            return;
        }

        if (chapterInfoImg != null) // 确认主章节 Image 引用有效后再执行图片刷新，避免缺少 UI 配置时抛出空引用异常。
            RefreshChapterImage(chapter.ChapterSprite, useImageFade); // 将目标章节 Sprite 和动画开关交给专用渐变入口统一处理。

        if (chapterDescriptionText != null)
        {
            // 未填写自定义章节名称时，根据当前索引自动生成默认名称。
            chapterDescriptionText.text = string.IsNullOrWhiteSpace(chapter.ChapterName) ? $"第{currentChapterIndex + 1}章" : chapter.ChapterName;
        }
    }

    /// <summary>
    /// 根据设置立即刷新章节图片，或在当前图片与目标图片之间执行交叉渐变。
    /// </summary>
    /// <param name="targetSprite">切换后需要显示的章节图片。</param>
    /// <param name="useFade">是否使用交叉渐变。</param>
    private void RefreshChapterImage(Sprite targetSprite, bool useFade) // 统一处理章节 Sprite 的立即刷新与异步交叉渐变，确保所有入口遵循相同的状态清理规则。
    { // 打开章节图片刷新入口的作用域。
        if (chapterInfoImg == null) // 如果主章节 Image 没有正确绑定，就无法显示旧图、目标图或任何渐变效果。
            return; // 立即结束本次图片刷新，避免后续代码访问空的 Image 引用而抛出异常。

        if (!useFade || !isActiveAndEnabled || chapterImageFadeDuration <= 0f) // 首次刷新、组件未激活或动画时长无效时，都不应该启动逐帧异步渐变。
        { // 打开立即切换图片的分支作用域。
            SetChapterImageImmediately(targetSprite); // 终止旧任务并直接把目标 Sprite 设置为主图，保证界面立即进入稳定状态。
            return; // 立即切换已经完成，因此结束方法并阻止下面的渐变任务继续创建。
        } // 完成立即切换分支的处理。

        // 连续点击时先完成上一次切换，再从上一次的目标图片渐变到新的目标图片。
        StopChapterImageFade(true); // 递增任务版本并应用旧目标，防止多个 UniTask 同时修改同一组 Image 的透明度。

        if (chapterInfoImg.sprite == targetSprite && chapterInfoImg.enabled == (targetSprite != null)) // 如果当前稳定主图已经与目标 Sprite 及其显示状态完全一致，就没有必要重复播放动画。
            return; // 跳过冗余渐变，减少逐帧更新和 UI 重绘开销。

        int fadeVersion = ++chapterImageFadeVersion; // 为新任务生成唯一版本号，使它能够判断自己是否已被后续点击或面板停用淘汰。
        FadeChapterImageAsync(targetSprite, fadeVersion).Forget(); // 以 fire-and-forget 方式启动 UniTask；任务内部自行处理销毁与版本取消，不阻塞按钮点击流程。
    } // 完成章节图片刷新入口的处理。

    /// <summary>
    /// 创建与章节图片区域完全重合的临时图片层。
    /// 该对象只在运行时创建，不需要手动修改 Prefab，也不会拦截 UI 点击。
    /// </summary>
    private void CreateChapterTransitionImage() // 创建并初始化专门负责淡入目标 Sprite 的运行时叠加 Image。
    { // 打开渐变叠加层创建逻辑的作用域。
        if (chapterTransitionImg != null || chapterInfoImg == null) // 已经创建过叠加层时复用现有对象；主图不存在时则没有可对齐的参考对象。
            return; // 直接结束以避免重复创建子对象，或在缺少主图时执行无效初始化。

        GameObject transitionObject = new GameObject("img章节渐变层", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image)); // 一次性创建完整的 UGUI 图片对象，包含布局、渲染器和 Image 三个必要组件。
        transitionObject.layer = chapterInfoImg.gameObject.layer; // 复制主图所在层，确保叠加层被相同的 UI Camera 与层级剔除规则正确渲染。

        RectTransform transitionRect = transitionObject.GetComponent<RectTransform>(); // 获取新对象的 RectTransform，以便将它精确铺满主章节图片区域。
        transitionRect.SetParent(chapterInfoImg.rectTransform, false); // 把叠加层设为主图子对象，并保留标准本地变换，确保它始终绘制在主图上方。
        transitionRect.anchorMin = Vector2.zero; // 将左下锚点设置为父节点左下角，使叠加层从父矩形的起点开始拉伸。
        transitionRect.anchorMax = Vector2.one; // 将右上锚点设置为父节点右上角，使叠加层随父矩形尺寸完整拉伸。
        transitionRect.offsetMin = Vector2.zero; // 清除左侧和底部偏移，保证叠加层边缘与主图边缘完全重合。
        transitionRect.offsetMax = Vector2.zero; // 清除右侧和顶部偏移，避免叠加层尺寸比主图多出或少掉像素。
        transitionRect.localScale = Vector3.one; // 恢复标准本地缩放，防止继承或创建过程中的异常缩放影响图片重合效果。

        chapterTransitionImg = transitionObject.GetComponent<Image>(); // 缓存叠加层的 Image 引用，后续每次切换直接复用而不再查找组件。
        CopyChapterImageSettings(chapterInfoImg, chapterTransitionImg); // 复制主图的材质与显示模式，确保目标 Sprite 的渲染方式与旧图保持一致。
        chapterTransitionImg.raycastTarget = false; // 禁止叠加层参与射线检测，避免它覆盖在主图上方后阻挡按钮或其他 UI 点击。
        ResetChapterTransitionImage(); // 创建完成后立即清空并禁用 Image，使它只在真正播放渐变时产生绘制开销。
    } // 完成渐变叠加层的创建和初始化。

    /// <summary>
    /// 将原章节图片的渲染设置复制到渐变层，确保普通、切片和填充图片都保持一致。
    /// </summary>
    private void CopyChapterImageSettings(Image source, Image target) // 将影响 Sprite 外观和裁剪方式的 Image 配置从主图同步到渐变叠加层。
    { // 打开 Image 渲染设置复制逻辑的作用域。
        target.material = source.material; // 使用与主图相同的 UI 材质，确保自定义着色、混合模式和模板测试表现一致。
        target.type = source.type; // 同步 Simple、Sliced、Tiled 或 Filled 类型，避免新旧图片在切换时显示方式突然变化。
        target.preserveAspect = source.preserveAspect; // 同步宽高比保持选项，确保两层图片采用相同的拉伸或留边策略。
        target.fillCenter = source.fillCenter; // 同步切片图片是否绘制中心区域的选项，保证九宫格图片外观完全一致。
        target.fillMethod = source.fillMethod; // 同步填充图片的水平、垂直或径向方式，兼容当前 Image 可能使用的 Filled 模式。
        target.fillAmount = source.fillAmount; // 同步当前填充比例，使叠加层不会比主图多显示或少显示一部分内容。
        target.fillClockwise = source.fillClockwise; // 同步径向填充方向，避免 Filled 图片在渐变层中从相反方向绘制。
        target.fillOrigin = source.fillOrigin; // 同步填充起点，保证目标图片的裁剪起始位置与旧图一致。
        target.useSpriteMesh = source.useSpriteMesh; // 同步是否使用 Sprite 自定义网格，使透明区域优化和轮廓渲染保持一致。
        target.pixelsPerUnitMultiplier = source.pixelsPerUnitMultiplier; // 同步每单位像素倍率，确保切片边框与平铺密度在两层之间不发生跳变。
        target.maskable = source.maskable; // 同步遮罩支持，使叠加层能够与主图一样受到父级 Mask 或 RectMask2D 的裁剪。
    } // 完成所有与章节图片外观相关的 Image 设置复制。

    /// <summary>
    /// 在旧图片淡出的同时让新图片淡入。
    /// 使用不受 Time.timeScale 影响的时间，暂停状态下也能正常切换 UI。
    /// </summary>
    private async UniTask FadeChapterImageAsync(Sprite targetSprite, int fadeVersion) // 使用 UniTask 按帧增加目标图透明度，并通过版本号与销毁令牌安全响应取消。
    { // 打开章节图片异步渐变任务的作用域。
        CreateChapterTransitionImage(); // 确保叠加 Image 已经存在；正常情况下 Awake 已创建，此处同时为特殊调用顺序提供兜底。

        isChapterImageFading = true; // 标记渐变已经开始，使连续点击或面板停用时能够正确完成并清理当前任务。
        chapterImageFadeTarget = targetSprite; // 保存本次渐变的最终 Sprite，供 StopChapterImageFade 在任务中途被打断时立即应用。

        bool hasCurrentSprite = chapterInfoImg.enabled && chapterInfoImg.sprite != null; // 判断主图当前是否真正显示有效 Sprite，以决定是否需要保留旧图或执行淡出。
        bool hasTargetSprite = targetSprite != null; // 判断目标章节是否配置了 Sprite，以决定叠加层需要淡入图片还是仅让旧图淡出为空。

        CopyChapterImageSettings(chapterInfoImg, chapterTransitionImg); // 每次播放前重新同步主图设置，以兼容运行时对材质或 Image 类型所做的修改。
        chapterInfoImg.color = SetColorAlpha(chapterImageBaseColor, hasCurrentSprite ? chapterImageBaseColor.a : 0f); // 将旧图重置到完整基础透明度；没有旧图时则把 Alpha 设为零。
        chapterInfoImg.enabled = hasCurrentSprite; // 根据旧 Sprite 是否有效决定主 Image 是否参与当前过渡渲染。

        chapterTransitionImg.sprite = targetSprite; // 把即将显示的新章节 Sprite 放入上层 Image，使其能够覆盖在旧图之上逐渐显现。
        chapterTransitionImg.color = SetColorAlpha(chapterImageBaseColor, 0f); // 将目标图初始 Alpha 设为零，保证动画开始瞬间仍只看到旧图。
        chapterTransitionImg.enabled = hasTargetSprite; // 只有目标 Sprite 有效时才启用叠加 Image，避免空图片产生无意义的 UI 重绘。

        float elapsedTime = 0f; // 从零开始累计不受 Time.timeScale 影响的真实帧时间，用于计算当前渐变进度。
        while (elapsedTime < chapterImageFadeDuration) // 在累计时间达到 Inspector 配置的持续时长之前持续逐帧更新透明度。
        { // 打开单帧渐变更新循环的作用域。
            if (fadeVersion != chapterImageFadeVersion) // 如果全局版本号已变化，说明本任务已经被新切换或停止操作取代。
                return; // 立即结束旧任务且不再修改 UI；最新任务或停止方法会负责维护最终稳定状态。

            elapsedTime += Time.unscaledDeltaTime; // 累加真实帧间隔，使暂停游戏或修改 timeScale 时 UI 渐变仍按正常速度播放。
            float normalizedTime = Mathf.Clamp01(elapsedTime / chapterImageFadeDuration); // 将已用时间转换为零到一的标准进度，并限制浮点误差导致的越界值。
            float fadeProgress = Mathf.SmoothStep(0f, 1f, normalizedTime); // 使用平滑插值减缓动画起止速度，让切换比线性 Alpha 变化更加自然。

            if (hasCurrentSprite && !hasTargetSprite) // 当目标 Sprite 为空时没有上层新图可以遮盖旧图，因此需要显式降低旧图 Alpha。
                chapterInfoImg.color = SetColorAlpha(chapterImageBaseColor, chapterImageBaseColor.a * (1f - fadeProgress)); // 按反向进度让旧图从原始 Alpha 平滑降至零，实现从图片渐变到空状态。

            if (hasTargetSprite) // 只有目标章节配置有效 Sprite 时，才需要更新叠加层的淡入透明度。
                chapterTransitionImg.color = SetColorAlpha(chapterImageBaseColor, chapterImageBaseColor.a * fadeProgress); // 按平滑进度逐渐提高新图 Alpha；新图覆盖旧图时视觉上同时完成旧图淡出。

            bool isDestroyed = await UniTask.Yield(PlayerLoopTiming.Update, chapterFadeDestroyToken).SuppressCancellationThrow(); // 无堆分配地等待下一次 Update，并把对象销毁取消转换为布尔值而不是抛出异常。
            if (isDestroyed) // 如果等待期间面板对象已被销毁，所有 Image 引用都不应再被访问。
                return; // 安静结束异步任务，避免销毁场景或退出游戏时出现 MissingReferenceException。
        } // 完成所有渐变帧的透明度更新。

        if (fadeVersion != chapterImageFadeVersion) // 循环结束后再次检查版本，覆盖最后一次等待期间恰好发生新切换的边界情况。
            return; // 如果任务已经过期，就把最终状态交给新任务或停止逻辑处理，避免旧目标覆盖新选择。

        ApplyChapterImage(targetSprite); // 将已经完全淡入的目标 Sprite 正式写入主 Image，作为后续切换的稳定旧图。
        ResetChapterTransitionImage(); // 清空并禁用临时叠加 Image，避免动画结束后继续占用一次额外绘制。
        chapterImageFadeTarget = null; // 清除已完成的目标引用，防止后续停止操作误认为仍有待应用图片。
        isChapterImageFading = false; // 标记当前不再处于渐变状态，使面板状态与实际渲染结果保持一致。
    } // 完成章节图片 UniTask 渐变任务。

    /// <summary>
    /// 立即显示指定章节图片，并清理可能正在运行的渐变。
    /// </summary>
    private void SetChapterImageImmediately(Sprite targetSprite) // 不播放动画地设置目标 Sprite，并确保旧异步任务无法继续修改图片状态。
    { // 打开立即设置章节图片的作用域。
        StopChapterImageFade(false); // 递增版本号并清理渐变层，但不应用旧任务目标，因为当前参数才是最终需要显示的图片。
        ApplyChapterImage(targetSprite); // 将调用方指定的 Sprite 直接设置为稳定主图，同时恢复基础颜色和启用状态。
    } // 完成立即设置章节图片的处理。

    /// <summary>
    /// 停止当前渐变。需要衔接下一次切换时，可以先直接应用上一次的目标图片。
    /// </summary>
    private void StopChapterImageFade(bool applyFadeTarget) // 使当前 UniTask 失效并统一恢复两层 Image，必要时把未完成的渐变目标直接应用为主图。
    { // 打开章节图片渐变停止与收尾逻辑的作用域。
        chapterImageFadeVersion++; // 递增全局任务版本，使所有持有旧版本号的异步循环在下一次检查时立即退出。

        if (applyFadeTarget && isChapterImageFading) // 仅在调用方要求完成旧目标且当前确实正在渐变时，才需要覆盖主图。
            ApplyChapterImage(chapterImageFadeTarget); // 把尚未完全淡入的目标 Sprite 立即设为稳定主图，保证连续点击按章节顺序自然衔接。

        ResetChapterTransitionImage(); // 无论是否应用旧目标，都清空并关闭临时叠加层，避免残留半透明图片。
        chapterImageFadeTarget = null; // 清除旧任务保存的目标 Sprite，避免它在后续停止操作中被重复应用。
        isChapterImageFading = false; // 将渐变状态重置为未运行，与已经完成的清理结果保持一致。
    } // 完成当前章节图片渐变的停止和状态收尾。

    /// <summary>
    /// 将章节图片组件恢复为稳定显示状态。
    /// </summary>
    private void ApplyChapterImage(Sprite targetSprite) // 把指定 Sprite 写入主章节 Image，并恢复动画之外应有的稳定显示属性。
    { // 打开主章节图片稳定状态应用逻辑的作用域。
        chapterInfoImg.sprite = targetSprite; // 更新主 Image 的 Sprite，使下一次渐变能够把它作为旧图继续使用。
        chapterInfoImg.color = chapterImageBaseColor; // 恢复 Awake 时缓存的完整基础颜色，清除渐变过程中留下的临时 Alpha。

        // 未配置图片时隐藏 Image，避免显示上一章节残留的图片或空白 UI 方块。
        chapterInfoImg.enabled = targetSprite != null; // 有有效 Sprite 时启用主 Image；目标为空时禁用它以彻底停止空白 Graphic 绘制。
    } // 完成主章节 Image 的稳定状态应用。

    /// <summary>
    /// 隐藏并清空临时渐变图片层。
    /// </summary>
    private void ResetChapterTransitionImage() // 将运行时叠加 Image 恢复为空闲状态，以便下一次章节切换安全复用。
    { // 打开渐变叠加层重置逻辑的作用域。
        if (chapterTransitionImg == null) // Awake 尚未完成、主图缺失或对象正在销毁时，叠加 Image 可能还不存在。
            return; // 没有可重置的 Image 时直接结束，避免空引用异常。

        chapterTransitionImg.sprite = null; // 释放叠加 Image 对上一张目标 Sprite 的引用，防止后续错误显示旧资源。
        chapterTransitionImg.color = SetColorAlpha(chapterImageBaseColor, 0f); // 保留基础 RGB 但把 Alpha 清零，保证再次启用前不会瞬间闪出旧透明度。
        chapterTransitionImg.enabled = false; // 禁用临时 Image，避免非动画期间提交额外的 UI 顶点和绘制批次。
    } // 完成渐变叠加 Image 的空闲状态重置。

    /// <summary>
    /// 返回仅替换透明度的新颜色，不修改传入的颜色值。
    /// </summary>
    private Color SetColorAlpha(Color color, float alpha) // 创建一个仅 Alpha 不同的颜色副本，避免直接修改缓存的 chapterImageBaseColor。
    { // 打开颜色透明度替换辅助方法的作用域。
        color.a = alpha; // 修改按值传入的 Color 副本透明度，因此原始 RGB 和缓存基础颜色都不会受到影响。
        return color; // 返回带有目标 Alpha 的新颜色，供主 Image 或叠加 Image 立即应用。
    } // 完成颜色透明度替换并结束辅助方法。

    /// <summary>
    /// 检查当前章节配置并异步加载对应场景。
    /// </summary>
    private void EnterCurrentChapter()
    {
        // 正在加载时忽略后续点击，防止同时发起多个场景加载任务。
        if (isLoadingChapter || !HasChapterData())
            return;

        currentChapterIndex = GetValidChapterIndex(currentChapterIndex);
        ChapterData chapter = chapters[currentChapterIndex];

        if (chapter == null || string.IsNullOrWhiteSpace(chapter.SceneName))
        {
            LogSystem.Error($"SelectLevelPanel: 第 {currentChapterIndex + 1} 章没有配置场景名称");
            return;
        }

        // SceneMgr 最终通过 SceneManager 加载场景，因此场景必须先加入 Build Settings。
        if (!Application.CanStreamedLevelBeLoaded(chapter.SceneName))
        {
            LogSystem.Error($"SelectLevelPanel: 场景 {chapter.SceneName} 不存在或未加入 Build Settings");
            return;
        }

        isLoadingChapter = true;
        // 进入关卡前立即停止开始场景的背景音乐，并清空播放列表防止自动切到下一首
        MusicMgr.Instance.StopBKMusicAndClearList();
        MusicMgr.Instance.Dispose();
        //隐藏面板
        UIMgr.Instance.HidePanelWithAnimation<SelectLevelPanel>(E_HideType.淡出);
        //调用框架代码
        SceneMgr.Instance.LoadSceneAsyn(chapter.SceneName);
    }

    /// <summary>
    /// 检查 Inspector 中是否正好配置了五个章节。
    /// </summary>
    /// <returns>章节列表存在且数量为五时返回 true，否则返回 false。</returns>
    private bool HasChapterData()
    {
        if (chapters != null && chapters.Count == ChapterCount)
            return true;

        int currentCount = chapters == null ? 0 : chapters.Count;
        LogSystem.Error($"SelectLevelPanel: 必须配置 {ChapterCount} 个章节，当前配置了 {currentCount} 个");
        return false;
    }

    /// <summary>
    /// 返回开始面板。
    /// 同时兼容由 UIMgr 创建的面板，以及直接挂载在场景对象上的面板。
    /// </summary>
    private void ReturnToBeginPanel()
    {
        UIMgr.Instance.HidePanelWithAnimation<SelectLevelPanel>(E_HideType.淡出, ShowBeginPanel, 0.3f);        
    }

    /// <summary>
    /// 通过 UI 管理器显示开始面板。
    /// </summary>
    private void ShowBeginPanel()
    {
        UIMgr.Instance.ShowPanel<BeginPanel>(MyAssetBundleName.开始场景UI面板包);
    }

    /// <summary>
    /// 将章节索引限制在当前章节列表的有效范围内。
    /// </summary>
    /// <param name="index">需要校正的章节索引。</param>
    /// <returns>有效的章节索引；列表为空时返回 0。</returns>
    private int GetValidChapterIndex(int index)
    {
        if (chapters == null || chapters.Count == 0)
            return 0;

        return Mathf.Clamp(index, 0, chapters.Count - 1);
    }
}
