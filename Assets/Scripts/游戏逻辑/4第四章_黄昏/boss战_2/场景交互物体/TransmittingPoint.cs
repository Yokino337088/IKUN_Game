using UnityEngine;

/// <summary>
/// 传送点：玩家进入触发区域时，将玩家瞬间传送至指定的目标点位置。
/// 目标传送点可在Inspector中拖入场景中的Transform来设定。
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class TransmittingPoint : MonoBehaviour
{
    [Header("目标传送点（拖入场景中要传送到的空物体或Transform）")]
    [SerializeField] 
    private Transform targetPoint;

    [Header("传送后是否保留玩家的水平速度")]
    [SerializeField] 
    private bool preserveHorizontalVelocity = true;

    

    

    /// <summary>
    /// 玩家进入传送点触发区域时，将玩家传送到目标点
    /// </summary>
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player"))
            return;

        

        // 校验目标传送点是否已设置
        if (targetPoint == null)
        {
            Debug.LogWarning("TransmittingPoint: 目标传送点未设置！", this);
            return;
        }

        // 通过BossLevelMgr获取玩家控制器
        BossPlayerControl playerControl = BossLevelMgr.Instance?.PlayerControl;
        if (playerControl == null)
            return;

        // 传送前保存水平速度（用于传送后恢复）
        Rigidbody2D playerRb = playerControl.GetComponent<Rigidbody2D>();
        float savedVelocityX = 0f;
        if (playerRb != null && preserveHorizontalVelocity)
        {
            savedVelocityX = playerRb.velocity.x;
        }

        // 执行传送：将玩家位置设置到目标点
        playerControl.transform.position = targetPoint.position;

        // 传送后恢复水平速度（垂直速度清零，防止落地误判）
        if (playerRb != null && preserveHorizontalVelocity)
        {
            playerRb.velocity = new Vector2(savedVelocityX, 0f);
        }
    }

#if UNITY_EDITOR
    /// <summary>
    /// 编辑器中选中传送点时，在Scene视图绘制目标点和连线，方便调试
    /// </summary>
    private void OnDrawGizmosSelected()
    {
        if (targetPoint != null)
        {
            // 绘制传送连线
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(transform.position, targetPoint.position);

            // 绘制目标点
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(targetPoint.position, 0.5f);

            // 绘制传送方向箭头
            Vector3 dir = (targetPoint.position - transform.position).normalized;
            Vector3 midPoint = (transform.position + targetPoint.position) * 0.5f;
            Gizmos.DrawLine(midPoint, midPoint + dir * 0.5f);
        }
    }
#endif
}
