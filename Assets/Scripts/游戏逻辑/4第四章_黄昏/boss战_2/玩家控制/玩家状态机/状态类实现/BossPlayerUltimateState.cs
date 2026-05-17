using TangmenFramework;

/// <summary>
/// 玩家大招状态。
/// 
/// 进入该状态后播放大招动画，并注册一个 Animancer 动画事件回调（在动画 95% 处触发），
/// 回调中释放弹幕并切换到待机状态。
/// 
/// 大招是最高优先级的动作，播放期间不可被任何输入打断（不注册任何输入事件监听）。
/// 
/// 防重入设计与其他动作状态一致：
/// - _isCompleted 标志确保 UltimateComplete 回调只执行一次
/// - QuitState 中清除动画事件回调，防止幽灵回调
/// 
/// 相关 Bug 记录：Bug #001（攻击动画卡死_同步事件级联导致状态机重入）
/// </summary>
public class BossPlayerUltimateState : BossPlayerState
{
    public override E_BossPlayerStateType StateType => E_BossPlayerStateType.大招;

    /// <summary>
    /// 防重入标志。为 true 时表示大招已经完成，后续的 UltimateComplete 回调将被忽略。
    /// </summary>
    private bool _isCompleted;

    public BossPlayerUltimateState(StateMachine<E_BossPlayerStateType, IBossPlayerFSMObj> machine) : base(machine) { }

    public override void EnterState()
    {
        base.EnterState();
        _isCompleted = false;
        AIObj.StartUltimateCooldown();
        EventCenter.Instance.EventTrigger(MyEventTypeString.玩家大招开始冷却事件);
        PlayAnimation(E_BossPlayerStateType.大招);
        AddAnimationEvent(0.95f, UltimateComplete);
        LogSystem.Info("玩家进入大招状态");
    }

    /// <summary>
    /// 退出状态时：设置防重入标志 + 清除动画事件回调 + 注销输入事件监听。
    /// </summary>
    public override void QuitState()
    {
        _isCompleted = true;
        ClearAnimationEvents();
        base.QuitState();
    }

    public override void UpdateState()
    {
        base.UpdateState();
        
        // 备用方案：如果动画事件没有触发，使用Update检测动画进度
        if (!_isCompleted && _currentAnimState != null)
        {
            float normalizedTime = _currentAnimState.NormalizedTime % 1;
            if (normalizedTime >= 0.95f && normalizedTime < 1.0f)
            {
                UltimateComplete();
            }
        }
    }

    /// <summary>
    /// 大招动画完成回调（由 Animancer 在动画播放到 95% 时触发）。
    /// 先设 _isCompleted = true（防止重入），再释放弹幕效果，最后切换到待机。
    /// </summary>
    private void UltimateComplete()
    {
        if (_isCompleted)
            return;

        _isCompleted = true;
        AIObj.SummoningBulletComments();
        ChangeState(E_BossPlayerStateType.待机);
    }
}
