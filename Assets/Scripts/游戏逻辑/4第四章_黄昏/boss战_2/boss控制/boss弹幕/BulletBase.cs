using TangmenFramework;
using UnityEngine;

/// <summary>
/// Boss弹幕基类（模板方法模式）
/// 公共逻辑：生命周期、屏幕边界检测、自动回收
/// 子类只需要实现 MoveLogic() 定义各自的运动轨迹
/// </summary>
public abstract class BulletBase : MonoBehaviour
{

    /// <summary>
    /// 子弹移动速度
    /// </summary>
    protected float moveSpeed;
    /// <summary>
    /// 子弹生命周期
    /// </summary>
    protected float lifetime = 5f;    
    /// <summary>
    /// 子弹生命周期计时器
    /// </summary>
    protected float timer;
    /// <summary>
    /// 子弹是否激活
    /// </summary>
    protected bool isActive;
    /// <summary>
    /// 主相机
    /// </summary>
    protected Camera mainCamera;
    /// <summary>
    /// 销毁边界
    /// </summary>
    protected float destroyMargin = 2f;

    void OnEnable()
    {
        isActive = true;
        timer = 0f;

        if (mainCamera == null)
            mainCamera = Camera.main;
    }

    /// <summary>
    /// 初始化子弹公共参数
    /// 子类重写时应先调用 base.Init()
    /// </summary>
    public virtual void Init(float speed)
    {
        moveSpeed = speed;
        timer = 0f;
        isActive = true;
    }

    void Update()
    {
        if (!isActive)
            return;

        timer += Time.deltaTime;

        // 执行子类定义的移动逻辑
        MoveLogic(Time.deltaTime);

        // 超出屏幕边界或超时则自动回收
        if (IsOutOfScreen() || timer >= lifetime)
        {
            Recycle();
        }
    }

    /// <summary>
    /// 子弹的运动逻辑（子类实现）
    /// </summary>
    protected abstract void MoveLogic(float deltaTime);

    /// <summary>
    /// 检测是否超出屏幕边界（考虑destroyMargin）
    /// </summary>
    /// <returns></returns>
    protected bool IsOutOfScreen()
    {
        if (mainCamera == null)
            return false;

        Vector3 viewPos = mainCamera.WorldToViewportPoint(transform.position);

        return viewPos.x < -destroyMargin || viewPos.x > 1f + destroyMargin
            || viewPos.y < -destroyMargin || viewPos.y > 1f + destroyMargin;
    }

    /// 回收子弹：禁用对象并放回对象池
    public void Recycle()
    {
        isActive = false;
        GOPoolMgr.Instance.PushObj(gameObject);
    }
}
