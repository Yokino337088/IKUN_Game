using Animancer;
using DG.Tweening;
using System;
using System.Collections.Generic;
using UnityEngine;
using TangmenFramework;
using static UnityEngine.GraphicsBuffer;

/// <summary>
/// Boss控制器，管理Boss的状态、移动和动画
/// </summary>
public class BossController : MonoBehaviour, IBossFSMObj
{
    /// <summary>
    /// 原缩放
    /// </summary>
    private Vector3 originalScale = Vector3.one * 3.5f;


    #region 状态机相关
    /// <summary>
    /// 状态机
    /// </summary>
    private BossStateMachine stateMachine;

    /// <summary>
    /// 当前状态
    /// </summary>
    public BossStateType CurrentState => stateMachine.CurrentStateType;
    #endregion

    #region 动画相关
    /// <summary>
    /// Animancer组件
    /// </summary>
    [Header("动画组件")]
    public AnimancerComponent animancer;

    /// <summary>
    /// 动画剪辑映射表
    /// </summary>
    public List<AnimationClipMapping> animationClips = new List<AnimationClipMapping>();
    //这里通过字典的方式来避免写switch-case语句，这就是表驱动法
    private Dictionary<BossPhaseType, AnimationClip> clipDictionary = new Dictionary<BossPhaseType, AnimationClip>();

    /// <summary>
    /// 动画剪辑映射结构，boss状态对
    /// </summary>
    [Serializable]
    public struct AnimationClipMapping
    {
        public BossPhaseType stateType;
        public AnimationClip clip;
    }
    #endregion

    #region 移动相关
    /// <summary>
    /// 初始位置
    /// </summary>
    private Vector3 initialPosition;

    /// <summary>
    /// 当前目标位置
    /// </summary>
    private Vector3 targetPosition;

    /// <summary>
    /// 是否正在移动
    /// </summary>
    public bool IsMoving { get; private set; }

    /// <summary>
    /// 浮空动画Tweener（DOTween动画句柄，用于控制浮空上下浮动动画的生命周期）
    /// </summary>
    private Tweener floatTween;

    /// <summary>
    /// 当前移动Tweener（DOTween动画句柄，用于控制位移动画的生命周期）
    /// </summary>
    private Tweener currentMoveTween;

    /// <summary>
    /// 浮空参数
    /// </summary>
    [Header("浮空参数")]
    public float floatSpeed = 1.0f;       // 浮动速度：值越大，上下浮动越快
    public float floatAmplitude = 0.5f;   // 浮动幅度：值越大，浮动范围越大
    public float swaySpeed = 0.5f;        // 左右摇摆速度
    public float swayRange = 1.0f;        // 左右摇摆范围

    /// <summary>
    /// 移动参数
    /// </summary>
    [Header("移动参数")]
    public float moveDuration = 0.5f;     // 移动到目标位置所需的时间（秒）

    /// <summary>
    /// 激光攻击是否向左移动
    /// </summary>
    private bool isLaserMovingLeft = true;

    /// <summary>
    /// 激光攻击移动边界
    /// </summary>
    private float laserMoveBoundaryLeft;
    private float laserMoveBoundaryRight;

    /// <summary>
    /// 激光攻击移动速度
    /// </summary>
    private float laserMoveSpeed = 2.5f;

    /// <summary>
    /// 蓄力动画效果Tweener（DOTween动画句柄，用于控制蓄力缩放动画的生命周期）
    /// </summary>
    private Tweener chargeEffectTween;

    /// <summary>
    /// 激光动画效果Tweener（DOTween动画句柄，用于控制激光震动动画的生命周期）
    /// </summary>
    private Tweener laserEffectTween;
    #endregion

    #region 攻击相关
    /// <summary>
    /// 攻击范围
    /// </summary>
    [Header("攻击参数")]
    public float attackRange = 8f;

    /// <summary>
    /// 玩家Transform缓存
    /// </summary>
    private Transform playerTransform;
    #endregion

    #region 运行时数据

    private BossData bossData;

    #endregion

    #region 生命周期
    private void Awake()
    {
        bossData = new BossData();
        // 记录初始位置
        initialPosition = transform.position;
        targetPosition = initialPosition;

        // 初始化状态机
        InitializeStateMachine();
    }

    private void Start()
    {
        // 查找玩家
        FindPlayer();

        // 开始浮空动画
        StartFloatingAnimation();
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

    private void OnDestroy()
    {
        // 清理动画
        StopAllTweens();
    }

    private void OnDrawGizmosSelected()
    {
        // 绘制攻击范围
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
    #endregion

    #region 状态机初始化
    private void InitializeStateMachine()
    {
        stateMachine = new BossStateMachine(this);

        // 添加状态
        stateMachine.AddState<BossIdleState>(BossStateType.Idle);
        stateMachine.AddState<BossAlertState>(BossStateType.Alert);
        stateMachine.AddState<BossAttackState>(BossStateType.Attack);
        stateMachine.AddState<BossRecoverState>(BossStateType.Recover);
        stateMachine.AddState<BossPhaseChangeState>(BossStateType.PhaseChange);

        // 添加攻击子状态
        stateMachine.AddState<BulletPattern1State>(BossStateType.BulletPattern1);
        stateMachine.AddState<BulletPattern2State>(BossStateType.BulletPattern2);
        stateMachine.AddState<LaserAttackState>(BossStateType.LaserAttack);
        stateMachine.AddState<ChargeAttackState>(BossStateType.ChargeAttack);
        stateMachine.AddState<TeleportAttackState>(BossStateType.TeleportAttack);

        // 设置初始状态
        stateMachine.ChangeState(BossStateType.Idle);
    }
    #endregion


    


    /// <summary>
    /// 初始化动画
    /// </summary>
    private void AnimationInit()
    {
        //遍历列表
        foreach (var info in animationClips)
        {
            //添加数据
            clipDictionary.Add(info.stateType, info.clip);
        }
    }

    

    #region 玩家查找
    private void FindPlayer()
    {
        var player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerTransform = player.transform;
        }
        else
        {
            Debug.LogWarning("未找到玩家对象");
        }
    }
    #endregion

    #region 移动控制
    /// <summary>
    /// 开始浮空动画
    /// DOTween播放逻辑：使用DOLocalMoveY在Y轴上做循环往复运动，模拟漂浮效果
    /// </summary>
    public void StartFloatingAnimation()
    {
        // 安全检查：如果浮空动画已经在播放，避免重复创建
        if (floatTween != null && floatTween.IsActive())
            return;

        // 【DOTween】将Boss在Y轴上从当前位置移动到 初始Y+浮动幅度 的位置
        // DOLocalMoveY：只修改localPosition的Y分量，不受父节点影响
        floatTween = transform.DOLocalMoveY(initialPosition.y + floatAmplitude, 1f / floatSpeed)
            .SetEase(Ease.InOutSine)    // 【缓动曲线】InOutSine = 正弦波缓入缓出，开头加速→中间匀速→结尾减速，模拟自然漂浮感
            .SetLoops(-1, LoopType.Yoyo) // 【循环模式】-1 = 无限循环，Yoyo = 来回往复（到达终点后反向回到起点，如此反复）
            .SetUpdate(true);            // 【忽略时间缩放】设为true表示不管Time.timeScale是否为0（如暂停时），动画都继续播放
    }

    /// <summary>
    /// 停止浮空动画
    /// DOTween播放逻辑：Kill掉浮空动画的Tweener，停止Y轴浮动
    /// </summary>
    public void StopFloatingAnimation()
    {
        // 【DOTween】Kill()：立即终止动画并释放资源，Boss停在当前Y轴位置
        //及时清理引用类型防止内存泄漏
        floatTween?.Kill();
        floatTween = null;
    }

    /// <summary>
    /// 移动到指定位置
    /// DOTween播放逻辑：使用DOMove平滑移动到目标坐标
    /// </summary>
    /// <param name="target">目标世界坐标</param>
    /// <param name="duration">移动耗时（秒），默认0.5秒</param>
    /// <param name="onComplete">移动完成后的回调</param>
    public void MoveTo(Vector3 target, float duration = 1f, Action onComplete = null)
    {
        // 【DOTween】先停掉上一次移动动画，避免两个Tweener同时控制position导致抖动
        StopCurrentMove();

        IsMoving = true;
        targetPosition = target;

        // 【DOTween】DOMove：在duration秒内从当前位置平滑移动到target坐标（修改transform.position）
        currentMoveTween = transform.DOMove(target, duration)
            .SetEase(Ease.OutQuad)  // 【缓动曲线】OutQuad = 二次函数缓出，开头快→结尾慢，模拟逐渐刹车停止的减速感
            .OnComplete(() =>       // 【完成回调】DOTween动画播放完毕后自动执行
            {
                IsMoving = false;           // 标记移动结束
                onComplete?.Invoke();       // 执行外部传入的回调
            })
            .SetUpdate(true);        // 【忽略时间缩放】暂停时仍然继续移动
    }

    /// <summary>
    /// 停止当前移动
    /// DOTween播放逻辑：Kill掉移动动画的Tweener
    /// </summary>
    public void StopCurrentMove()
    {
        // 【DOTween】Kill()：立即终止DOMove动画，Boss停在当前坐标位置
        currentMoveTween?.Kill();
        currentMoveTween = null;
        IsMoving = false;
    }

    /// <summary>
    /// 返回初始位置
    /// DOTween播放逻辑：调用MoveTo回到最初记录的位置
    /// </summary>
    public void ReturnToInitialPosition()
    {
        MoveTo(initialPosition, 1f);
    }

    

    /// <summary>
    /// 停止所有动画
    /// DOTween播放逻辑：清理本Transform上的所有Tweener
    /// </summary>
    private void StopAllTweens()
    {
        StopCurrentMove();
        StopFloatingAnimation();

        // 【DOTween】Kill(transform)：一键杀掉挂在这个Transform上的所有Tweener（包括浮空、移动、缩放、颜色等）
        // 返回值为被杀掉的Tweener数量，这里不关心返回值所以忽略
        DOTween.Kill(transform);
    }
    #endregion

    #region 攻击范围检测
    /// <summary>
    /// 检测玩家是否在攻击范围内
    /// </summary>
    public bool IsPlayerInAttackRange()
    {
        if (playerTransform == null)
            return false;

        float distance = Vector3.Distance(transform.position, playerTransform.position);
        return distance <= attackRange;
    }
    #endregion

    #region IBossFSMObj接口实现
    AnimancerComponent IAnimancerFSMObj.Animancer => animancer;

    public AnimancerComponent Animancer => throw new NotImplementedException();

    /// <summary>
    /// 获取Boss控制器
    /// </summary>
    public BossController GetBossController()
    {
        return this;
    }

    BossData IBossFSMObj.GetBossData()
    {
        return bossData;
    }

    AnimationClip IBossFSMObj.GetAnimationClip(BossPhaseType bossPhaseType)
    {
        //直接查字典返回
        return clipDictionary[bossPhaseType];
    }

    void IBossFSMObj.DoAttackStartAnimation()
    {
        // 保存初始状态
        Quaternion initialRotation = transform.rotation;

        // 创建一个序列来串联多个动画
        Sequence sequence = DOTween.Sequence();

        // 第一步：旋转720度（两圈）
        sequence.Append(transform.DORotate(new Vector3(0, 0, 720), 1f).SetEase(Ease.OutQuad).SetRelative());

        // 第二步：抖动1秒（在旋转完成后执行）
        sequence.Append(transform.DOShakeRotation(1f, 60).SetEase(Ease.Linear));

        // 3. 复原到初始旋转（平滑过渡）
        sequence.Append(transform.DORotateQuaternion(initialRotation, 0.25f).SetEase(Ease.OutCubic));

        //播放序列
        sequence.Play();
    }

    void IBossFSMObj.DoBullet1Animation()
    {
        Vector3 targetPos = new Vector3(playerTransform.position.x, playerTransform.position.y + 6, 0);

        MoveTo(targetPos);
    }

    /// <summary>
    /// 螺旋弹幕移动动画
    /// DOTween播放逻辑：向玩家方向往复移动，DOMove + Yoyo循环
    /// </summary>
    void IBossFSMObj.DoBullet2Animation()
    {
        // 计算指向玩家的方向向量（归一化后长度为1，只保留方向信息）
        Vector3 moveDir = (playerTransform.position - transform.position).normalized;
        // 目标位置 = 当前位置 + 方向 * X个单位（向玩家靠近X个单位）
        Vector3 targetPos = transform.position + moveDir * 3f;

        // 【DOTween】DOMove：在1.5秒内移动到目标位置
        currentMoveTween = transform.DOMove(targetPos, 1.5f) 
            .SetEase(Ease.Linear)       // 【缓动曲线】Linear = 匀速运动，没有加速和减速，适合弹幕类持续攻击
            .SetLoops(-1, LoopType.Yoyo); // 【循环模式】-1 = 无限循环，Yoyo = 到达终点后反向回到起点再循环
    }

    /// <summary>
    /// 蓄力攻击移动动画
    /// DOTween播放逻辑：移动到边缘位置，完成后触发回调开始蓄力
    /// </summary>
    /// <param name="onComplete">移动完成后执行的回调（开始蓄力）</param>
    void IBossFSMObj.DoChargeAnimation(Action onComplete)
    {
        // 随机决定移动到左边还是右边：50%概率左边缘，50%概率右边缘
        float edgeX = UnityEngine.Random.value > 0.5f
            ? transform.position.x - 6f   // 向左移动4个单位
            : transform.position.x + 6f;  // 向右移动4个单位
        // 目标位置：边缘X坐标，Y坐标比当前位置高2个单位（蓄力时升高）
        Vector3 targetPos = new Vector3(edgeX, transform.position.y + 2f, 0);

        // 调用MoveTo移动到边缘，移动完成后自动执行onComplete回调（即开始蓄力）
        MoveTo(targetPos, 1f, onComplete);
    }

    /// <summary>
    /// 激光攻击移动动画
    /// DOTween播放逻辑：移动到玩家正上方，完成后触发回调开始激光攻击
    /// </summary>
    /// <param name="onComplete">移动完成后执行的回调（开始发射激光）</param>
    void IBossFSMObj.DoLaserAnimation(Action onComplete)
    {
        // 设置激光水平移动的左右边界：以移动前的位置为中心，左右各4个单位
        laserMoveBoundaryLeft = transform.position.x - 4f;
        laserMoveBoundaryRight = transform.position.x + 4f;
        isLaserMovingLeft = true;   // 默认从向左移动开始

        // 目标位置：玩家正上方4个单位
        Vector3 playerPos = playerTransform.position;
        Vector3 targetPos = new Vector3(playerPos.x, playerPos.y + 4f, 0);

        // 调用MoveTo移动到玩家上方，移动完成后自动执行onComplete回调（即开始攻击）
        MoveTo(targetPos, 0.5f, onComplete);
    }

    /// <summary>
    /// 激光攻击的水平移动逻辑（每帧调用）
    /// DOTween播放逻辑：在左右边界之间往复移动，每帧调用DOMoveX实现平滑跟随
    /// </summary>
    void IBossFSMObj.DoLaserHorizontalMove()
    {
        Vector3 currentPos = transform.position;

        // 根据当前移动方向计算新X坐标
        if (isLaserMovingLeft)
        {
            // 向左移动：X坐标减去 速度×每帧时间
            currentPos.x -= laserMoveSpeed * Time.deltaTime;
            // 碰到左边界时反转方向
            if (currentPos.x <= laserMoveBoundaryLeft)
            {
                isLaserMovingLeft = false;
            }
        }
        else
        {
            // 向右移动：X坐标加上 速度×每帧时间
            currentPos.x += laserMoveSpeed * Time.deltaTime;
            // 碰到右边界时反转方向
            if (currentPos.x >= laserMoveBoundaryRight)
            {
                isLaserMovingLeft = true;
            }
        }

        // 【DOTween】DOMoveX：只修改X分量，Y和Z保持不变
        // 每帧创建一次（0.1秒的短动画），因为每帧都会重新计算目标X，形成平滑的实时跟随效果
        transform.DOMoveX(currentPos.x, 0.1f)
            .SetEase(Ease.Linear)    // 【缓动曲线】Linear = 匀速，激光扫射过程速度恒定
            .SetUpdate(true);        // 【忽略时间缩放】确保暂停时也能正常运行
    }

    

    /// <summary>
    /// 蓄力攻击DOTween动画效果（缩放脉冲 + 颜色变红）
    /// DOTween播放逻辑：DOScale逐渐放大 + DOColor变红，给玩家蓄力威压感
    /// </summary>
    /// <param name="duration">蓄力持续时间（秒）</param>
    void IBossFSMObj.DoChargeAnimationEffect(float duration)
    {
        // 停止旧的效果动画，避免两个Tweener叠加导致缩放动画冲突
        chargeEffectTween?.Kill();

        // 【DOTween】DOScale：将localScale从当前值平滑过渡到5倍大小（表示蓄力能量积累）
        // 比如初始scale是(1,1,1)，duration秒后会变成(5,5,5)，Boss视觉上逐渐膨胀变大
        chargeEffectTween = transform.DOScale(6f, duration)
            .SetEase(Ease.InOutQuad) // 【缓动曲线】InOutQuad = 缓入缓出二次曲线，开始慢→中间快→结尾慢，像蓄力能量逐渐充能
            .SetUpdate(true);        // 【忽略时间缩放】暂停时蓄力效果仍然可见

        // 获取SpriteRenderer组件（如果Boss使用的是SpriteRenderer渲染而非MeshRenderer）
        var sr = GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            // 【DOTween】DOColor：将Sprite的颜色从当前色平滑过渡到红色
            // duration*0.3 = 前30%时间内变成红色，之后保持红色到蓄力结束，形成"能量充满变红"的视觉提示
            sr.DOColor(Color.red, duration * 0.3f)
                .SetEase(Ease.OutQuad)  // 【缓动曲线】OutQuad = 二次缓出，开头快→结尾慢，快速变红后趋于稳定
                .SetUpdate(true);
        }
    }

    /// <summary>
    /// 停止蓄力动画效果并恢复原始状态
    /// DOTween播放逻辑：Kill缩放动画 + 恢复localScale + DOColor恢复白色
    /// </summary>
    void IBossFSMObj.StopChargeAnimationEffect()
    {
        // 【DOTween】Kill掉蓄力缩放动画，防止它继续修改scale覆盖后面的恢复操作
        chargeEffectTween?.Kill();
        chargeEffectTween = null;

        // 立即恢复缩放为(1,1,1)，回到原始大小
        transform.localScale = originalScale;

        // 恢复Sprite颜色
        var sr = GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            // 【DOTween】DOColor：0.2秒内从当前颜色平滑过渡回白色
            sr.DOColor(Color.white, 0.2f)
                .SetEase(Ease.OutQuad)  // 【缓动曲线】OutQuad = 二次缓出，先快后慢的逐渐恢复
                .SetUpdate(true);       // 【忽略时间缩放】
        }
    }

    /// <summary>
    /// 激光攻击DOTween动画效果（屏幕震动）
    /// DOTween播放逻辑：DOShakePosition使Boss位置抖动，营造激光能量冲击感
    /// </summary>
    void IBossFSMObj.DoLaserAnimationEffect()
    {
        // 停止旧的震动动画，避免多个震动叠加导致抖动失控
        laserEffectTween?.Kill();

        // 【DOTween】DOShakePosition参数详解：
        //   0.3f  = 震动总时长0.3秒，完成后自动停止
        //   0.15f = 震动强度/幅度0.15个单位（水平+垂直方向偏移量）
        //   10    = 振动次数（vibrato），值越大抖动越密集
        //   90    = 随机性（randomness），0-180，值越大越不规则，90=中等随机
        //   false = 不沿X轴震动？（后面true会覆盖X）
        //   true  = 同时震动X和Y两个方向（左右+上下同时抖）
        laserEffectTween = transform.DOShakePosition(0.3f, 0.15f, 10, 90, false, true)
            .SetUpdate(true);   // 【忽略时间缩放】
    }

    /// <summary>
    /// 停止激光动画效果（震动）
    /// DOTween播放逻辑：Kill掉震动动画
    /// </summary>
    void IBossFSMObj.StopLaserAnimationEffect()
    {
        // 【DOTween】Kill掉震动动画，Boss停止抖动
        laserEffectTween?.Kill();
        laserEffectTween = null;
    }

    /// <summary>
    /// 瞬移到指定位置（使用DOTween实现闪现效果）
    /// DOTween播放逻辑：缩小消失 → 移动到目标 → 放大出现，三段式动画模拟经典瞬移效果
    /// </summary>
    /// <param name="target">目标世界坐标</param>
    void IBossFSMObj.TeleportTo(Vector3 target)
    {
        //先停止当前的移动
        StopCurrentMove();

        //设置状态
        IsMoving = true;
        targetPosition = target;

        // === 第一段：缩小消失（0.1秒） ===
        // 【DOTween】DOScale：0.1秒内缩小到(0,0,0)，Boss视觉上像被吸入空间裂缝
        transform.DOScale(Vector3.zero, 0.1f)
            .SetEase(Ease.InBack)      // 【缓动曲线】InBack = 先微微后退再加速前进，有"吸入"的视觉感——在缩小的起始会先反向微涨一下再快速缩小
            .OnComplete(() =>          // 【完成回调】缩小完成后执行第二段
            {
                // === 第二段：移动到目标位置（0.15秒） ===
                // 【DOTween】DOMove：0.15秒内从当前位置移动到目标坐标，Boss在不可见状态下位移
                currentMoveTween = transform.DOMove(target, 0.15f)
                    .SetEase(Ease.OutQuad);  // 【缓动曲线】OutQuad = 先快后慢的减速移动

                // === 第三段：放大出现（0.15秒） ===
                // 【DOTween】DOScale：0.15秒内从(0,0,0)放大到原来的大小，Boss从目标位置膨胀出现
                transform.DOScale(originalScale, 0.2f)
                    .SetEase(Ease.OutBack)  // 【缓动曲线】OutBack = 先冲过头再回弹到目标值——放大到略大于1再弹回1，有"弹出来"的视觉冲击力
                    .OnComplete(() =>
                    {
                        IsMoving = false;   // 整个瞬移动画完成，解除移动锁定
                    })
                    .SetUpdate(true);       // 【忽略时间缩放】
            })
            .SetUpdate(true);               // 【忽略时间缩放】
    }

    BossPhaseType IBossFSMObj.CheckNowPhase()
    {
        //直接返回枚举字段就行了
        return bossData.nowPhase;
    }
    #endregion
}
