using UnityEngine;

/// <summary>
/// Boss战玩家摄像机跟随（死区模式 + 横向预瞄）
/// 专门为 i wanna 风格 2D 横板 Boss 战设计。
///
/// 核心机制：
/// 1. 死区模式：玩家在屏幕中央的自由区域内移动时，摄像机不跟，只在玩家走出死区时才追过去
/// 2. 横向预瞄：摄像机在玩家移动方向上提前偏移一段距离，让玩家视野更开阔
/// 3. 非对称死区：横版游戏的水平移动远多于垂直移动，X和Y的死区大小可分开设置
/// 4. 战斗区域限制：摄像机不会超出Boss房间的边界
///
/// 挂载到场景中的主摄像机上即可
/// </summary>
public class BossBattleCameraFollow : MonoBehaviour
{
    [Header("========== 跟随目标 ==========")]
    [Tooltip("要跟随的玩家Transform")]
    public Transform target;

    [Tooltip("Boss的Transform，用于计算双目标居中（可选）")]
    public Transform bossTarget;

    [Header("========== 死区设置 ==========")]
    [Tooltip("水平死区比例。0=无死区始终跟随，0.3=玩家走到屏幕横向边缘30%处才移动摄像机")]
    [Range(0f, 0.5f)]
    public float horizontalDeadZone = 0.2f;

    [Tooltip("垂直死区比例。横版游戏垂直移动少，可以设大一点减少上下抖动")]
    [Range(0f, 0.5f)]
    public float verticalDeadZone = 0.35f;

    [Header("========== 横向预瞄 ==========")]
    [Tooltip("是否启用预瞄：摄像机在玩家移动方向上提前偏移一段距离")]
    public bool enableLookAhead = true;

    [Tooltip("预瞄偏移量（世界单位），值越大摄像机越往前看")]
    [Range(0f, 8f)]
    public float lookAheadOffset = 3f;

    [Tooltip("预瞄平滑速度，值越大摄像机越快地调整预瞄位置")]
    [Range(1f, 20f)]
    public float lookAheadSmoothSpeed = 5f;

    [Header("========== Boss追瞳 ==========")]
    [Tooltip("是否考虑Boss位置来调整摄像机。开启后摄像机会在玩家和Boss之间取中点")]
    public bool trackBoss = true;

    [Tooltip("Boss对摄像机的影响力。0=只看玩家，1=只看Boss，0.5=玩家和Boss各占一半")]
    [Range(0f, 1f)]
    public float bossInfluence = 0.3f;

    [Header("========== 平滑设置 ==========")]
    [Tooltip("摄像机移动的平滑速度，值越大越灵敏")]
    [Range(1f, 20f)]
    public float snapSpeed = 6f;

    [Header("========== 视野设置 ==========")]
    [Tooltip("摄像机Z轴偏移")]
    public Vector3 cameraOffset = new Vector3(0, 0, -10f);

    [Header("========== 边界限制 ==========")]
    [Tooltip("是否限制摄像机不超出房间边界")]
    public bool clampToRoomBounds = true;

    [Tooltip("房间左下角边界（世界坐标）")]
    public Vector2 roomBottomLeft;

    [Tooltip("房间右上角边界（世界坐标）")]
    public Vector2 roomTopRight;

    [Tooltip("房间边界留白比例")]
    [Range(0f, 0.3f)]
    public float roomEdgePadding = 0.05f;

    [Header("========== 动态视野 ==========")]
    [Tooltip("Boss战时是否动态拉远摄像机视野，让玩家看到更大的战斗范围")]
    public bool dynamicFOV = true;

    [Tooltip("基础视野大小")]
    [Range(3f, 10f)]
    public float baseCameraSize = 5f;

    [Tooltip("战斗中拉远后的最大视野")]
    [Range(5f, 15f)]
    public float combatCameraSize = 6.5f;

    [Tooltip("视野过渡平滑速度")]
    [Range(0.5f, 5f)]
    public float fovSmoothSpeed = 2f;

    private Camera cam;
    private Vector3 targetCameraPosition;
    private float camHeight;
    private float camWidth;
    private float currentLookAheadX;
    private float targetLookAheadX;
    private Vector3 previousTargetPosition;
    private float targetOrthoSize;

    private void Awake()
    {
        cam = GetComponent<Camera>();
        if (cam == null)
        {
            cam = Camera.main;
        }

        targetOrthoSize = baseCameraSize;
    }

    private void Start()
    {
        InitializeCamera();
    }

    private void LateUpdate()
    {
        if (target == null)
        {
            return;
        }

        UpdateLookAhead();
        UpdateCameraFOV();
        UpdateCameraPosition();
    }

    private void InitializeCamera()
    {
        if (target != null)
        {
            targetCameraPosition = target.position + cameraOffset;
            transform.position = targetCameraPosition;
            previousTargetPosition = target.position;
        }

        if (cam != null)
        {
            cam.orthographicSize = baseCameraSize;
        }

        camHeight = cam.orthographicSize * 2f;
        camWidth = camHeight * cam.aspect;
        targetOrthoSize = baseCameraSize;
    }

    /// <summary>
    /// 更新横向预瞄：根据玩家的移动方向，计算摄像机应该提前偏移多少
    /// </summary>
    private void UpdateLookAhead()
    {
        if (!enableLookAhead || target == null)
        {
            targetLookAheadX = 0f;
            return;
        }

        // 计算玩家这一帧的移动量
        float deltaX = target.position.x - previousTargetPosition.x;

        // 根据移动方向设置预瞄偏移
        if (Mathf.Abs(deltaX) > 0.01f)
        {
            // 向右移动 → 摄像机往右看
            // 向左移动 → 摄像机往左看
            targetLookAheadX = Mathf.Sign(deltaX) * lookAheadOffset;
        }

        // 平滑过渡预瞄值，避免摄像机突然跳变
        currentLookAheadX = Mathf.Lerp(currentLookAheadX, targetLookAheadX, Time.deltaTime * lookAheadSmoothSpeed);

        previousTargetPosition = target.position;
    }

    /// <summary>
    /// 动态调整摄像机视野大小：战斗中视野拉大，平时恢复
    /// </summary>
    private void UpdateCameraFOV()
    {
        if (!dynamicFOV || cam == null)
        {
            return;
        }

        // 根据Boss是否存在决定视野大小
        float desiredSize = bossTarget != null && bossTarget.gameObject.activeInHierarchy ? combatCameraSize : baseCameraSize;

        targetOrthoSize = Mathf.Lerp(targetOrthoSize, desiredSize, Time.deltaTime * fovSmoothSpeed);
        cam.orthographicSize = targetOrthoSize;

        // 视野大小变了，宽高也要重新算
        camHeight = cam.orthographicSize * 2f;
        camWidth = camHeight * cam.aspect;
    }

    /// <summary>
    /// 核心逻辑：死区检测 + 摄像机移动
    /// </summary>
    private void UpdateCameraPosition()
    {
        // 计算实际的跟随目标点
        Vector3 followTarget = CalculateFollowTarget();

        // 计算死区半宽
        float deadZoneWidth = camWidth * (0.5f - horizontalDeadZone);
        // 计算死区半高
        float deadZoneHeight = camHeight * (0.5f - verticalDeadZone);

        // 死区边界
        float zoneMinX = targetCameraPosition.x - deadZoneWidth;
        float zoneMaxX = targetCameraPosition.x + deadZoneWidth;
        float zoneMinY = targetCameraPosition.y - deadZoneHeight;
        float zoneMaxY = targetCameraPosition.y + deadZoneHeight;

        // 玩家是否超出死区？
        bool needMoveX = followTarget.x < zoneMinX || followTarget.x > zoneMaxX;
        bool needMoveY = followTarget.y < zoneMinY || followTarget.y > zoneMaxY;

        if (needMoveX || needMoveY)
        {
            Vector3 newTarget = targetCameraPosition;

            if (needMoveX)
            {
                newTarget.x = followTarget.x - Mathf.Sign(followTarget.x - targetCameraPosition.x) * deadZoneWidth;
            }

            if (needMoveY)
            {
                newTarget.y = followTarget.y - Mathf.Sign(followTarget.y - targetCameraPosition.y) * deadZoneHeight;
            }

            targetCameraPosition = newTarget;
        }

        // 房间边界限制
        targetCameraPosition = ClampToRoomBounds(targetCameraPosition);

        // 保持Z轴
        targetCameraPosition.z = cameraOffset.z;

        // 平滑移动到目标位置
        transform.position = Vector3.Lerp(transform.position, targetCameraPosition, Time.deltaTime * snapSpeed);
    }

    /// <summary>
    /// 计算摄像机实际应该跟随的位置
    /// 综合考虑：玩家位置 + 预瞄偏移 + Boss位置（如果启用）
    /// </summary>
    private Vector3 CalculateFollowTarget()
    {
        // 基础跟随目标 = 玩家位置 + 预瞄偏移（X轴方向）
        Vector3 baseTarget = target.position;
        baseTarget.x += currentLookAheadX;

        if (!trackBoss || bossTarget == null || bossInfluence <= 0f)
        {
            return baseTarget;
        }

        // 在玩家和Boss之间取加权中点
        Vector3 bossPos = bossTarget.position;
        Vector3 midPoint = Vector3.Lerp(baseTarget, bossPos, bossInfluence);

        return midPoint;
    }

    /// <summary>
    /// 限制摄像机位置不超出Boss房间边界
    /// </summary>
    private Vector3 ClampToRoomBounds(Vector3 position)
    {
        if (!clampToRoomBounds)
        {
            return position;
        }

        // 计算摄像机中心点能到达的合法范围
        float edgePaddingX = camWidth * roomEdgePadding;
        float edgePaddingY = camHeight * roomEdgePadding;

        float minX = roomBottomLeft.x + camWidth / 2f - edgePaddingX;
        float maxX = roomTopRight.x - camWidth / 2f + edgePaddingX;
        float minY = roomBottomLeft.y + camHeight / 2f - edgePaddingY;
        float maxY = roomTopRight.y - camHeight / 2f + edgePaddingY;

        // 房间比摄像机还小的情况
        if (minX > maxX)
        {
            minX = maxX = (roomBottomLeft.x + roomTopRight.x) / 2f;
        }
        if (minY > maxY)
        {
            minY = maxY = (roomBottomLeft.y + roomTopRight.y) / 2f;
        }

        position.x = Mathf.Clamp(position.x, minX, maxX);
        position.y = Mathf.Clamp(position.y, minY, maxY);

        return position;
    }

    #region 外部接口

    /// <summary>
    /// 设置摄像机跟随目标
    /// </summary>
    public void SetTarget(Transform targetTransform)
    {
        target = targetTransform;
        if (target != null)
        {
            targetCameraPosition = target.position + cameraOffset;
            transform.position = targetCameraPosition;
            previousTargetPosition = target.position;
        }
    }

    /// <summary>
    /// 设置Boss目标，用于追瞳计算
    /// </summary>
    public void SetBossTarget(Transform bossTransform)
    {
        bossTarget = bossTransform;
    }

    /// <summary>
    /// 设置房间边界（世界坐标）
    /// </summary>
    public void SetRoomBounds(Vector2 bottomLeft, Vector2 topRight)
    {
        roomBottomLeft = bottomLeft;
        roomTopRight = topRight;
    }

    /// <summary>
    /// 立即跳转到玩家位置（无平滑过渡）
    /// </summary>
    public void SnapToTarget()
    {
        if (target != null)
        {
            targetCameraPosition = target.position + cameraOffset;
            targetCameraPosition.z = cameraOffset.z;
            transform.position = targetCameraPosition;
            previousTargetPosition = target.position;
        }
    }

    #endregion
}
