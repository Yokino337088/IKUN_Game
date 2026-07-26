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

    /// <summary>
    /// 通过 BasePanel.GetControl 按名称查找左右长按按钮控件。
    /// </summary>
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

    /// <summary>左移按钮按下。</summary>
    private void OnLeftPointerDown()
    {
        _leftPressed = true;
        RefreshHorizontalInput();
    }

    /// <summary>左移按钮释放。</summary>
    private void OnLeftPointerUp()
    {
        _leftPressed = false;
        RefreshHorizontalInput();
    }

    /// <summary>右移按钮按下。</summary>
    private void OnRightPointerDown()
    {
        _rightPressed = true;
        RefreshHorizontalInput();
    }

    /// <summary>右移按钮释放。</summary>
    private void OnRightPointerUp()
    {
        _rightPressed = false;
        RefreshHorizontalInput();
    }

    /// <summary>
    /// 根据左右按钮状态计算水平输入值：仅左按=-1，仅右按=+1，同时按或都不按=0。
    /// </summary>
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

    /// <summary>
    /// 通过框架 EventCenter 发送水平输入事件，与 PC 键盘共用同一事件通道。
    /// </summary>
    /// <param name="value">-1=左, 0=无, +1=右。</param>
    private static void SendHorizontalInput(float value)
    {
        // 与 PC InputMgr 完全复用同一个框架事件，玩家控制器无需关心输入来自键盘还是 UGUI。
        EventCenter.Instance.EventTrigger<float>(E_EventType.E_Input_Horizontal, value);
    }
}
