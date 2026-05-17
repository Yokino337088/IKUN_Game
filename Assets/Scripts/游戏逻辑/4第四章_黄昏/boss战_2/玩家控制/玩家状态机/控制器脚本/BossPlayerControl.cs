using Animancer;
using System;
using System.Collections;
using System.Collections.Generic;
using TangmenFramework;
using Unity.VisualScripting;
using UnityEngine;

public class BossPlayerControl : MonoBehaviour,IBossPlayerFSMObj
{
    [Header("移动速度")]
    public float moveSpeed = 5f;
    [Header("跳跃高度")]
    public float jumpForce = 5f;

    [Header("普攻冷却时间（秒）")]
    public float attackCooldown = 0.5f;
    [Header("技能冷却时间（秒）")]
    public float skillCooldown = 2f;
    [Header("大招冷却时间（秒）")]
    public float ultimateCooldown = 5f;

    [SerializeField]
    [Header("Animancer组件引用")]
    private AnimancerComponent animancer;

    [SerializeField]
    [Header("动画配置")]
    private BossPlayerAnimationSO animationSO;

    [SerializeField]
    [Header("普攻子弹")]
    private GameObject BulletObj;

    [SerializeField]
    [Header("技能子弹")]
    private GameObject MissileObj;

    [SerializeField]
    [Header("弹幕管理器")]
    private BulletCommentMgr commentMgr;

    [SerializeField]
    [Header("大招弹幕数量")]
    private int ultimateCommentCount = 30;

    [SerializeField]
    [Header("地面层级")]
    private LayerMask groundLayerMask;

    [SerializeField]
    [Header("弹幕左边的发射点")]
    private Transform leftPoint;

    [SerializeField]
    [Header("弹幕右边的发射点")]
    private Transform rightPoint;

    [SerializeField]
    [Header("移动停止死区阈值，低于此值视为停止移动")]
    [Range(0.01f, 0.5f)]
    private float stopThreshold = 0.1f;

    //护盾特效组件引用
    [SerializeField]
    private ShieldEffect shieldEffect;

    private BossPlayerData playerData;

    private float _attackCooldownTimer;
    private float _skillCooldownTimer;
    private float _ultimateCooldownTimer;


    private Vector2 moveDir;

    /// <summary>
    /// 记录上一次触发移动事件的帧数，防止同一帧内重复触发开始/停止移动事件
    /// </summary>
    private int _lastMoveEventFrame = -1;

    /// <summary>
    /// 记录上一次触发攻击/技能/跳跃事件的帧数，防止同一帧内重复触发多个动作事件（如攻击和跳跃同时按下时只触发一个事件）
    /// </summary>
    private int _lastActionEventFrame = -1;

    private InputConfig inputConfig;

    private Rigidbody2D rb;

    private SpriteRenderer spriteRenderer;

    private BossPlayerStateMachine stateMachine;

    AnimancerComponent IAnimancerFSMObj.Animancer => animancer;

    Vector2 IBossPlayerFSMObj.PlayerMoveDir => moveDir;

    

    /// <summary>
    /// 接口实现，获取对应的动画片段
    /// </summary>
    /// <param name="stateType"></param>
    /// <returns></returns>
    AnimationClip IBossPlayerFSMObj.GetAnimationClip(E_BossPlayerStateType stateType)
    {
        switch (stateType)
        {
            case E_BossPlayerStateType.待机:
                return animationSO.idle;
            case E_BossPlayerStateType.移动:
                return animationSO.walk;
            case E_BossPlayerStateType.跳跃:
                return animationSO.jump;
            case E_BossPlayerStateType.下落:
                return animationSO.fall;
            case E_BossPlayerStateType.技能:
                return animationSO.skill;
            case E_BossPlayerStateType.大招:
                return animationSO.ultimate;
            case E_BossPlayerStateType.普攻:
                return animationSO.attack;
            default: 
                return null;
        }
    }



    private void PlayerMove()
    {
        if (Mathf.Abs(moveDir.x) > stopThreshold && stateMachine.CurrentStateType != E_BossPlayerStateType.技能)
        {
            if(stateMachine.CurrentStateType == E_BossPlayerStateType.普攻)
                transform.Translate(moveSpeed / 2 * moveDir * Time.deltaTime);
            else
                transform.Translate(moveSpeed * moveDir * Time.deltaTime);
        }
            
    }

    void Start()
    {
        AddInputBinding();
        RegisterEvent();
        RegisterState();
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        BossLevelMgr.Instance.RegisterPlayer(this);
        playerData = new BossPlayerData();
    }

    void Update()
    {
        FlipPlayer();

        
        PlayerMove();

        UpdateCooldowns();

        stateMachine.UpdateState();
    }

    private void UpdateCooldowns()
    {
        if (_attackCooldownTimer > 0)
            _attackCooldownTimer -= Time.deltaTime;
        if (_skillCooldownTimer > 0)
            _skillCooldownTimer -= Time.deltaTime;
        if (_ultimateCooldownTimer > 0)
            _ultimateCooldownTimer -= Time.deltaTime;
    }

    public bool CanAttack() => _attackCooldownTimer <= 0;
    public bool CanUseSkill() => _skillCooldownTimer <= 0;
    public bool CanUseUltimate() => _ultimateCooldownTimer <= 0;

    public void StartAttackCooldown() => _attackCooldownTimer = attackCooldown;
    public void StartSkillCooldown() => _skillCooldownTimer = skillCooldown;
    public void StartUltimateCooldown() => _ultimateCooldownTimer = ultimateCooldown;

    // 提供冷却计时器的公开访问，供UI面板使用
    public float SkillCooldownTimer => _skillCooldownTimer;
    public float UltimateCooldownTimer => _ultimateCooldownTimer;

    private void OnDestroy()
    {
        UnRegisterEvent();
    }

    private void AddInputBinding()
    {
        inputConfig = new InputConfig();
        inputConfig.AddKeyboardBinding(E_EventType.E_Input_Jump, KeyCode.W);
        inputConfig.AddKeyboardBinding(E_EventType.E_Input_Attack, KeyCode.J);
        inputConfig.AddKeyboardBinding(E_EventType.E_Input_Skill, KeyCode.K);
        inputConfig.AddKeyboardBinding(E_EventType.E_Input_Ultimate, KeyCode.Space);

        inputConfig.ApplyToInputMgr();
    }

    private void RegisterEvent()
    {
        EventCenter.Instance.AddEventListener<float>(E_EventType.E_Input_Horizontal, OnHorizontalInput);
       
        EventCenter.Instance.AddEventListener(E_EventType.E_Input_Jump, OnJumpPressed);
        EventCenter.Instance.AddEventListener(E_EventType.E_Input_Attack, OnAttackPressed);
        EventCenter.Instance.AddEventListener(E_EventType.E_Input_Skill, OnSkillPressed);
        EventCenter.Instance.AddEventListener(E_EventType.E_Input_Ultimate, OnUltimatePressed);

        EventCenter.Instance.AddEventListener<int>(MyEventTypeString.玩家受伤事件, OnPlayerGetDamage);
    }

    private void UnRegisterEvent()
    {
        EventCenter.Instance.RemoveEventListener<float>(E_EventType.E_Input_Horizontal, OnHorizontalInput);

        EventCenter.Instance.RemoveEventListener(E_EventType.E_Input_Jump, OnJumpPressed);
        EventCenter.Instance.RemoveEventListener(E_EventType.E_Input_Attack, OnAttackPressed);
        EventCenter.Instance.RemoveEventListener(E_EventType.E_Input_Skill, OnSkillPressed);
        EventCenter.Instance.RemoveEventListener(E_EventType.E_Input_Ultimate, OnUltimatePressed);

        EventCenter.Instance.RemoveEventListener<int>(MyEventTypeString.玩家受伤事件, OnPlayerGetDamage);
    }

    private void RegisterState()
    {
        stateMachine = new BossPlayerStateMachine(this);
        stateMachine.AddState<BossPlayerIdleState>(E_BossPlayerStateType.待机);
        stateMachine.AddState<BossPlayerMoveState>(E_BossPlayerStateType.移动);
        stateMachine.AddState<BossPlayerAttackState>(E_BossPlayerStateType.普攻);
        stateMachine.AddState<BossPlayerJumpState>(E_BossPlayerStateType.跳跃);
        stateMachine.AddState<BossPlayerFallState>(E_BossPlayerStateType.下落);
        stateMachine.AddState<BossPlayerSkillState>(E_BossPlayerStateType.技能);
        stateMachine.AddState<BossPlayerUltimateState>(E_BossPlayerStateType.大招);
        stateMachine.ChangeState(E_BossPlayerStateType.待机);
    }

    /// <summary>
    /// 水平输入事件处理方法，根据输入值更新 moveDir.x，并触发玩家开始移动或停止移动事件（如果状态发生变化）。
    /// </summary>
    /// <remarks>记录调用前的移动状态并调用 CheckMoveStateChange 以在状态变化时处理相应逻辑。</remarks>
    /// <param name="value">水平输入值，用于设置 moveDir.x。</param>
    private void OnHorizontalInput(float value)
    {
        bool wasMoving = Mathf.Abs(moveDir.x) > stopThreshold;
        moveDir.x = value;
        CheckMoveStateChange(wasMoving);
    }

    

    private void CheckMoveStateChange(bool wasMoving)
    {
        bool isMoving = Mathf.Abs(moveDir.x) > stopThreshold;

        // 保护①：如果移动状态根本没变，不触发任何事件
        if (wasMoving == isMoving)
            return;

        //保护②：同一帧内只允许触发一次移动事件，阻断级联循环
        if (Time.frameCount == _lastMoveEventFrame)
            return;

        //记录当前帧数，防止同一帧内重复触发事件
        _lastMoveEventFrame = Time.frameCount;

        if (isMoving)
            EventCenter.Instance.EventTrigger(MyEventTypeString.玩家开始移动事件);
        else
            EventCenter.Instance.EventTrigger(MyEventTypeString.玩家停止移动事件);
    }

    /// <summary>
    /// 根据水平方向的移动向量设置 SpriteRenderer 的 flipX 属性；向右时为 false，向左时为 true。
    /// </summary>
    /// <remarks>当水平分量为 0 时不改变翻转状态。假定 spriteRenderer 和 moveDir 已初始化并可用。</remarks>
    private void FlipPlayer()
    {
        if (moveDir.x > 0)
            spriteRenderer.flipX = false;
        else if (moveDir.x < 0)
            spriteRenderer.flipX = true;
    }

    private void OnJumpPressed()
    {
        //保护：同一帧内只允许触发一次动作事件，阻断级联循环（如攻击和跳跃同时按下时只触发一个事件）
        if (Time.frameCount == _lastActionEventFrame)
            return;
        _lastActionEventFrame = Time.frameCount;
        EventCenter.Instance.EventTrigger(MyEventTypeString.玩家跳跃事件);
    }

    private void OnAttackPressed()
    {
        //保护：同一帧内只允许触发一次动作事件，阻断级联循环（如攻击和跳跃同时按下时只触发一个事件）
        if (Time.frameCount == _lastActionEventFrame)
            return;
        
        //冷却时间检查
        if (!CanAttack())
            return;
            
        _lastActionEventFrame = Time.frameCount;
        EventCenter.Instance.EventTrigger(MyEventTypeString.玩家普攻事件);
    }

    private void OnSkillPressed()
    {
        //保护：同一帧内只允许触发一次动作事件，阻断级联循环（如攻击和跳跃同时按下时只触发一个事件）
        if (Time.frameCount == _lastActionEventFrame)
            return;
        
        //冷却时间检查
        if (!CanUseSkill())
            return;
            
        _lastActionEventFrame = Time.frameCount;
        EventCenter.Instance.EventTrigger(MyEventTypeString.玩家技能事件);
    }

    private void OnUltimatePressed()
    {
        //保护：同一帧内只允许触发一次动作事件，阻断级联循环（如攻击和跳跃同时按下时只触发一个事件）
        if (Time.frameCount == _lastActionEventFrame)
            return;
        
        //冷却时间检查
        if (!CanUseUltimate())
            return;
            
        _lastActionEventFrame = Time.frameCount;
        EventCenter.Instance.EventTrigger(MyEventTypeString.玩家大招事件);
    }

    void IBossPlayerFSMObj.LaunchBullet()
    {
        GameObject bulletObj = GOPoolMgr.Instance.GetObj(BulletObj);
        if (bulletObj.TryGetComponent<BossPlayerBullet>(out var bullet))
        {
            bullet.transform.position = spriteRenderer.flipX ? leftPoint.position : rightPoint.position;
            bullet.Init(15f, 0.5f);
            bullet.SetDamageParams(40, 70, 0.35f, 1.5f);
        }
    }

    void IBossPlayerFSMObj.LaunchMissile()
    {
        GameObject bulletObj = GOPoolMgr.Instance.GetObj(MissileObj);
        if (bulletObj.TryGetComponent<BossPlayerBullet>(out var bullet))
        {
            bullet.transform.position = spriteRenderer.flipX ? leftPoint.position : rightPoint.position;
            bullet.Init(12f, 0.7f);
            bullet.SetDamageParams(200, 300, 0.5f, 1.5f);
        }
    }

    void IBossPlayerFSMObj.SummoningBulletComments()
    {
        if (commentMgr != null)
        {
            commentMgr.SpawnComments(ultimateCommentCount);
        }
        else
        {
            LogSystem.Error("BulletCommentMgr未绑定，无法生成弹幕");
        }
    }

    void IBossPlayerFSMObj.DoPlayerJump()
    {
        //给玩家施加一个向上的力，使其跳跃
        rb.AddForce(transform.up * jumpForce,ForceMode2D.Impulse);
    }

    void IBossPlayerFSMObj.StartAttackCooldown()
    {
        StartAttackCooldown();
    }

    void IBossPlayerFSMObj.StartSkillCooldown()
    {
        StartSkillCooldown();
    }

    void IBossPlayerFSMObj.StartUltimateCooldown()
    {
        StartUltimateCooldown();
    }

    private bool IsInLayerMask(int layer, LayerMask mask)
    {
        return ((1 << layer) & mask) != 0;
    }

    private bool hasCreateShield = false;

    /// <summary>
    /// 最近一次被击中的世界坐标（用于护盾波纹定位）
    /// </summary>
    private Vector3 _lastHitPosition;

    /// <summary>
    /// 玩家受伤时处理的回调
    /// </summary>
    private void OnPlayerGetDamage(int hp)
    {
        //检查是否是最后一滴血
        if (playerData.NowHp == 1)
        {
            if (!hasCreateShield)//如果还没有创建过护盾特效，就创建一个护盾特效对象并将其作为玩家的子对象，以实现护盾效果
            {
                GameObject shieldObj = Instantiate(shieldEffect.gameObject, transform.position, Quaternion.identity);
                shieldObj.transform.SetParent(transform);

                // 将引用指向实例化的对象，后续 PlayHitEffect 才能作用在正确的实例上
                shieldEffect = shieldObj.GetComponent<ShieldEffect>();

                hasCreateShield = true;
            }
            else//已经生成护盾特效的话，那就去设置护盾特效的材质属性，让它显示出受伤状态
            {
                if (shieldEffect != null)
                    shieldEffect.PlayHitEffect(_lastHitPosition);
            }
            //播放格挡音效
            MusicMgr.Instance.PlaySoundSafe(MyAssetBundleName.第四章音效包, "格挡");
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        //检测到玩家与地面碰撞，并且当前状态是下落状态，触发玩家落地事件
        if (IsInLayerMask(collision.gameObject.layer, groundLayerMask) && stateMachine.CurrentStateType == E_BossPlayerStateType.下落)
        {
            EventCenter.Instance.EventTrigger(MyEventTypeString.玩家落地事件);
        }
        

    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("boss子弹"))
        {
            
            // 记录碰撞点的世界坐标，传递给护盾波纹
            _lastHitPosition = collision.transform.position;
            playerData.GetDamage();
        }
    }

}
