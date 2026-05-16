# Boss AI 设计文档 - I Wanna 风格浮空 Boss

## 1. 需求分析

### 1.1 核心特性
- **浮空Boss**：Boss 始终悬浮在空中，不接触地面
- **三形态切换**：根据血量阶段切换不同形态
- **I Wanna 风格**：高难度、精确躲避、弹幕式攻击
- **有限状态机**：使用 FSM 管理 Boss 行为逻辑

### 1.2 动画资源分析

| 形态 | 文件夹路径 | 序列帧数量 | 设计用途 |
|------|-----------|-----------|----------|
| 道理形态1 | `Assets/艺术资产/第四章/boss/道理形态1/` | 4帧 | 第一阶段（满血~60%）|
| 道理形态2 | `Assets/艺术资产/第四章/boss/道理形态2/` | 4帧 | 第二阶段（60%~30%）|
| 陶喆形态 | `Assets/艺术资产/第四章/boss/陶喆形态/` | 9帧 | 第三阶段（30%以下）|

### 1.3 设计目标
- 清晰的状态划分和转换逻辑
- 高度可扩展的状态机架构
- 支持状态优先级和中断机制
- 完善的动画状态同步

---

## 2. 状态机设计

### 2.1 状态总览

```
┌─────────────────────────────────────────────────────────────────┐
│                        Boss FSM                                │
├─────────────────────────────────────────────────────────────────┤
│  Idle (待机)                                                    │
│     ↓ 玩家进入范围                                               │
│  Alert (警戒)                                                    │
│     ↓ 确认攻击                                                  │
│  Attack (攻击) ←──────────────────────┐                         │
│     ├─ BulletPattern1 (弹幕模式1)      │                         │
│     ├─ BulletPattern2 (弹幕模式2)      │                         │
│     ├─ LaserAttack (激光攻击)          │                         │
│     └─ ChargeAttack (蓄力攻击)         │                         │
│           ↓ 攻击完成                                            │
│  Recover (恢复) ──────────────────────┘                         │
│     ↓ 恢复完成                                                  │
│  Idle (待机)                                                    │
│                                                                 │
│  ↓ 形态切换触发条件                                              │
│  ├─ Phase1 → Phase2 → Phase3                                   │
│  └─ 每个阶段有独立的攻击模式池                                   │
└─────────────────────────────────────────────────────────────────┘
```

### 2.2 状态详细定义

#### 2.2.1 Idle（待机状态）

| 属性 | 说明 |
|------|------|
| **状态ID** | `BossState.Idle` |
| **触发条件** | Boss 未检测到玩家或战斗结束 |
| **行为** | 缓慢漂浮动画，轻微左右移动 |
| **动画** | 对应形态的待机帧循环 |
| **转换条件** | 玩家进入攻击范围 → Alert |

#### 2.2.2 Alert（警戒状态）

| 属性 | 说明 |
|------|------|
| **状态ID** | `BossState.Alert` |
| **触发条件** | 玩家进入攻击范围 |
| **行为** | 停止漂浮，面向玩家，播放警戒动画 |
| **动画** | 特定警戒帧或待机帧加速 |
| **转换条件** | 检测到玩家位置 → Attack |

#### 2.2.3 Attack（攻击状态 - 分层状态机）

**设计思想**：将攻击状态设计为**分层状态（Hierarchical State）**，攻击状态本身作为父状态，管理多个攻击模式子状态。使用项目中的 `HierarchicalState` 和 `HierarchicalStateMachine` 实现。

**分层结构**：

```
Attack（父状态）
└── 子状态机（Child State Machine）
    ├── BulletPattern1（圆形散射弹幕）
    ├── BulletPattern2（螺旋弹幕）
    ├── LaserAttack（直线激光追踪）
    ├── ChargeAttack（蓄力攻击）
    └── TeleportAttack（瞬移攻击）
```

**攻击模式子状态定义**：

| 子状态 | 描述 | 适用阶段 | 优先级 |
|--------|------|----------|--------|
| **BulletPattern1** | 圆形散射弹幕，360°均匀发射8~12发子弹 | Phase1 | 低 |
| **BulletPattern2** | 螺旋弹幕，持续旋转发射子弹 | Phase1/Phase2 | 中 |
| **LaserAttack** | 直线激光追踪，延迟0.5s追踪玩家位置 | Phase2/Phase3 | 中 |
| **ChargeAttack** | 蓄力2秒后发射24~32发大范围弹幕 | Phase3 | 高 |
| **TeleportAttack** | 瞬移到玩家附近后立即攻击，冷却5s | Phase3 | 高 |

**分层状态机优势**：

1. **职责分离**：父状态（Attack）处理攻击状态的进入/退出逻辑，子状态专注于具体攻击模式
2. **可扩展性**：新增攻击模式只需添加子状态，无需修改父状态逻辑
3. **状态复用**：同一攻击模式可在多个阶段复用
4. **优先级管理**：父状态可根据当前阶段和Boss状态选择合适的子状态

**攻击状态转换流程图**：

```
进入 Attack 状态
    ↓
父状态初始化（播放攻击动画、设置攻击参数）
    ↓
根据当前阶段选择默认子状态
    ↓
进入子状态（执行具体攻击模式）
    ↓
子状态完成
    ↓
父状态判断是否继续攻击
    ├─ 是 → 选择下一个攻击模式子状态
    └─ 否 → 退出 Attack 状态 → 进入 Recover 状态
```

**父状态职责**：
- 进入攻击状态时的通用逻辑（播放攻击动画）
- 根据当前阶段和血量选择攻击模式
- 管理攻击间隔和冷却时间
- 判断是否需要转换到其他状态
- 狂暴模式下跳过恢复状态，直接选择下一个攻击模式

**子状态职责**：
- 实现具体攻击模式的逻辑
- 处理攻击模式的开始、执行、结束
- 通知父状态攻击完成

#### 2.2.4 Recover（恢复状态）

| 属性 | 说明 |
|------|------|
| **状态ID** | `BossState.Recover` |
| **触发条件** | 攻击动作完成 |
| **行为** | 短暂停顿，恢复漂浮状态 |
| **动画** | 恢复帧或待机帧 |
| **转换条件** | 恢复时间结束 → Idle/Alert |

#### 2.2.5 PhaseChange（形态切换状态）

| 属性 | 说明 |
|------|------|
| **状态ID** | `BossState.PhaseChange` |
| **触发条件** | 血量达到阶段阈值 |
| **行为** | 播放形态切换动画，无敌帧 |
| **动画** | 形态切换特效 |
| **转换条件** | 切换动画完成 → Idle |

---

## 3. 阶段设计

### 3.1 阶段划分

| 阶段 | 血量范围 | 形态 | 攻击模式 | 难度 |
|------|---------|------|----------|------|
| Phase 1 | 100% ~ 60% | 道理形态1 | 弹幕Pattern1、Pattern2 | 低 |
| Phase 2 | 60% ~ 30% | 道理形态2 | 弹幕Pattern2、激光 | 中 |
| Phase 3 | 30% ~ 0% | 陶喆形态 | 激光、蓄力、瞬移 | 高 |

### 3.2 阶段特性

#### Phase 1 - 道理形态1
- 攻击频率较低
- 弹幕密度较小
- 主要使用圆形散射和简单螺旋

#### Phase 2 - 道理形态2
- 攻击频率提升
- 弹幕密度增加
- 加入激光追踪攻击
- Boss移动速度提升

#### Phase 3 - 陶喆形态
- 攻击频率最高
- 弹幕密度最大
- 加入蓄力攻击和瞬移
- 无敌帧减少

---

## 4. 技术实施方案

### 4.1 代码结构

```
Assets/Scripts/游戏逻辑/第四章_黄昏/Boss/
├── BossAI/
│   ├── BossFSM.cs              # 状态机核心控制器（使用框架的 StateMachine）
│   ├── BossState.cs            # 状态枚举定义
│   └── States/
│       ├── BossIdleState.cs    # 待机状态（BaseState）
│       ├── BossAlertState.cs   # 警戒状态（BaseState）
│       ├── BossAttackState.cs  # 攻击状态（HierarchicalState - 父状态）
│       ├── BossRecoverState.cs # 恢复状态（BaseState）
│       ├── BossPhaseChangeState.cs # 形态切换状态（BaseState）
│       ├── BossEnrageState.cs  # 狂暴状态（HierarchicalState - 父状态）
│       └── AttackPatterns/     # 攻击模式子状态（Attack的子状态）
│           ├── BulletPattern1State.cs    # 圆形散射弹幕
│           ├── BulletPattern2State.cs    # 螺旋弹幕
│           ├── LaserAttackState.cs       # 激光攻击
│           ├── ChargeAttackState.cs      # 蓄力攻击
│           └── TeleportAttackState.cs    # 瞬移攻击
├── BossController.cs           # Boss 主控脚本（实现 IFSMObj 接口）
├── BossData.cs                 # Boss 配置数据
└── BulletSystem/               # 弹幕系统（单独模块）
```

### 4.2 核心类设计

#### 4.2.1 状态枚举定义

```csharp
public enum BossStateType
{
    // 顶层状态
    Idle,           // 待机
    Alert,          // 警戒
    Attack,         // 攻击（父状态）
    Recover,        // 恢复
    PhaseChange,    // 形态切换
    Enrage,         // 狂暴（父状态）
    
    // 攻击子状态（Attack的子状态）
    Attack_BulletPattern1,     // 圆形散射弹幕
    Attack_BulletPattern2,     // 螺旋弹幕
    Attack_Laser,              // 激光攻击
    Attack_Charge,             // 蓄力攻击
    Attack_Teleport,           // 瞬移攻击
    
    // 狂暴子状态（Enrage的子状态，使用强化版攻击）
    Enrage_BulletPattern1,     // 强化圆形散射
    Enrage_BulletPattern2,     // 强化螺旋弹幕
    Enrage_Laser,              // 强化激光
    Enrage_Charge,             // 强化蓄力
    Enrage_Teleport            // 强化瞬移
}

public enum BossPhase
{
    Phase1,     // 100% ~ 60%
    Phase2,     // 60% ~ 30%
    Phase3,     // 30% ~ 10%
    Phase4      // 10% ~ 0%（狂暴）
}
```

#### 4.2.2 BossController（主控脚本 - 实现 IFSMObj 接口）

```csharp
public class BossController : MonoBehaviour, IFSMObj
{
    // 当前形态
    public BossPhase currentPhase;
    
    // 当前血量百分比
    public float healthPercent;
    
    // 状态机（使用框架的 GenericStateMachine）
    private StateMachine<BossStateType, IFSMObj> stateMachine;
    
    // 动画控制器
    private Animator animator;
    
    // 刚体（用于浮空移动）
    private Rigidbody2D rb;
    
    // 攻击模式池（按阶段配置）
    public List<AttackPattern> phase1Attacks;
    public List<AttackPattern> phase2Attacks;
    public List<AttackPattern> phase3Attacks;
    public List<AttackPattern> phase4Attacks; // 狂暴模式攻击
    
    // 攻击参数
    public float attackInterval = 2f;        // 攻击间隔
    public float bulletMultiplier = 1f;      // 子弹数量倍率
    public float bulletSpeedMultiplier = 1f; // 子弹速度倍率
    
    // 是否处于狂暴模式
    public bool isEnraged => currentPhase == BossPhase.Phase4;
    
    private void Awake()
    {
        // 初始化状态机
        InitializeStateMachine();
    }
    
    private void InitializeStateMachine()
    {
        // 创建根状态机
        stateMachine = new GenericStateMachine<BossStateType, IFSMObj>(this);
        
        // 添加顶层状态
        stateMachine.AddState<BossIdleState>(BossStateType.Idle);
        stateMachine.AddState<BossAlertState>(BossStateType.Alert);
        stateMachine.AddState<BossAttackState>(BossStateType.Attack);
        stateMachine.AddState<BossRecoverState>(BossStateType.Recover);
        stateMachine.AddState<BossPhaseChangeState>(BossStateType.PhaseChange);
        stateMachine.AddState<BossEnrageState>(BossStateType.Enrage);
        
        // 设置初始状态
        stateMachine.ChangeState(BossStateType.Idle);
    }
    
    private void Update()
    {
        // 更新状态机
        stateMachine?.UpdateState();
    }
    
    private void FixedUpdate()
    {
        // 物理更新状态机
        stateMachine?.FixedUpdateState();
    }
}
```

#### 4.2.3 BossAttackState（攻击状态 - 分层状态实现）

```csharp
public class BossAttackState : HierarchicalState<BossStateType, IFSMObj>
{
    // 攻击间隔计时器
    private float attackTimer;
    
    // 当前选中的攻击模式
    private BossStateType currentAttackPattern;
    
    // 攻击模式池（根据阶段筛选）
    private List<BossStateType> availableAttacks;
    
    public override BossStateType StateType => BossStateType.Attack;
    
    public BossAttackState(StateMachine<BossStateType, IFSMObj> machine) 
        : base(machine)
    {
        // 创建子状态机
        InitializeChildStateMachine();
    }
    
    private void InitializeChildStateMachine()
    {
        // 创建分层状态机作为子状态机
        var childMachine = new HierarchicalStateMachine<BossStateType, IFSMObj>(
            AIObj, this
        );
        
        // 添加攻击模式子状态
        childMachine.AddState<BulletPattern1State>(BossStateType.Attack_BulletPattern1);
        childMachine.AddState<BulletPattern2State>(BossStateType.Attack_BulletPattern2);
        childMachine.AddState<LaserAttackState>(BossStateType.Attack_Laser);
        childMachine.AddState<ChargeAttackState>(BossStateType.Attack_Charge);
        childMachine.AddState<TeleportAttackState>(BossStateType.Attack_Teleport);
        
        // 设置子状态机
        SetChildStateMachine(childMachine);
    }
    
    public override void EnterState()
    {
        base.EnterState();
        
        // 获取当前阶段的可用攻击模式
        RefreshAvailableAttacks();
        
        // 选择第一个攻击模式作为默认子状态
        if (availableAttacks.Count > 0)
        {
            currentAttackPattern = availableAttacks[0];
            DefaultChildStateType = currentAttackPattern;
        }
        
        // 播放攻击动画
        GetBossController().animator.SetTrigger("IsAttacking");
        
        Debug.Log("进入攻击状态");
    }
    
    public override void QuitState()
    {
        base.QuitState();
        
        // 重置攻击计时器
        attackTimer = 0f;
        
        Debug.Log("退出攻击状态");
    }
    
    public override void UpdateState()
    {
        base.UpdateState();
        
        // 检查是否需要转换到其他状态
        CheckStateTransition();
    }
    
    private void RefreshAvailableAttacks()
    {
        var boss = GetBossController();
        availableAttacks = new List<BossStateType>();
        
        // 根据当前阶段选择可用攻击模式
        switch (boss.currentPhase)
        {
            case BossPhase.Phase1:
                availableAttacks.Add(BossStateType.Attack_BulletPattern1);
                availableAttacks.Add(BossStateType.Attack_BulletPattern2);
                break;
            case BossPhase.Phase2:
                availableAttacks.Add(BossStateType.Attack_BulletPattern2);
                availableAttacks.Add(BossStateType.Attack_Laser);
                break;
            case BossPhase.Phase3:
                availableAttacks.Add(BossStateType.Attack_Laser);
                availableAttacks.Add(BossStateType.Attack_Charge);
                availableAttacks.Add(BossStateType.Attack_Teleport);
                break;
        }
    }
    
    /// <summary>
    /// 当子攻击模式完成时调用
    /// </summary>
    public void OnAttackPatternComplete()
    {
        var boss = GetBossController();
        
        // 狂暴模式：立即选择下一个攻击模式
        if (boss.isEnraged)
        {
            SelectNextAttackPattern();
            ChildStateMachine.ChangeState(currentAttackPattern);
            return;
        }
        
        // 非狂暴模式：转换到恢复状态
        ChangeState(BossStateType.Recover);
    }
    
    private void SelectNextAttackPattern()
    {
        // 随机选择下一个攻击模式（避免重复）
        var currentIndex = availableAttacks.IndexOf(currentAttackPattern);
        var nextIndex = (currentIndex + 1) % availableAttacks.Count;
        
        // 有概率跳转到随机攻击模式
        if (UnityEngine.Random.value > 0.5f)
        {
            nextIndex = UnityEngine.Random.Range(0, availableAttacks.Count);
        }
        
        currentAttackPattern = availableAttacks[nextIndex];
    }
    
    private void CheckStateTransition()
    {
        var boss = GetBossController();
        
        // 检查狂暴模式触发
        if (boss.healthPercent <= 0.1f && !boss.isEnraged)
        {
            ChangeState(BossStateType.Enrage);
        }
        
        // 检查玩家是否离开攻击范围（仅非狂暴模式）
        if (!boss.isEnraged && !IsPlayerInRange())
        {
            ChangeState(BossStateType.Idle);
        }
    }
    
    private bool IsPlayerInRange()
    {
        // 检测玩家是否在攻击范围内
        // 实现省略...
        return true;
    }
    
    private BossController GetBossController()
    {
        return AIObj as BossController;
    }
}
```

#### 4.2.4 攻击模式子状态示例（BulletPattern1State）

```csharp
public class BulletPattern1State : BaseState<BossStateType, IFSMObj>
{
    // 攻击持续时间
    private float attackDuration = 2f;
    
    // 攻击计时器
    private float attackTimer;
    
    // 是否正在发射子弹
    private bool isFiring;
    
    // 子弹发射间隔
    private float fireInterval = 0.1f;
    
    // 上次发射时间
    private float lastFireTime;
    
    // 子弹数量
    private int bulletCount = 12;
    
    // 当前发射的子弹索引
    private int currentBulletIndex;
    
    public override BossStateType StateType => BossStateType.Attack_BulletPattern1;
    
    public BulletPattern1State(StateMachine<BossStateType, IFSMObj> machine) 
        : base(machine) { }
    
    public override void EnterState()
    {
        attackTimer = 0f;
        isFiring = true;
        lastFireTime = 0f;
        currentBulletIndex = 0;
        
        Debug.Log("进入圆形散射弹幕攻击");
    }
    
    public override void QuitState()
    {
        isFiring = false;
        Debug.Log("退出圆形散射弹幕攻击");
    }
    
    public override void UpdateState()
    {
        attackTimer += Time.deltaTime;
        
        // 检查攻击是否完成
        if (attackTimer >= attackDuration)
        {
            // 通知父状态攻击完成
            NotifyParentAttackComplete();
            return;
        }
        
        // 发射子弹
        if (isFiring && attackTimer >= lastFireTime + fireInterval)
        {
            FireBullet();
            lastFireTime = attackTimer;
        }
    }
    
    private void FireBullet()
    {
        if (currentBulletIndex >= bulletCount)
        {
            isFiring = false;
            return;
        }
        
        var boss = GetBossController();
        
        // 计算子弹角度（360度均匀分布）
        float angle = (360f / bulletCount) * currentBulletIndex;
        Vector2 direction = new Vector2(
            Mathf.Cos(angle * Mathf.Deg2Rad),
            Mathf.Sin(angle * Mathf.Deg2Rad)
        );
        
        // 创建子弹（使用弹幕系统）
        BulletSystem.Instance.SpawnBullet(
            boss.transform.position,
            direction,
            speed: 5f * boss.bulletSpeedMultiplier,
            damage: 1
        );
        
        currentBulletIndex++;
    }
    
    private void NotifyParentAttackComplete()
    {
        // 获取父状态并通知攻击完成
        var parentState = stateMachine as HierarchicalStateMachine<BossStateType, IFSMObj>;
        if (parentState?.ParentState is BossAttackState attackState)
        {
            attackState.OnAttackPatternComplete();
        }
    }
    
    private BossController GetBossController()
    {
        return AIObj as BossController;
    }
}
```

### 4.3 状态转换表

| 当前状态 | 触发条件 | 目标状态 |
|----------|---------|----------|
| Idle | 玩家进入范围 | Alert |
| Alert | 检测到玩家位置 | Attack |
| Attack | 攻击完成 | Recover |
| Recover | 恢复完成 | Idle |
| *任意* | 血量达到阶段阈值 | PhaseChange |
| PhaseChange | 切换动画完成 | Idle |

---

## 5. 动画配置

### 5.1 动画剪辑设置

每个形态需要创建以下动画剪辑：

| 动画名称 | 帧数范围 | 用途 |
|----------|---------|------|
| `Idle_1` | 道理形态1全帧 | Phase1 待机 |
| `Idle_2` | 道理形态2全帧 | Phase2 待机 |
| `Idle_3` | 陶喆形态全帧 | Phase3 待机 |
| `Alert_1` | 道理形态1第1帧 | Phase1 警戒 |
| `Alert_2` | 道理形态2第1帧 | Phase2 警戒 |
| `Alert_3` | 陶喆形态第1帧 | Phase3 警戒 |
| `Attack_1` | 道理形态1攻击帧 | Phase1 攻击 |
| `Attack_2` | 道理形态2攻击帧 | Phase2 攻击 |
| `Attack_3` | 陶喆形态攻击帧 | Phase3 攻击 |
| `PhaseChange` | 切换特效帧 | 形态切换 |

### 5.2 Animator Controller 状态机

```
Animator Controller: BossAnimator
├─ Any State → Idle
│   ├─ Parameter: Phase = 1 → Idle_1
│   ├─ Parameter: Phase = 2 → Idle_2
│   └─ Parameter: Phase = 3 → Idle_3
├─ Idle → Alert (Trigger: IsAlert)
├─ Alert → Attack (Trigger: IsAttacking)
├─ Attack → Recover (Trigger: AttackEnd)
├─ Recover → Idle (Trigger: RecoverEnd)
└─ Any State → PhaseChange (Trigger: PhaseChange)
    └─ PhaseChange → Idle (Trigger: PhaseChangeEnd)
```

---

## 6. 攻击模式设计

### 6.1 BulletPattern1（圆形散射弹幕）

| 属性 | 值 |
|------|-----|
| **子弹数量** | 8~12 发 |
| **扩散角度** | 360° |
| **发射间隔** | 0.1s |
| **子弹速度** | 5~7 m/s |
| **适用阶段** | Phase1 |

**移动逻辑**：
- **攻击前**：保持当前位置，不移动
- **攻击中**：保持稳定位置，专注于发射弹幕
- **攻击后**：保持原位，等待状态转换

```csharp
public class BulletPattern1State : BaseState<BossStateType, IFSMObj>
{
    public override void EnterState()
    {
        var boss = GetBossController();
        
        // 停止所有移动，保持当前位置稳定
        boss.StopCurrentMove();
        
        // 停止浮空动画（攻击时保持稳定）
        boss.StopFloatingAnimation();
        
        // 播放攻击准备动画
        boss.animator.SetTrigger("PrepareAttack");
    }
    
    public override void UpdateState()
    {
        // 发射弹幕逻辑...
        
        // 攻击完成后通知父状态
        if (attackComplete)
        {
            NotifyParentAttackComplete();
        }
    }
    
    public override void QuitState()
    {
        // 恢复浮空动画
        GetBossController().StartFloatingAnimation();
    }
}
```

### 6.2 BulletPattern2（螺旋弹幕）

| 属性 | 值 |
|------|-----|
| **子弹数量** | 持续发射 |
| **旋转速度** | 180°/s |
| **发射间隔** | 0.05s |
| **子弹速度** | 6~8 m/s |
| **适用阶段** | Phase1/Phase2 |

**移动逻辑**：
- **攻击前**：缓慢向玩家方向移动（DOTween）
- **攻击中**：保持移动状态，螺旋弹幕跟随Boss位置
- **攻击后**：继续移动或返回原位

```csharp
public class BulletPattern2State : BaseState<BossStateType, IFSMObj>
{
    private Tweener moveTween;
    
    public override void EnterState()
    {
        var boss = GetBossController();
        
        // 停止浮空动画
        boss.StopFloatingAnimation();
        
        // 向玩家方向缓慢移动
        Vector3 playerPos = FindPlayerPosition();
        Vector3 moveDir = (playerPos - boss.transform.position).normalized;
        Vector3 targetPos = boss.transform.position + moveDir * 2f;
        
        // 使用DOTween平滑移动
        moveTween = boss.transform.DOMove(targetPos, 1f)
            .SetEase(Ease.Linear)
            .SetLoops(-1, LoopType.Yoyo);
    }
    
    public override void UpdateState()
    {
        // 持续发射螺旋弹幕...
        
        if (attackComplete)
        {
            NotifyParentAttackComplete();
        }
    }
    
    public override void QuitState()
    {
        // 停止移动动画
        moveTween?.Kill();
        
        // 恢复浮空动画
        GetBossController().StartFloatingAnimation();
    }
}
```

### 6.3 LaserAttack（激光攻击）

| 属性 | 值 |
|------|-----|
| **激光长度** | 屏幕宽度 |
| **移动速度** | 2~3 m/s |
| **追踪精度** | 延迟0.5s追踪 |
| **持续时间** | 3s |
| **适用阶段** | Phase2/Phase3 |

**移动逻辑**：
- **攻击前**：快速移动到玩家上方（DOTween）
- **攻击中**：沿水平方向左右移动，激光跟随移动轨迹
- **攻击后**：快速返回原位

```csharp
public class LaserAttackState : BaseState<BossStateType, IFSMObj>
{
    private bool isMovingLeft = true;
    private float moveBoundaryLeft;
    private float moveBoundaryRight;
    
    public override void EnterState()
    {
        var boss = GetBossController();
        
        // 停止浮空动画
        boss.StopFloatingAnimation();
        
        // 快速移动到玩家上方
        Vector3 playerPos = FindPlayerPosition();
        Vector3 targetPos = new Vector3(playerPos.x, playerPos.y + 4f, 0);
        
        boss.MoveTo(targetPos, 0.3f, () =>
        {
            // 移动完成后开始攻击
            StartLaserAttack();
        });
        
        // 设置移动边界
        moveBoundaryLeft = boss.InitialPosition.x - 4f;
        moveBoundaryRight = boss.InitialPosition.x + 4f;
    }
    
    public override void UpdateState()
    {
        var boss = GetBossController();
        
        // 水平移动
        float moveSpeed = 2.5f;
        Vector3 currentPos = boss.transform.position;
        
        if (isMovingLeft)
        {
            currentPos.x -= moveSpeed * Time.deltaTime;
            if (currentPos.x <= moveBoundaryLeft)
            {
                isMovingLeft = false;
            }
        }
        else
        {
            currentPos.x += moveSpeed * Time.deltaTime;
            if (currentPos.x >= moveBoundaryRight)
            {
                isMovingLeft = true;
            }
        }
        
        // 使用DOTween实现平滑移动
        boss.transform.DOMoveX(currentPos.x, 0.1f)
            .SetEase(Ease.Linear)
            .SetUpdate(true);
        
        // 检查攻击是否完成
        if (attackComplete)
        {
            NotifyParentAttackComplete();
        }
    }
    
    public override void QuitState()
    {
        // 停止所有移动
        GetBossController().StopCurrentMove();
        
        // 恢复浮空动画
        GetBossController().StartFloatingAnimation();
    }
}
```

### 6.4 ChargeAttack（蓄力攻击）

| 属性 | 值 |
|------|-----|
| **蓄力时间** | 2s |
| **子弹数量** | 24~32 发 |
| **扩散角度** | 360° |
| **子弹速度** | 8~10 m/s |
| **适用阶段** | Phase3 |

**移动逻辑**：
- **蓄力前**：移动到场地边缘位置
- **蓄力中**：保持静止，蓄力动画播放
- **攻击后**：快速退回安全位置

```csharp
public class ChargeAttackState : BaseState<BossStateType, IFSMObj>
{
    private float chargeTimer;
    private bool isCharging = false;
    
    public override void EnterState()
    {
        chargeTimer = 0f;
        isCharging = true;
        
        var boss = GetBossController();
        
        // 停止浮空动画
        boss.StopFloatingAnimation();
        
        // 移动到边缘位置（随机左或右）
        float edgeX = UnityEngine.Random.value > 0.5f 
            ? boss.InitialPosition.x - 4f 
            : boss.InitialPosition.x + 4f;
        Vector3 targetPos = new Vector3(edgeX, boss.InitialPosition.y + 2f, 0);
        
        boss.MoveTo(targetPos, 0.4f, () =>
        {
            // 到达位置后开始蓄力
            StartCharging();
        });
    }
    
    private void StartCharging()
    {
        isCharging = true;
        chargeTimer = 0f;
        
        // 播放蓄力动画
        GetBossController().animator.SetTrigger("Charge");
    }
    
    public override void UpdateState()
    {
        if (isCharging)
        {
            chargeTimer += Time.deltaTime;
            
            // 更新蓄力进度（可以通过动画参数传递）
            float chargePercent = Mathf.Clamp01(chargeTimer / 2f);
            GetBossController().animator.SetFloat("ChargePercent", chargePercent);
            
            // 蓄力完成后发射
            if (chargeTimer >= 2f)
            {
                FireChargeAttack();
                isCharging = false;
            }
        }
        else
        {
            // 攻击完成后等待退出
            if (attackComplete)
            {
                NotifyParentAttackComplete();
            }
        }
    }
    
    private void FireChargeAttack()
    {
        // 发射蓄力弹幕...
        
        // 播放攻击动画
        GetBossController().animator.SetTrigger("FireCharge");
    }
    
    public override void QuitState()
    {
        // 停止所有移动
        GetBossController().StopCurrentMove();
        
        // 恢复浮空动画
        GetBossController().StartFloatingAnimation();
    }
}
```

### 6.5 TeleportAttack（瞬移攻击）

| 属性 | 值 |
|------|-----|
| **瞬移距离** | 5~8 单位 |
| **瞬移冷却** | 5s |
| **攻击延迟** | 0.5s |
| **适用阶段** | Phase3 |

**移动逻辑**：
- **瞬移前**：播放瞬移准备动画
- **瞬移中**：瞬时移动到玩家附近
- **攻击后**：瞬移回安全位置

```csharp
public class TeleportAttackState : BaseState<BossStateType, IFSMObj>
{
    private float teleportDelay = 0.5f;
    private float attackDelay = 0.5f;
    private float timer;
    private TeleportPhase currentPhase;
    
    private enum TeleportPhase
    {
        Preparing,
        Teleporting,
        Attacking,
        Returning
    }
    
    public override void EnterState()
    {
        timer = 0f;
        currentPhase = TeleportPhase.Preparing;
        
        var boss = GetBossController();
        
        // 停止浮空动画
        boss.StopFloatingAnimation();
        
        // 播放瞬移准备动画
        boss.animator.SetTrigger("TeleportPrepare");
        
        // 播放瞬移特效
        PlayTeleportEffect(boss.transform.position);
    }
    
    public override void UpdateState()
    {
        var boss = GetBossController();
        
        switch (currentPhase)
        {
            case TeleportPhase.Preparing:
                timer += Time.deltaTime;
                
                if (timer >= teleportDelay)
                {
                    // 瞬移到玩家附近
                    TeleportToPlayer();
                    currentPhase = TeleportPhase.Attacking;
                    timer = 0f;
                }
                break;
                
            case TeleportPhase.Attacking:
                timer += Time.deltaTime;
                
                // 短暂延迟后攻击
                if (timer >= attackDelay)
                {
                    // 执行攻击
                    ExecuteAttack();
                    currentPhase = TeleportPhase.Returning;
                    timer = 0f;
                }
                break;
                
            case TeleportPhase.Returning:
                timer += Time.deltaTime;
                
                // 攻击完成后瞬移回原位
                if (timer >= 0.3f)
                {
                    TeleportBack();
                    NotifyParentAttackComplete();
                }
                break;
        }
    }
    
    private void TeleportToPlayer()
    {
        var boss = GetBossController();
        var player = FindPlayerPosition();
        
        // 计算瞬移位置（玩家附近但保持安全距离）
        Vector3 direction = (player - boss.transform.position).normalized;
        Vector3 teleportPos = player - direction * 3f; // 在玩家3个单位外
        
        // 限制在战斗区域内
        teleportPos.x = Mathf.Clamp(teleportPos.x, 
            boss.InitialPosition.x - 5f, 
            boss.InitialPosition.x + 5f);
        teleportPos.y = Mathf.Clamp(teleportPos.y, 
            boss.InitialPosition.y - 2f, 
            boss.InitialPosition.y + 4f);
        
        // 隐藏Boss（瞬移特效）
        boss.gameObject.SetActive(false);
        
        // 播放瞬移特效
        PlayTeleportEffect(boss.transform.position);
        
        // 瞬移（瞬时移动）
        boss.TeleportTo(teleportPos);
        
        // 显示Boss
        boss.gameObject.SetActive(true);
        
        // 播放到达特效
        PlayTeleportArrivalEffect(teleportPos);
        
        // 播放瞬移到达动画
        boss.animator.SetTrigger("TeleportArrive");
    }
    
    private void TeleportBack()
    {
        var boss = GetBossController();
        
        // 隐藏Boss
        boss.gameObject.SetActive(false);
        
        // 播放瞬移特效
        PlayTeleportEffect(boss.transform.position);
        
        // 瞬移回初始位置
        boss.TeleportTo(boss.InitialPosition);
        
        // 显示Boss
        boss.gameObject.SetActive(true);
        
        // 播放到达特效
        PlayTeleportArrivalEffect(boss.InitialPosition);
    }
    
    private void ExecuteAttack()
    {
        // 执行瞬移后的攻击...
        GetBossController().animator.SetTrigger("TeleportAttack");
    }
    
    private void PlayTeleportEffect(Vector3 position)
    {
        // 播放瞬移粒子特效
        // 实现省略...
    }
    
    private void PlayTeleportArrivalEffect(Vector3 position)
    {
        // 播放瞬移到达粒子特效
        // 实现省略...
    }
    
    public override void QuitState()
    {
        // 恢复浮空动画
        GetBossController().StartFloatingAnimation();
    }
}
```

---

## 7. 浮空移动实现

### 7.1 浮空逻辑

```csharp
// 在 BossController 中
private void FloatingMovement()
{
    // 正弦波实现上下浮动
    float floatY = Mathf.Sin(Time.time * floatSpeed) * floatAmplitude;
    Vector3 newPos = transform.position;
    newPos.y = baseY + floatY;
    
    // 轻微左右移动
    float moveX = Mathf.Sin(Time.time * moveSpeed) * moveRange;
    newPos.x = baseX + moveX;
    
    transform.position = newPos;
}
```

### 7.2 浮空参数

| 参数 | 默认值 | 说明 |
|------|-------|------|
| `floatSpeed` | 1.0f | 上下浮动速度 |
| `floatAmplitude` | 0.5f | 浮动幅度 |
| `moveSpeed` | 0.5f | 左右移动速度 |
| `moveRange` | 1.0f | 左右移动范围 |

---

## 8. 各状态移动逻辑设计（DOTween实现）

### 8.1 移动逻辑概述

Boss 的移动根据不同状态有不同的行为模式，所有移动均使用 **DOTween** 实现平滑动画效果：

| 状态 | 移动模式 | 移动速度 | 特殊效果 |
|------|----------|----------|----------|
| **Idle** | 浮空漂浮（正弦波） | 低 | 上下浮动 + 轻微左右摆动 |
| **Alert** | 停止移动，面向玩家 | - | 仅旋转朝向玩家 |
| **Attack** | 定点攻击位置移动 | 中等 | 根据攻击模式选择位置 |
| **Recover** | 缓慢退回原位 | 低 | 平滑回到初始位置 |
| **PhaseChange** | 瞬移到新位置 | 瞬时 | 位置突变 + 特效 |
| **Enrage** | 快速移动 | 高 | 攻击间隔短距离瞬移 |

### 8.2 核心移动组件

```csharp
// BossController 中添加移动相关字段
public class BossController : MonoBehaviour, IFSMObj
{
    // 初始位置（作为Idle状态的返回点）
    private Vector3 initialPosition;
    
    // 当前目标位置
    private Vector3 targetPosition;
    
    // 移动速度参数
    public float moveDuration = 0.5f;      // 移动到目标位置的时间
    public float floatSpeed = 1.0f;        // 浮空速度
    public float floatAmplitude = 0.5f;    // 浮空幅度
    public float swaySpeed = 0.5f;         // 左右摇摆速度
    public float swayRange = 1.0f;         // 左右摇摆范围
    
    // DOTween 动画引用
    private Tweener currentMoveTween;
    private Tweener floatTween;
    
    // 移动状态
    private bool isMoving = false;
    
    private void Awake()
    {
        // 记录初始位置
        initialPosition = transform.position;
        targetPosition = initialPosition;
        
        // 初始化浮空动画
        StartFloatingAnimation();
    }
    
    /// <summary>
    /// 开始浮空动画
    /// </summary>
    private void StartFloatingAnimation()
    {
        // 使用 DOTween 实现循环浮动
        floatTween = transform.DOLocalMoveY(initialPosition.y + floatAmplitude, 1f / floatSpeed)
            .SetEase(Ease.InOutSine)
            .SetLoops(-1, LoopType.Yoyo)
            .SetUpdate(true);
    }
    
    /// <summary>
    /// 停止浮空动画
    /// </summary>
    private void StopFloatingAnimation()
    {
        floatTween?.Kill();
        floatTween = null;
    }
    
    /// <summary>
    /// 移动到指定位置
    /// </summary>
    /// <param name="target">目标位置</param>
    /// <param name="duration">移动时间</param>
    /// <param name="onComplete">完成回调</param>
    public void MoveTo(Vector3 target, float duration = 0.5f, Action onComplete = null)
    {
        // 停止当前移动
        StopCurrentMove();
        
        isMoving = true;
        targetPosition = target;
        
        // 使用 DOTween 平滑移动
        currentMoveTween = transform.DOMove(target, duration)
            .SetEase(Ease.OutQuad)
            .OnComplete(() =>
            {
                isMoving = false;
                onComplete?.Invoke();
            })
            .SetUpdate(true);
    }
    
    /// <summary>
    /// 停止当前移动
    /// </summary>
    public void StopCurrentMove()
    {
        currentMoveTween?.Kill();
        currentMoveTween = null;
        isMoving = false;
    }
    
    /// <summary>
    /// 瞬移到指定位置（瞬时移动）
    /// </summary>
    public void TeleportTo(Vector3 target)
    {
        StopCurrentMove();
        transform.position = target;
        targetPosition = target;
    }
    
    /// <summary>
    /// 面向玩家
    /// </summary>
    public void LookAtPlayer()
    {
        Vector3 playerPos = FindObjectOfType<PlayerController>().transform.position;
        Vector3 direction = playerPos - transform.position;
        direction.z = 0; // 2D游戏忽略Z轴
        
        if (direction != Vector3.zero)
        {
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            Quaternion targetRotation = Quaternion.AngleAxis(angle, Vector3.forward);
            
            // 使用 DOTween 平滑旋转
            transform.DORotateQuaternion(targetRotation, 0.3f)
                .SetEase(Ease.OutQuad);
        }
    }
    
    /// <summary>
    /// 恢复到初始位置
    /// </summary>
    public void ReturnToInitialPosition(Action onComplete = null)
    {
        MoveTo(initialPosition, 1f, onComplete);
    }
}
```

### 8.3 各状态移动实现

#### 8.3.1 Idle 状态移动

**移动行为**：持续浮空漂浮，上下浮动 + 轻微左右摆动

```csharp
public class BossIdleState : BaseState<BossStateType, IFSMObj>
{
    public override BossStateType StateType => BossStateType.Idle;
    
    public BossIdleState(StateMachine<BossStateType, IFSMObj> machine) 
        : base(machine) { }
    
    public override void EnterState()
    {
        var boss = GetBossController();
        
        // 确保浮空动画运行
        boss.StartFloatingAnimation();
        
        // 如果不在初始位置，平滑返回
        if (boss.transform.position != boss.InitialPosition)
        {
            boss.ReturnToInitialPosition();
        }
        
        Debug.Log("进入Idle状态 - 开始浮空");
    }
    
    public override void QuitState()
    {
        // 浮空动画继续运行（其他状态可能也需要）
        Debug.Log("退出Idle状态");
    }
    
    public override void UpdateState()
    {
        // Idle状态持续检测玩家进入范围
        CheckPlayerInRange();
    }
    
    private void CheckPlayerInRange()
    {
        var boss = GetBossController();
        
        // 检测玩家是否进入攻击范围
        if (IsPlayerInAttackRange())
        {
            ChangeState(BossStateType.Alert);
        }
    }
    
    private bool IsPlayerInAttackRange()
    {
        // 实现省略...
        return false;
    }
    
    private BossController GetBossController()
    {
        return AIObj as BossController;
    }
}
```

#### 8.3.2 Alert 状态移动

**移动行为**：停止移动，平滑转向玩家

```csharp
public class BossAlertState : BaseState<BossStateType, IFSMObj>
{
    private float alertDuration = 1f;
    private float timer;
    
    public override BossStateType StateType => BossStateType.Alert;
    
    public BossAlertState(StateMachine<BossStateType, IFSMObj> machine) 
        : base(machine) { }
    
    public override void EnterState()
    {
        timer = 0f;
        
        var boss = GetBossController();
        
        // 停止所有移动动画
        boss.StopCurrentMove();
        
        // 保持浮空动画，但停止左右摆动
        // （可选：可以稍微减小浮空幅度表示警戒）
        
        // 面向玩家
        boss.LookAtPlayer();
        
        Debug.Log("进入Alert状态 - 面向玩家");
    }
    
    public override void QuitState()
    {
        Debug.Log("退出Alert状态");
    }
    
    public override void UpdateState()
    {
        timer += Time.deltaTime;
        
        // 保持面向玩家
        GetBossController().LookAtPlayer();
        
        // 警戒一段时间后进入攻击状态
        if (timer >= alertDuration)
        {
            ChangeState(BossStateType.Attack);
        }
    }
    
    private BossController GetBossController()
    {
        return AIObj as BossController;
    }
}
```

#### 8.3.3 Attack 状态移动

**移动行为**：根据攻击模式选择攻击位置，可能移动到特定位置或保持原位

```csharp
public class BossAttackState : HierarchicalState<BossStateType, IFSMObj>
{
    // 攻击位置范围（相对于初始位置）
    public Vector2 attackAreaMin = new Vector2(-5f, 2f);
    public Vector2 attackAreaMax = new Vector2(5f, 5f);
    
    public override BossStateType StateType => BossStateType.Attack;
    
    public BossAttackState(StateMachine<BossStateType, IFSMObj> machine) 
        : base(machine)
    {
        InitializeChildStateMachine();
    }
    
    public override void EnterState()
    {
        base.EnterState();
        
        var boss = GetBossController();
        
        // 停止浮空动画（攻击时保持稳定位置）
        boss.StopFloatingAnimation();
        
        // 选择攻击位置
        SelectAttackPosition();
        
        Debug.Log("进入Attack状态");
    }
    
    public override void QuitState()
    {
        base.QuitState();
        
        var boss = GetBossController();
        
        // 恢复浮空动画
        boss.StartFloatingAnimation();
        
        Debug.Log("退出Attack状态");
    }
    
    private void SelectAttackPosition()
    {
        var boss = GetBossController();
        
        // 根据攻击模式选择位置
        switch (currentAttackPattern)
        {
            case BossStateType.Attack_BulletPattern1:
            case BossStateType.Attack_BulletPattern2:
                // 圆形/螺旋弹幕：保持原位
                break;
                
            case BossStateType.Attack_Laser:
                // 激光攻击：移动到玩家上方
                MoveToPlayerAbove();
                break;
                
            case BossStateType.Attack_Charge:
                // 蓄力攻击：移动到边缘位置
                MoveToEdgePosition();
                break;
                
            case BossStateType.Attack_Teleport:
                // 瞬移攻击：瞬移到玩家附近（由子状态处理）
                break;
        }
    }
    
    private void MoveToPlayerAbove()
    {
        var boss = GetBossController();
        var player = FindObjectOfType<PlayerController>();
        
        // 移动到玩家上方一定距离
        Vector3 targetPos = player.transform.position;
        targetPos.y += 4f; // 在玩家上方4个单位
        targetPos.x = Mathf.Clamp(targetPos.x, 
            boss.InitialPosition.x - 3f, 
            boss.InitialPosition.x + 3f);
        
        boss.MoveTo(targetPos, 0.3f);
    }
    
    private void MoveToEdgePosition()
    {
        var boss = GetBossController();
        
        // 随机选择左边或右边边缘
        float xOffset = UnityEngine.Random.value > 0.5f ? -4f : 4f;
        Vector3 targetPos = boss.InitialPosition;
        targetPos.x += xOffset;
        targetPos.y += 2f; // 稍微升高
        
        boss.MoveTo(targetPos, 0.4f);
    }
    
    private BossController GetBossController()
    {
        return AIObj as BossController;
    }
}
```

#### 8.3.4 Recover 状态移动

**移动行为**：缓慢退回初始位置

```csharp
public class BossRecoverState : BaseState<BossStateType, IFSMObj>
{
    private float recoverDuration = 1.5f;
    private float timer;
    
    public override BossStateType StateType => BossStateType.Recover;
    
    public BossRecoverState(StateMachine<BossStateType, IFSMObj> machine) 
        : base(machine) { }
    
    public override void EnterState()
    {
        timer = 0f;
        
        var boss = GetBossController();
        
        // 开始返回初始位置
        boss.ReturnToInitialPosition(() =>
        {
            // 返回完成后自动切换回Idle状态
            ChangeState(BossStateType.Idle);
        });
        
        // 恢复浮空动画
        boss.StartFloatingAnimation();
        
        Debug.Log("进入Recover状态 - 返回初始位置");
    }
    
    public override void QuitState()
    {
        Debug.Log("退出Recover状态");
    }
    
    public override void UpdateState()
    {
        // 移动由DOTween处理，这里可以添加其他逻辑
        timer += Time.deltaTime;
        
        // 安全检查：如果移动时间过长，强制切换状态
        if (timer > recoverDuration * 2)
        {
            ChangeState(BossStateType.Idle);
        }
    }
    
    private BossController GetBossController()
    {
        return AIObj as BossController;
    }
}
```

#### 8.3.5 PhaseChange 状态移动

**移动行为**：瞬移到新位置，配合形态切换特效

```csharp
public class BossPhaseChangeState : BaseState<BossStateType, IFSMObj>
{
    private float changeDuration = 2f;
    private float timer;
    private bool hasTeleported = false;
    
    public override BossStateType StateType => BossStateType.PhaseChange;
    
    public BossPhaseChangeState(StateMachine<BossStateType, IFSMObj> machine) 
        : base(machine) { }
    
    public override void EnterState()
    {
        timer = 0f;
        hasTeleported = false;
        
        var boss = GetBossController();
        
        // 停止所有动画
        boss.StopCurrentMove();
        boss.StopFloatingAnimation();
        
        // 播放形态切换特效
        PlayPhaseChangeEffect();
        
        Debug.Log("进入PhaseChange状态");
    }
    
    public override void QuitState()
    {
        Debug.Log("退出PhaseChange状态");
    }
    
    public override void UpdateState()
    {
        timer += Time.deltaTime;
        
        // 在切换动画中间进行瞬移
        if (!hasTeleported && timer > changeDuration * 0.5f)
        {
            TeleportToNewPosition();
            hasTeleported = true;
        }
        
        // 切换完成
        if (timer >= changeDuration)
        {
            // 根据新阶段决定下一个状态
            var boss = GetBossController();
            
            if (boss.currentPhase == BossPhase.Phase4)
            {
                ChangeState(BossStateType.Enrage);
            }
            else
            {
                ChangeState(BossStateType.Idle);
            }
        }
    }
    
    private void PlayPhaseChangeEffect()
    {
        // 播放粒子特效、音效等
        // 实现省略...
    }
    
    private void TeleportToNewPosition()
    {
        var boss = GetBossController();
        
        // 根据新形态选择新位置
        Vector3 newPosition = boss.InitialPosition;
        
        // 每个阶段稍微改变位置增加视觉变化
        switch (boss.currentPhase)
        {
            case BossPhase.Phase2:
                newPosition.y += 1f;
                break;
            case BossPhase.Phase3:
                newPosition.y += 2f;
                newPosition.x += UnityEngine.Random.value > 0.5f ? 1f : -1f;
                break;
            case BossPhase.Phase4:
                newPosition.y += 3f;
                break;
        }
        
        // 瞬移到新位置
        boss.TeleportTo(newPosition);
        
        // 更新初始位置记录
        boss.SetInitialPosition(newPosition);
    }
    
    private BossController GetBossController()
    {
        return AIObj as BossController;
    }
}
```

#### 8.3.6 Enrage 状态移动

**移动行为**：快速移动，攻击间隔短距离瞬移

```csharp
public class BossEnrageState : HierarchicalState<BossStateType, IFSMObj>
{
    public override BossStateType StateType => BossStateType.Enrage;
    
    public BossEnrageState(StateMachine<BossStateType, IFSMObj> machine) 
        : base(machine)
    {
        InitializeChildStateMachine();
    }
    
    public override void EnterState()
    {
        base.EnterState();
        
        var boss = GetBossController();
        
        // 停止浮空动画（狂暴模式不需要）
        boss.StopFloatingAnimation();
        
        // 播放狂暴特效
        PlayEnrageEffect();
        
        Debug.Log("进入Enrage状态 - 狂暴模式");
    }
    
    public override void QuitState()
    {
        base.QuitState();
        
        Debug.Log("退出Enrage状态");
    }
    
    public override void UpdateState()
    {
        base.UpdateState();
        
        // 狂暴模式持续检测玩家位置
        TrackPlayer();
    }
    
    private void TrackPlayer()
    {
        var boss = GetBossController();
        var player = FindObjectOfType<PlayerController>();
        
        if (player == null) return;
        
        // 保持面向玩家
        boss.LookAtPlayer();
        
        // 攻击间隔期间进行短距离瞬移接近玩家
        if (!boss.IsMoving && IsBetweenAttacks())
        {
            MoveCloserToPlayer();
        }
    }
    
    private bool IsBetweenAttacks()
    {
        // 判断是否在攻击间隔
        // 实现省略...
        return true;
    }
    
    private void MoveCloserToPlayer()
    {
        var boss = GetBossController();
        var player = FindObjectOfType<PlayerController>();
        
        // 计算接近位置（保持一定距离）
        Vector3 direction = player.transform.position - boss.transform.position;
        direction.z = 0;
        
        if (direction.magnitude > 3f) // 距离大于3个单位时移动
        {
            Vector3 targetPos = boss.transform.position + direction.normalized * 2f;
            
            // 限制在战斗区域内
            targetPos.x = Mathf.Clamp(targetPos.x, 
                boss.InitialPosition.x - 5f, 
                boss.InitialPosition.x + 5f);
            targetPos.y = Mathf.Clamp(targetPos.y, 
                boss.InitialPosition.y - 2f, 
                boss.InitialPosition.y + 4f);
            
            // 快速移动（0.2秒）
            boss.MoveTo(targetPos, 0.2f);
        }
    }
    
    private void PlayEnrageEffect()
    {
        // 播放狂暴特效：发光、粒子、音效等
        // 实现省略...
    }
    
    private BossController GetBossController()
    {
        return AIObj as BossController;
    }
}
```

### 8.4 DOTween 动画管理

```csharp
// 在 BossController 中添加动画管理方法
public class BossController : MonoBehaviour, IFSMObj
{
    // 动画缓存
    private Dictionary<string, Tweener> tweenerCache = new Dictionary<string, Tweener>();
    
    /// <summary>
    /// 播放命名动画
    /// </summary>
    public Tweener PlayTween(string name, Tweener tweener)
    {
        // 停止同名动画
        StopTween(name);
        
        // 缓存并返回
        tweenerCache[name] = tweener;
        return tweener;
    }
    
    /// <summary>
    /// 停止命名动画
    /// </summary>
    public void StopTween(string name)
    {
        if (tweenerCache.TryGetValue(name, out var tweener))
        {
            tweener.Kill();
            tweenerCache.Remove(name);
        }
    }
    
    /// <summary>
    /// 停止所有动画
    /// </summary>
    public void StopAllTweens()
    {
        foreach (var tweener in tweenerCache.Values)
        {
            tweener.Kill();
        }
        tweenerCache.Clear();
        
        StopCurrentMove();
        StopFloatingAnimation();
    }
    
    private void OnDestroy()
    {
        // 清理所有动画
        StopAllTweens();
    }
}
```

### 8.5 移动参数配置表

| 参数 | 默认值 | 说明 | 影响状态 |
|------|-------|------|----------|
| `floatSpeed` | 1.0f | 浮空上下移动速度 | Idle, Recover |
| `floatAmplitude` | 0.5f | 浮空幅度 | Idle, Recover |
| `swaySpeed` | 0.5f | 左右摇摆速度 | Idle |
| `swayRange` | 1.0f | 左右摇摆范围 | Idle |
| `moveDuration` | 0.5f | 标准移动时间 | Attack, Recover |
| `attackAreaMin` | (-5, 2) | 攻击区域最小值 | Attack |
| `attackAreaMax` | (5, 5) | 攻击区域最大值 | Attack |

---

## 9. 阶段切换机制

### 9.1 血量检测

```csharp
// 在 BossController 中
private void CheckPhaseChange()
{
    float newPercent = currentHealth / maxHealth;
    
    if (newPercent <= 0.3f && currentPhase != BossPhase.Phase3)
    {
        ChangePhase(BossPhase.Phase3);
    }
    else if (newPercent <= 0.6f && currentPhase != BossPhase.Phase2)
    {
        ChangePhase(BossPhase.Phase2);
    }
}
```

### 8.2 阶段切换流程

1. 检测到血量阈值
2. 触发形态切换状态
3. 播放切换动画（无敌帧）
4. 更新当前形态
5. 更新攻击模式池
6. 恢复待机状态

---

## 9. 碰撞与伤害

### 9.1 碰撞体设置

| 碰撞体类型 | 用途 | 层级 |
|-----------|------|------|
| Circle Collider 2D (大) | 攻击范围检测 | EnemyTrigger |
| Circle Collider 2D (中) | 伤害判定 | EnemyHitbox |
| Circle Collider 2D (小) | Boss本体 | EnemyBody |

### 9.2 伤害流程

```
玩家攻击 → 检测碰撞 → 判断是否无敌帧 → 扣血 → 更新血量百分比 → 检测阶段切换
```

---

## 10. 调试与测试

### 10.1 调试工具

- **Gizmos 绘制**：显示攻击范围、子弹轨迹
- **Console 日志**：输出状态切换信息
- **Inspector 参数**：可调节血量、攻击参数

### 10.2 测试要点

| 测试项 | 测试方法 |
|--------|----------|
| 状态转换 | 模拟玩家进入/离开范围 |
| 攻击模式 | 触发各阶段攻击 |
| 形态切换 | 手动修改血量 |
| 浮空移动 | 观察 Boss 移动轨迹 |
| 伤害系统 | 玩家攻击 Boss |

---

## 11. 扩展规划

### 11.1 可扩展性设计

- **攻击模式池化**：支持动态添加新攻击模式
- **状态扩展**：预留特殊状态接口
- **配置数据化**：攻击参数可配置

### 11.2 已实现功能

- ✅ 狂暴模式（血量低于10%）

### 11.3 后续扩展

- 环境互动攻击
- 多阶段混合攻击
- 动态难度调整（根据玩家表现）

---

## 附录：文件路径汇总

| 文件类型 | 路径 |
|---------|------|
| 动画资源 | `Assets/艺术资产/第四章/boss/` |
| Boss AI 代码 | `Assets/Scripts/游戏逻辑/第四章_黄昏/Boss/` |
| 弹幕系统 | `Assets/Scripts/游戏逻辑/第四章_黄昏/Boss/BulletSystem/` |
| 设计文档 | `Assets/AAA文档详解/4第四章/boss战设计/` |

---

**文档版本**: v1.0  
**创建日期**: 2026-05-14  
**适用项目**: IKUN_Game - 第四章黄昏

