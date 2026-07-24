using TangmenFramework;
using UnityEngine;

/// <summary>
/// 第一章节奏接箱玩家控制器。
/// PC 的 A/D、方向键由框架 InputMgr 转换为水平事件；移动端按钮也发送同一个事件，
/// 因此玩家移动逻辑不需要区分输入来源。
/// </summary>
public class RhythmCatchPlayerController : MonoBehaviour
{
    [Header("最大移动速度（单位/秒）输入拉满时达到此速度")]
    [SerializeField, Min(0.1f)] 
    private float moveSpeed = 9f;

    [Header("加速率，从静止加速到 moveSpeed 的快慢")]
    [SerializeField, Min(0.1f)] 
    private float acceleration = 36f;

    [Header("减速率，松开输入后回弹到静止的快慢（比加速略大，收住惯性）")]
    [SerializeField, Min(0.1f)] 
    private float deceleration = 44f;

    [Header("玩家水平移动的左边界")]
    [SerializeField] 
    private float minX = -4.9f;

    [Header("玩家水平移动的右边界")]
    [SerializeField] 
    private float maxX = 4.9f;

    [Header("移动时的倾斜角度")]
    [SerializeField] 
    private float tiltAngle = 8f;                    

    [Header("Catch Area")]
    [SerializeField] 
    private Transform visualRoot;

    //水平输入值
    private float _horizontalInput;
    //当前的速度
    private float _currentVelocity;

    private bool _controlEnabled = true;
    private BoxCollider2D _catchCollider;

    public float CurrentHorizontalInput => _horizontalInput;
    /// <summary>
    /// 接取区域的碰撞体，供掉落物使用物理碰撞检测横向覆盖。
    /// </summary>
    public BoxCollider2D CatchCollider => _catchCollider;

    private void Awake()
    {
        _catchCollider = GetComponent<BoxCollider2D>();
        if (_catchCollider == null)
            _catchCollider = gameObject.AddComponent<BoxCollider2D>();
    }

    private void OnEnable()
    {
        // 访问单例会初始化框架输入管理器，并由 MonoMgr 持续执行 InputMgr.UpdateInput。
        InputMgr.Instance.SetEnabled(true);

        // PC 键盘和移动端 UGUI 都统一发送 E_Input_Horizontal。
        EventCenter.Instance.AddEventListener<float>(E_EventType.E_Input_Horizontal, OnHorizontalInput);
    }

    private void OnDisable()
    {
        EventCenter.Instance.RemoveEventListener<float>(E_EventType.E_Input_Horizontal, OnHorizontalInput);
        _horizontalInput = 0f;
        _currentVelocity = 0f;
    }

    private void Update()
    {
        float targetInput = _controlEnabled ? _horizontalInput : 0f;
        float targetVelocity = targetInput * moveSpeed;

        // 输入开始时快速加速，输入释放后使用更高减速度收住惯性，兼顾顺滑与可控性。
        float changeSpeed = Mathf.Abs(targetVelocity) > 0.01f ? acceleration : deceleration;
        _currentVelocity = Mathf.MoveTowards(_currentVelocity, targetVelocity, changeSpeed * Time.deltaTime);

        //计算位置，限制在对应的范围之内
        Vector3 position = transform.position;
        position.x = Mathf.Clamp(position.x + _currentVelocity * Time.deltaTime, minX, maxX);
        transform.position = position;

        // 白盒角色根据移动方向轻微倾斜，提供即时的方向反馈。
        if (visualRoot != null)
        {
            float tilt = -targetInput * tiltAngle;
            Quaternion targetRotation = Quaternion.Euler(0f, 0f, tilt);
            visualRoot.localRotation = Quaternion.Slerp(visualRoot.localRotation, targetRotation, 12f * Time.deltaTime);
        }
    }

    /// <summary>
    /// 关卡结束或失败时关闭控制，同时平滑停止当前速度。
    /// </summary>
    public void SetControlEnabled(bool enabled)
    {
        _controlEnabled = enabled;
        if (!enabled)
            _horizontalInput = 0f;
    }

    /// <summary>
    /// 供白盒场景搭建逻辑一次性写入可调参数。
    /// </summary>
    public void Configure(float speed, float minimumX, float maximumX, Transform visual)
    {
        moveSpeed = Mathf.Max(0.1f, speed);
        minX = minimumX;
        maxX = maximumX;
        visualRoot = visual;
    }

    private void OnHorizontalInput(float value)
    {
        _horizontalInput = Mathf.Clamp(value, -1f, 1f);
    }
}
