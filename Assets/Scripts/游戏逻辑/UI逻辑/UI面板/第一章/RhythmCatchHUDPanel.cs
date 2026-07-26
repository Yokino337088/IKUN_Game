using DG.Tweening;
using TangmenFramework;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 第一章节奏接箱 HUD 面板。
/// 面板遵循唐老师 UI 框架约定：预制体名与脚本类名一致，控件通过 BasePanel.GetControl 按名称获取。
/// </summary>
public class RhythmCatchHUDPanel : BasePanel
{
    /// <summary>分数文本。</summary>
    private Text _scoreText;
    /// <summary>连击数文本。</summary>
    private Text _comboText;
    /// <summary>剩余时间文本。</summary>
    private Text _timeText;
    /// <summary>判定反馈文本（Perfect/Great 等）。</summary>
    private Text _judgementText;
    /// <summary>状态文本（倒计时/GO/FAILED/FINISH）。</summary>
    private Text _stateText;
    /// <summary>护盾图标。</summary>
    private Image _shieldImage;
    /// <summary>磁铁图标（含 fillAmount 倒计时环）。</summary>
    private Image _magnetImage;
    /// <summary>是否已注册 EventCenter 事件监听。</summary>
    private bool _subscribed;
    /// <summary>上一次广播的分数，用于检测分数变化以触发弹跳动画。</summary>
    private int _lastScore;

    /// <summary>面板显示时解析控件、订阅事件并播放淡入动画。</summary>
    public override void ShowMe()
    {
        base.ShowMe();
        ResolveControls();
        SubscribeEvents();
        this.DoPanelFadeInAnimation(0.25f);
    }

    /// <summary>面板隐藏时注销事件、清理 DOTween 动画。</summary>
    public override void HideMe()
    {
        UnsubscribeEvents();
        KillUiTweens();
        base.HideMe();
    }

    /// <summary>第一章无按钮交互需求，保留空实现以兼容 BasePanel 自动注册。</summary>
    protected override void ClickBtn(string btnName)
    {
        base.ClickBtn(btnName);
    }

    /// <summary>
    /// 通过 BasePanel.GetControl 按控件名获取所有 UI 组件引用。
    /// BasePanel 在 Awake 中已缓存全部控件，这里不会触发 GameObject.Find。
    /// </summary>
    private void ResolveControls()
    {
        _scoreText = GetControl<Text>("txt分数");
        _comboText = GetControl<Text>("txt连击");
        _timeText = GetControl<Text>("txt时间");
        _judgementText = GetControl<Text>("txt判定");
        _stateText = GetControl<Text>("txt状态");
        _shieldImage = GetControl<Image>("img护盾");
        _magnetImage = GetControl<Image>("img磁铁");
    }

    /// <summary>
    /// 向 EventCenter 注册 HUD 快照更新和判定反馈两个事件监听。
    /// </summary>
    private void SubscribeEvents()
    {
        if (_subscribed)
            return;

        EventCenter.Instance.AddEventListener<RhythmCatchHudSnapshot>(RhythmCatchEventNames.HudChanged, OnHudChanged);
        EventCenter.Instance.AddEventListener<RhythmCatchJudgement>(RhythmCatchEventNames.JudgementChanged, OnJudgementChanged);
        _subscribed = true;
    }

    /// <summary>
    /// 从 EventCenter 注销事件监听，防止面板隐藏后继续接收回调。
    /// </summary>
    private void UnsubscribeEvents()
    {
        if (!_subscribed)
            return;

        EventCenter.Instance.RemoveEventListener<RhythmCatchHudSnapshot>(RhythmCatchEventNames.HudChanged, OnHudChanged);
        EventCenter.Instance.RemoveEventListener<RhythmCatchJudgement>(RhythmCatchEventNames.JudgementChanged, OnJudgementChanged);
        _subscribed = false;
    }

    /// <summary>
    /// 接收玩法层广播的 HUD 快照，刷新分数、连击、时间、道具图标和状态文字。
    /// 分数变化时触发 DOTween 弹跳动画。
    /// </summary>
    private void OnHudChanged(RhythmCatchHudSnapshot snapshot)
    {
        // --- 分数：检测变化时播放弹跳动画 ---
        if (_scoreText != null)
        {
            bool scoreChanged = snapshot.score != _lastScore;
            _scoreText.text = $"分数\n{snapshot.score:N0}";
            if (scoreChanged)
            {
                _lastScore = snapshot.score;
                _scoreText.rectTransform.DOKill();            // 先停旧动画
                _scoreText.rectTransform.localScale = Vector3.one;
                _scoreText.rectTransform.DOPunchScale(Vector3.one * 0.12f, 0.18f, 5, 0.45f);
            }
        }
        // --- 连击 / 剩余时间 ---
        if (_comboText != null)
            _comboText.text = $"连击数\n{snapshot.combo}";
        if (_timeText != null)
            _timeText.text = $"剩余时间\n{snapshot.durationSeconds - snapshot.elapsedSeconds:00.0}s";

        // --- 道具图标 ---
        if (_shieldImage != null)
            _shieldImage.gameObject.SetActive(snapshot.hasShield);
        if (_magnetImage != null)
        {
            bool magnetActive = snapshot.magnetRemaining > 0f;
            _magnetImage.gameObject.SetActive(magnetActive);
            if (magnetActive)
                _magnetImage.fillAmount = Mathf.Clamp01(snapshot.magnetRemaining / 5f); // 剩余时间→0~1进度
        }

        // --- 状态文字 ---
        if (_stateText != null)
        {
            switch (snapshot.state)
            {
                case RhythmCatchGameState.Countdown:
                    _stateText.gameObject.SetActive(true);
                    _stateText.text = snapshot.countdown > 0 ? snapshot.countdown.ToString() : "GO!";
                    break;
                case RhythmCatchGameState.Playing:
                    _stateText.gameObject.SetActive(false);
                    break;
                case RhythmCatchGameState.Failed:
                    _stateText.gameObject.SetActive(true);
                    _stateText.text = "FAILED";
                    break;
                case RhythmCatchGameState.Finished:
                    _stateText.gameObject.SetActive(true);
                    _stateText.text = "FINISH";
                    break;
                default:
                    _stateText.gameObject.SetActive(true);
                    _stateText.text = "READY";
                    break;
            }
        }
    }

    /// <summary>
    /// 接收判定反馈事件，根据判定类型更新文字、颜色，并播放 DOTween 弹跳+淡出动画。
    /// 同时触发连击数的弹跳效果。
    /// </summary>
    private void OnJudgementChanged(RhythmCatchJudgement judgement)
    {
        if (_judgementText == null)
            return;
        //设置判断文字
        switch (judgement)
        {
            case RhythmCatchJudgement.Perfect:
                _judgementText.text = "牛逼";
                _judgementText.color = new Color(0.35f, 1f, 1f);
                break;
            case RhythmCatchJudgement.Great:
                _judgementText.text = "666";
                _judgementText.color = new Color(1f, 0.75f, 0.15f);
                break;
            case RhythmCatchJudgement.Good:
                _judgementText.text = "还可以";
                _judgementText.color = new Color(0.35f, 1f, 0.45f);
                break;
            case RhythmCatchJudgement.Miss:
                _judgementText.text = "菜";
                _judgementText.color = new Color(1f, 0.2f, 0.2f);
                break;
            case RhythmCatchJudgement.Shielded:
                _judgementText.text = "格挡";
                _judgementText.color = new Color(0.25f, 0.65f, 1f);
                break;
            default:
                _judgementText.text = "鸡鸡";
                _judgementText.color = new Color(1f, 0.1f, 0.1f);
                break;
        }

        // --- 判定文字动画：复位 → 弹跳放大 → 淡出消失 ---
        RectTransform rect = _judgementText.rectTransform;
        // 停掉上一次动画
        rect.DOKill();                                     
        _judgementText.DOKill();
        // 复位缩放
        rect.localScale = Vector3.one;
        // 复位透明度
        _judgementText.canvasRenderer.SetAlpha(1f);        
        rect.DOPunchScale(Vector3.one * 0.3f, 0.24f, 6, 0.5f);
        // 延迟0.25s后淡出
        _judgementText.DOFade(0f, 0.45f).SetDelay(0.25f);  

        // --- 连击数弹跳 ---
        if (_comboText != null)
        {
            _comboText.rectTransform.DOKill();
            _comboText.rectTransform.localScale = Vector3.one;
            _comboText.rectTransform.DOPunchScale(Vector3.one * 0.12f, 0.18f, 5, 0.45f);
        }
    }

    /// <summary>
    /// 面板隐藏时停止所有运行中的 DOTween 动画，防止下一局残留运动状态。
    /// </summary>
    private void KillUiTweens()
    {
        _judgementText?.DOKill();
        _judgementText?.rectTransform.DOKill();
        _comboText?.rectTransform.DOKill();
        _scoreText?.rectTransform.DOKill();
    }

}
