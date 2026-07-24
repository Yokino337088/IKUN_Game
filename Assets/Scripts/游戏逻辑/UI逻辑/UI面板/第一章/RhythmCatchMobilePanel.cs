using TangmenFramework;

/// <summary>
/// 第一章节奏接箱移动端操作面板。
/// ChapterOneLevelBootstrap 仅在 PlatformHelper.IsMobile 为 true 时通过 UIMgr 显示此面板。
/// </summary>
public class RhythmCatchMobilePanel : BasePanel
{
    //按钮控件
    private LongPressButton _leftButton;
    private LongPressButton _rightButton;
    //按钮状态
    private bool _leftPressed;
    private bool _rightPressed;
    private bool _registered;

    public override void ShowMe()
    {
        base.ShowMe();
        ResolveControls();
        RegisterButtonEvents();
        this.DoPanelFadeInAnimation(0.2f);
    }

    public override void HideMe()
    {
        UnregisterButtonEvents();
        _leftPressed = false;
        _rightPressed = false;

        // 面板隐藏时强制发送 0，防止手指仍按住时切场景导致玩家持续移动。
        SendHorizontalInput(0f);
        base.HideMe();
    }

    //查找控件
    private void ResolveControls()
    {
        _leftButton = GetControl<LongPressButton>("左移");
        _rightButton = GetControl<LongPressButton>("右移");
    }

    /// <summary>
    /// 注册按钮事件
    /// </summary>
    private void RegisterButtonEvents()
    {
        if (_registered)
            return;

        if (_leftButton != null)
        {
            _leftButton.onPointerDown.AddListener(OnLeftPointerDown);
            _leftButton.onPointerUp.AddListener(OnLeftPointerUp);
        }

        if (_rightButton != null)
        {
            _rightButton.onPointerDown.AddListener(OnRightPointerDown);
            _rightButton.onPointerUp.AddListener(OnRightPointerUp);
        }

        _registered = true;
    }

    /// <summary>
    /// 注销按钮事件
    /// </summary>
    private void UnregisterButtonEvents()
    {
        if (!_registered)
            return;

        if (_leftButton != null)
        {
            _leftButton.onPointerDown.RemoveListener(OnLeftPointerDown);
            _leftButton.onPointerUp.RemoveListener(OnLeftPointerUp);
        }

        if (_rightButton != null)
        {
            _rightButton.onPointerDown.RemoveListener(OnRightPointerDown);
            _rightButton.onPointerUp.RemoveListener(OnRightPointerUp);
        }

        _registered = false;
    }

    private void OnLeftPointerDown()
    {
        _leftPressed = true;
        RefreshHorizontalInput();
    }

    private void OnLeftPointerUp()
    {
        _leftPressed = false;
        RefreshHorizontalInput();
    }

    private void OnRightPointerDown()
    {
        _rightPressed = true;
        RefreshHorizontalInput();
    }

    private void OnRightPointerUp()
    {
        _rightPressed = false;
        RefreshHorizontalInput();
    }

    private void RefreshHorizontalInput()
    {
        // 两侧同时按住时互相抵消；释放任意一侧后立即恢复另一侧方向。
        float direction = 0f;
        if (_leftPressed && !_rightPressed)
            direction = -1f;
        else if (_rightPressed && !_leftPressed)
            direction = 1f;

        SendHorizontalInput(direction);
    }

    private static void SendHorizontalInput(float value)
    {
        // 与 PC InputMgr 完全复用同一个框架事件，玩家控制器无需关心输入来自键盘还是 UGUI。
        EventCenter.Instance.EventTrigger<float>(E_EventType.E_Input_Horizontal, value);
    }
}
