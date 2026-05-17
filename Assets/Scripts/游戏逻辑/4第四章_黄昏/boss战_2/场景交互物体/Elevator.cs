using TangmenFramework;
using UnityEngine;

/// <summary>
/// 升降梯：在起点和终点之间做往返运动。
/// 玩家站在升降梯上时会跟随升降梯一起移动。
/// 在Inspector中分别拖入起点Transform和终点Transform来设定运动范围。
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class Elevator : MonoBehaviour
{
    [Header("起点位置（拖入场景中的空物体或Transform）")]
    [SerializeField] private Transform startPoint;

    [Header("终点位置（拖入场景中的空物体或Transform）")]
    [SerializeField] private Transform endPoint;

    [Header("移动速度")]
    [SerializeField] private float moveSpeed = 2f;

    [Header("到达端点后的停留时间（秒）")]
    [SerializeField] private float waitTime = 0.5f;

    private Rigidbody2D _rb;

    /// <summary>当前是否在端点停留等待中</summary>
    private bool _isWaiting;

    /// <summary>停留等待计时器</summary>
    private float _waitTimer;

    /// <summary>当前移动方向：true = 向终点移动，false = 向起点移动</summary>
    private bool _movingToEnd = true;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        // 升降梯自身必须是Kinematic，物理引擎才能正确追踪它的移动
        _rb.bodyType = RigidbodyType2D.Kinematic;
        _rb.interpolation = RigidbodyInterpolation2D.Interpolate;
    }

    private void Start()
    {
        if (startPoint != null)
            _rb.position = startPoint.position;
    }

    private void FixedUpdate()
    {
        if (startPoint == null || endPoint == null)
            return;

        if (_isWaiting)
        {
            _waitTimer -= Time.fixedDeltaTime;
            if (_waitTimer <= 0f)
                _isWaiting = false;
            return;
        }

        Vector2 target = _movingToEnd ? (Vector2)endPoint.position : (Vector2)startPoint.position;

        // 使用Rigidbody2D.MovePosition移动，物理引擎能正确追踪位移
        _rb.MovePosition(Vector2.MoveTowards(
            _rb.position,
            target,
            moveSpeed * Time.fixedDeltaTime
        ));

        if (Vector2.Distance(_rb.position, target) < 0.01f)
        {
            _isWaiting = true;
            _waitTimer = waitTime;
            _movingToEnd = !_movingToEnd;
        }
    }

    /// <summary>
    /// 玩家站上升降梯时，将玩家设为升降梯的子对象，并将Rigidbody2D设为Kinematic，
    /// 防止物理引擎的重力干扰导致升降梯下降时玩家跟不上
    /// </summary>
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            MusicMgr.Instance.PlaySound(MyAssetBundleName.第四章音效包, "升降梯");

            // 检测碰撞法线方向，只有玩家从上方踩上才跟随升降梯
            if (collision.contacts[0].normal.y < 0.5f)
                return;

            collision.gameObject.transform.SetParent(transform);
            Rigidbody2D rb = collision.gameObject.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.bodyType = RigidbodyType2D.Kinematic;
                rb.velocity = Vector2.zero;
            }
        }
    }

    /// <summary>
    /// 玩家离开升降梯时，解除父子关系，并将Rigidbody2D恢复为Dynamic
    /// </summary>
    private void OnCollisionExit2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            collision.gameObject.transform.SetParent(null);
            Rigidbody2D rb = collision.gameObject.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.bodyType = RigidbodyType2D.Dynamic;
            }
        }
    }

#if UNITY_EDITOR
    /// <summary>
    /// 编辑器中选中升降梯时，在Scene视图绘制起点、终点及连线，方便调试
    /// </summary>
    private void OnDrawGizmosSelected()
    {
        if (startPoint != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(startPoint.position, 0.3f);
        }
        if (endPoint != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(endPoint.position, 0.3f);
        }
        if (startPoint != null && endPoint != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(startPoint.position, endPoint.position);
        }
    }
#endif
}
