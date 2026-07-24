using Cysharp.Threading.Tasks;
using TangmenFramework;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 通用关卡切换面板。
/// 当当前关卡（歌曲列表）全部通关后弹出，提供"回到开始场景"和"进入下一关卡"两个选项。
/// 所有关卡均可复用此面板，只需在预制体 Inspector 中配置好目标场景名称即可。
/// 
/// 按钮命名约定（与 BasePanel 自动注册机制匹配）：
///   - btn回到开始场景 → 加载开始场景
///   - btn下一关       → 加载下一关场景
/// </summary>
public class LevelSwitchPanel : BasePanel
{
    [Header("场景配置")]
    [SerializeField]
    [Tooltip("点击「回到开始场景」时加载的场景名称，必须已加入 Build Settings")]
    private string startSceneName = "开始场景";

    [SerializeField]
    [Tooltip("点击「下一关」时加载的场景名称，必须已加入 Build Settings")]
    private string nextLevelSceneName = "";

    [Header("UI 控件（Inspector 手动拖拽绑定，未绑定时走 BasePanel.GetControl 按名查找）")]
    [SerializeField]
    [Tooltip("面板标题文本，显示如「关卡完成！」")]
    private Text titleText;

    [SerializeField]
    [Tooltip("返回开始场景的按钮 Image（可选，仅用于材质加载等特殊需求）")]
    private Image backButtonImage;

    [SerializeField]
    [Tooltip("进入下一关的按钮 Image（可选，仅用于材质加载等特殊需求）")]
    private Image nextButtonImage;

    // 运行时缓存
    private Button _backButton;
    private Button _nextButton;
    private bool _isNavigating; // 防止重复点击导致多次场景加载

    // ──────────────────────────────────────────────
    // BasePanel 生命周期
    // ──────────────────────────────────────────────

    public override void ShowMe()
    {
        base.ShowMe();

        // 解析 UI 控件引用（BasePanel 在 Awake 中已完成 GetControls 收集）
        ResolveControls();

        // 加载背景材质（与 BeginPanel 同理，移动端 AB 包需先设置 Shader）
        LoadButtonMaterials();

        // 面板淡入动画
        this.DoPanelFadeInAnimation(0.25f);

        _isNavigating = false;
    }

    public override void HideMe()
    {
        base.HideMe();
    }

    // ──────────────────────────────────────────────
    // 控件解析
    // ──────────────────────────────────────────────

    /// <summary>
    /// 获取所有 UI 控件引用。
    /// 优先使用 Inspector 手动拖拽的值；未绑定时通过 BasePanel.GetControl 按名称自动查找。
    /// </summary>
    private void ResolveControls()
    {
        // 标题文本：Inspector 未赋值时按约定名称查找
        if (titleText == null)
            titleText = GetControl<Text>("txt标题");

        // 两个按钮的 Button 组件（BasePanel 已按名称缓存，这里直接获取）
        _backButton = GetControl<Button>("btn回到开始场景");
        _nextButton = GetControl<Button>("btn下一关");

        // 如果存在下一关场景名称，启用下一关按钮；否则禁用（防止空场景加载）
        bool hasNextLevel = !string.IsNullOrWhiteSpace(nextLevelSceneName);
        if (_nextButton != null)
        {
            _nextButton.interactable = hasNextLevel;
            // 可选：让按钮变灰提示不可用
            Image nextImg = _nextButton.GetComponent<Image>();
            if (nextImg != null)
                nextImg.color = hasNextLevel ? Color.white : new Color(0.5f, 0.5f, 0.5f, 0.5f);
        }
    }

    // ──────────────────────────────────────────────
    // 材质加载
    // ──────────────────────────────────────────────

    /// <summary>
    /// 从 AB 包加载按钮材质。
    /// 如果 Inspector 中绑定了按钮 Image 且材质包名不为空，则异步加载材质并修复 Shader。
    /// 参考 BeginPanel 的做法，解决移动端 AB 包材质 Shader 丢失的问题。
    /// </summary>
    private void LoadButtonMaterials()
    {
        // 仅当绑定了按钮 Image 且材质包名可用时才加载
        if (backButtonImage == null && nextButtonImage == null)
            return;

        // 使用通用的 UI 面板材质包（这里使用开始场景的材质包作为后备，各关卡可自行重写）
        // 实际项目中若有独立的关卡切换材质包，可替换此处的 Bundle 名
        

        if (backButtonImage != null)
        {
            ABResMgr.Instance.LoadResAsync<Material>(MyAssetBundleName.开始场景材质包, "按钮材质", (mat) =>
            {
                if (mat != null && backButtonImage != null)
                {
                    mat.shader = Shader.Find("Custom/ButtonSelectEffect");
                    backButtonImage.material = mat;
                }
            });
        }

        if (nextButtonImage != null)
        {
            ABResMgr.Instance.LoadResAsync<Material>(MyAssetBundleName.开始场景材质包, "按钮材质", (mat) =>
            {
                if (mat != null && nextButtonImage != null)
                {
                    mat.shader = Shader.Find("Custom/ButtonSelectEffect");
                    nextButtonImage.material = mat;
                }
            });
        }
    }

    // ──────────────────────────────────────────────
    // 按钮事件（由 BasePanel 按控件名自动调用）
    // ──────────────────────────────────────────────

    /// <summary>
    /// BasePanel 自动注册的按钮点击回调。
    /// btnName 与预制体中 Button 对象的名称一一对应。
    /// </summary>
    /// <param name="btnName">被点击的按钮名称</param>
    protected override void ClickBtn(string btnName)
    {
        base.ClickBtn(btnName);

        // 防止在异步场景加载过程中重复点击
        if (_isNavigating)
            return;

        switch (btnName)
        {
            case "btn回到开始场景":
                // 回到开始场景（主菜单）
                NavigateToScene(startSceneName);
                break;

            case "btn下一关":
                // 进入下一关卡场景
                if (string.IsNullOrWhiteSpace(nextLevelSceneName))
                {
                    LogSystem.Warning("LevelSwitchPanel: 下一关场景名称未配置，无法跳转");
                    return;
                }
                NavigateToScene(nextLevelSceneName);
                break;

            default:
                LogSystem.Warning($"LevelSwitchPanel: 未处理的按钮点击 — {btnName}");
                break;
        }
    }

    // ──────────────────────────────────────────────
    // 场景导航
    // ──────────────────────────────────────────────

    /// <summary>
    /// 执行场景切换的核心方法。
    /// 先隐藏当前面板（带淡出动画），再通过框架的 SceneMgr 异步加载目标场景。
    /// 加载前会校验场景是否已加入 Build Settings。
    /// </summary>
    /// <param name="sceneName">目标场景名称（不含 .unity 扩展名）</param>
    private void NavigateToScene(string sceneName)
    {
        if (_isNavigating)
            return;

        // 校验场景是否存在
        if (string.IsNullOrWhiteSpace(sceneName))
        {
            LogSystem.Error("LevelSwitchPanel: 目标场景名称为空，无法跳转");
            return;
        }

        if (!Application.CanStreamedLevelBeLoaded(sceneName))
        {
            LogSystem.Error($"LevelSwitchPanel: 场景 \"{sceneName}\" 不存在或未加入 Build Settings");
            return;
        }

        _isNavigating = true;

        // 禁用两个按钮，防止动画播放期间再次点击
        if (_backButton != null) _backButton.interactable = false;
        if (_nextButton != null) _nextButton.interactable = false;

        // 使用框架提供的面板淡出动画，动画完成后执行场景加载回调
        UIMgr.Instance.HidePanelWithAnimation<LevelSwitchPanel>(E_HideType.淡出, () =>
        {
            // 通过框架的 SceneMgr 异步加载目标场景
            SceneMgr.Instance.LoadSceneAsyn(sceneName);
        }, 0.3f);
    }

    // ──────────────────────────────────────────────
    // 公开方法（供外部调用）
    // ──────────────────────────────────────────────

    /// <summary>
    /// 设置下一关的场景名称。
    /// 可在运行时由关卡管理器动态指定，实现"当前关卡→下一关卡"的灵活跳转。
    /// </summary>
    /// <param name="sceneName">下一关的场景名称</param>
    public void SetNextLevelSceneName(string sceneName)
    {
        nextLevelSceneName = sceneName;

        // 如果面板已经显示，同步更新按钮状态
        if (_nextButton != null)
        {
            bool hasNextLevel = !string.IsNullOrWhiteSpace(nextLevelSceneName);
            _nextButton.interactable = hasNextLevel;
            Image nextImg = _nextButton.GetComponent<Image>();
            if (nextImg != null)
                nextImg.color = hasNextLevel ? Color.white : new Color(0.5f, 0.5f, 0.5f, 0.5f);
        }
    }

    /// <summary>
    /// 设置回到开始场景的场景名称。
    /// </summary>
    /// <param name="sceneName">开始场景的名称</param>
    public void SetStartSceneName(string sceneName)
    {
        startSceneName = sceneName;
    }
}
