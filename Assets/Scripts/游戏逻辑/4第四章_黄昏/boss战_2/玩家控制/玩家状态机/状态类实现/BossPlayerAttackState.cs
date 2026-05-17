using TangmenFramework;
using UnityEngine;

/// <summary>
/// 玩家普攻状态。
/// 
/// 进入该状态后播放攻击动画，并注册一个 Animancer 动画事件回调（在动画 85% 处触发），
/// 回调中发射子弹并切换到待机状态。
/// 
/// 【防重入设计】
/// 该状态不注册任何输入事件监听（不重写 RegisterInputEvent），
/// 因此在攻击动画播放期间，玩家的移动/跳跃/普攻等输入不会触发状态切换。
/// 状态切换仅在 AttackComplete 动画事件回调中自动发生。
/// 
/// _isCompleted 标志的作用：
///   防止 AttackComplete 回调被重复执行。在某些极端情况下（如同步事件级联导致
///   状态在短时间内被反复切换），Animancer 的动画事件可能被触发多次。
///   _isCompleted 确保"发射子弹 + 切换状态"的逻辑只执行一次。
/// 
/// QuitState 中调用 ClearAnimationEvents 的作用：
///   当状态被外部强制退出时（如受到伤害、关卡重置等），必须清除旧动画上注册的
///   事件回调。否则下次进入该状态时，旧动画上的幽灵回调可能仍在生效，
///   导致动画逻辑错乱。
/// </summary>
public class BossPlayerAttackState : BossPlayerState
{
    public override E_BossPlayerStateType StateType => E_BossPlayerStateType.普攻;

    /// <summary>
    /// 防重入标志。为 true 时表示攻击已经完成，后续的 AttackComplete 回调将被忽略。
    /// EnterState 时重置为 false，QuitState 时设置为 true。
    /// </summary>
    private bool _isCompleted;

    public BossPlayerAttackState(StateMachine<E_BossPlayerStateType, IBossPlayerFSMObj> machine) : base(machine) { }

    public override void EnterState()
    {
        base.EnterState();
        _isCompleted = false;
        AIObj.StartAttackCooldown();
        PlayAnimation(E_BossPlayerStateType.普攻);
        MusicMgr.Instance.PlaySoundSafe(MyAssetBundleName.通用音效包, "鸡");
        AddAnimationEvent(0.85f, AttackComplete);
        LogSystem.Info("玩家进入普攻状态，动画事件已添加");
    }

    /// <summary>
    /// 退出状态时执行清理工作：
    /// 1. 设置 _isCompleted = true，确保已注册的 AttackComplete 回调不再生效
    /// 2. 调用 ClearAnimationEvents() 清除旧动画上的事件回调，防止幽灵回调
    /// 3. 调用 base.QuitState() 注销所有输入事件监听
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
        // normalizedTime >= 0.85 表示动画播放到85%以上
        // normalizedTime % 1 处理循环动画的情况
        if (!_isCompleted && _currentAnimState != null)
        {
            float normalizedTime = _currentAnimState.NormalizedTime % 1;
            if (normalizedTime >= 0.85f && normalizedTime < 0.9f)
            {
                AttackComplete();
            }
        }
    }

    /// <summary>
    /// 攻击动画完成回调（由 Animancer 在动画播放到 85% 时触发）。
    /// 
    /// 执行流程：
    /// 1. 检查 _isCompleted：如果已为 true（说明状态已退出或已执行过），直接返回
    /// 2. 立即设置 _isCompleted = true，防止后续重入
    /// 3. 通过 AIObj 接口发射子弹
    /// 4. 切换到待机状态
    /// 
    /// 注意：_isCompleted = true 必须设置在发射子弹之前，而不是之后。
    /// 因为 ChangeState 内部会同步触发待机状态的 EnterState，
    /// 待机状态可能因同步事件级联而立刻切回本状态，
    /// 如果此时 _isCompleted 还是 false，就会形成无限循环。
    /// </summary>
    private void AttackComplete()
    {
        if (_isCompleted)
            return;

        _isCompleted = true;
        AIObj.LaunchBullet();
        ChangeState(E_BossPlayerStateType.待机);
    }
}
