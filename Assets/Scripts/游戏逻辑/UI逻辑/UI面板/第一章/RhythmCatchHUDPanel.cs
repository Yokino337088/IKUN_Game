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
    private Text _scoreText;
    private Text _comboText;
    private Text _timeText;
    private Text _judgementText;
    private Text _stateText;
    private Text _gradeText;
    private Image _shieldImage;
    private Image _magnetImage;
    private Image _resultBackground;
    private bool _subscribed;

    public override void ShowMe()
    {
        base.ShowMe();
        ResolveControls();
        SubscribeEvents();
        this.DoPanelFadeInAnimation(0.25f);
    }

    public override void HideMe()
    {
        UnsubscribeEvents();
        KillUiTweens();
        base.HideMe();
    }

    protected override void ClickBtn(string btnName)
    {
        base.ClickBtn(btnName);
        // 第一章玩家无敌、无重新开始需求，按钮事件已移除。
    }

    private void ResolveControls()
    {
        // BasePanel 在 Awake 中已缓存全部 UGUI 控件，这里不会触发 GameObject.Find。
        _scoreText = GetControl<Text>("txt分数");
        _comboText = GetControl<Text>("txt连击");
        _timeText = GetControl<Text>("txt时间");
        _judgementText = GetControl<Text>("txt判定");
        _stateText = GetControl<Text>("txt状态");
        _gradeText = GetControl<Text>("txt评级");
        _shieldImage = GetControl<Image>("img护盾");
        _magnetImage = GetControl<Image>("img磁铁");
        _resultBackground = GetControl<Image>("img结算背景");
    }

    private void SubscribeEvents()
    {
        if (_subscribed)
            return;

        EventCenter.Instance.AddEventListener<RhythmCatchHudSnapshot>(RhythmCatchEventNames.HudChanged, OnHudChanged);
        EventCenter.Instance.AddEventListener<RhythmCatchJudgement>(RhythmCatchEventNames.JudgementChanged, OnJudgementChanged);
        _subscribed = true;
    }

    private void UnsubscribeEvents()
    {
        if (!_subscribed)
            return;

        EventCenter.Instance.RemoveEventListener<RhythmCatchHudSnapshot>(RhythmCatchEventNames.HudChanged, OnHudChanged);
        EventCenter.Instance.RemoveEventListener<RhythmCatchJudgement>(RhythmCatchEventNames.JudgementChanged, OnJudgementChanged);
        _subscribed = false;
    }

    private void OnHudChanged(RhythmCatchHudSnapshot snapshot)
    {
        if (_scoreText != null)
            _scoreText.text = $"分数\n{snapshot.score:N0}";
        if (_comboText != null)
            _comboText.text = $"连击数\n{snapshot.combo}";
        if (_timeText != null)
            _timeText.text = $"{snapshot.elapsedSeconds:00.0} / {snapshot.durationSeconds:00.0}s";

        if (_shieldImage != null)
            _shieldImage.gameObject.SetActive(snapshot.hasShield);
        if (_magnetImage != null)
        {
            bool magnetActive = snapshot.magnetRemaining > 0f;
            _magnetImage.gameObject.SetActive(magnetActive);
            if (magnetActive)
                _magnetImage.fillAmount = Mathf.Clamp01(snapshot.magnetRemaining / 5f);
        }

        bool showResult = snapshot.state == RhythmCatchGameState.Finished || snapshot.state == RhythmCatchGameState.Failed;
        if (_resultBackground != null)
            _resultBackground.gameObject.SetActive(showResult);
        if (_gradeText != null)
        {
            _gradeText.gameObject.SetActive(showResult);
            _gradeText.text = snapshot.state == RhythmCatchGameState.Failed
                ? $"挑战失败\n评级 {snapshot.grade}\n接取率 {snapshot.catchRate:P0}"
                : $"关卡完成\n评级 {snapshot.grade}\n接取率 {snapshot.catchRate:P0}\n最大 Combo {snapshot.maxCombo}";
        }

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

    private void OnJudgementChanged(RhythmCatchJudgement judgement)
    {
        if (_judgementText == null)
            return;

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
                _judgementText.text = "还行";
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

        // 判定文字 DOTween 弹跳，动画结束后自动回到原始缩放。
        RectTransform rect = _judgementText.rectTransform;
        rect.DOKill();
        _judgementText.DOKill();
        rect.localScale = Vector3.one;
        _judgementText.canvasRenderer.SetAlpha(1f);
        rect.DOPunchScale(Vector3.one * 0.3f, 0.24f, 6, 0.5f);
        _judgementText.DOFade(0f, 0.45f).SetDelay(0.25f);

        // 连击数弹跳：先复位到原始缩放再执行，避免累积偏移。
        if (_comboText != null)
        {
            _comboText.rectTransform.DOKill();
            _comboText.rectTransform.localScale = Vector3.one;
            _comboText.rectTransform.DOPunchScale(Vector3.one * 0.12f, 0.18f, 5, 0.45f);
        }
    }

    private void KillUiTweens()
    {
        _judgementText?.DOKill();
        _judgementText?.rectTransform.DOKill();
        _comboText?.rectTransform.DOKill();
    }

}
