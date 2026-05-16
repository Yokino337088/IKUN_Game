using UnityEngine;
using TangmenFramework;

/// <summary>
/// 迷宫关卡沉浸式摄像机跟随（死区模式）
/// 摄像机只在玩家走出安全区域时才移动，适合格子瞬移类迷宫游戏
/// 挂载到主摄像机上即可
/// </summary>
public class MazeCameraFollow : MonoBehaviour
{
    [Header("========== 跟随目标 ==========")]
    [Tooltip("要跟随的玩家Transform，留空则通过 SetTarget 设置")]
    public Transform target;

    [Header("========== 死区设置 ==========")]
    [Tooltip("死区比例，0=无死区（始终跟随），0.3=玩家走到屏幕边缘30%处才移动摄像机")]
    [Range(0f, 0.5f)]
    public float deadZone = 0.25f;

    [Tooltip("摄像机重新定位的平滑速度，值越大越快")]
    [Range(1f, 20f)]
    public float snapSpeed = 8f;

    [Header("========== 视野设置 ==========")]
    [Tooltip("摄像机与玩家的固定偏移（Z轴控制）")]
    public Vector3 cameraOffset = new Vector3(0, 0, -10f);

    [Tooltip("是否自动计算摄像机视野大小来适配迷宫")]
    public bool autoAdjustSize = true;

    [Tooltip("摄像机视野最小尺寸")]
    [Range(3f, 10f)]
    public float minCameraSize = 4f;

    [Tooltip("摄像机视野最大尺寸")]
    [Range(5f, 20f)]
    public float maxCameraSize = 10f;

    [Tooltip("迷宫边缘留白比例（0=紧贴边缘，0.1=留10%边距）")]
    [Range(0f, 1f)]
    public float edgePadding = 0.15f;

    [Header("========== 边界限制 ==========")]
    [Tooltip("是否限制摄像机不超出迷宫边界")]
    public bool clampToMazeBounds = true;

    /// <summary>
    /// 摄像机脚本
    /// </summary>
    private Camera cam;

    /// <summary>
    /// 摄像机看向的目标位置
    /// </summary>
    private Vector3 targetCameraPosition;

    /// <summary>
    /// 摄像机宽
    /// </summary>
    private float camHeight;

    /// <summary>
    /// 摄像机高
    /// </summary>
    private float camWidth;

    private void Awake()
    {
        //获取摄像机的脚本引用
        cam = GetComponent<Camera>();
        if (cam == null)
        {
            cam = Camera.main;
        }
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

        UpdateCameraPosition();
    }

    /// <summary>
    /// 核心逻辑：检查玩家是否超出死区，超出的才移动摄像机
    /// </summary>
    private void UpdateCameraPosition()
    {
        Vector3 playerPos = target.position;

        // 计算死区半宽
        float deadZoneWidth = camWidth * (0.5f - deadZone);
        //计算死区半高
        float deadZoneHeight = camHeight * (0.5f - deadZone);

        //计算死区边界的最小X值
        float zoneMinX = targetCameraPosition.x - deadZoneWidth;
        //计算死区边界的最大X值
        float zoneMaxX = targetCameraPosition.x + deadZoneWidth;
        //计算死区边界的最小Y值
        float zoneMinY = targetCameraPosition.y - deadZoneHeight;
        //计算死区边界的最大Y值
        float zoneMaxY = targetCameraPosition.y + deadZoneHeight;

        // 玩家是否超出死区？
        bool needMoveX = playerPos.x < zoneMinX || playerPos.x > zoneMaxX;
        bool needMoveY = playerPos.y < zoneMinY || playerPos.y > zoneMaxY;

        //如果需要移动摄像机
        if (needMoveX || needMoveY)
        {
            // 计算摄像机应该去的位置（把玩家放回死区边界）
            Vector3 newTarget = targetCameraPosition;

            //Mathf.Sign这一个函数的作用是提取一个数的符号，扔掉它的具体大小
            //当传入值大于0时返回正1，小于0时返回负1，等于0时返回0

            if (needMoveX)
            {
                //先用玩家X坐标减去摄像机目标X坐标，然后使用Mathf.Sign判断正负
                //正数→玩家在摄像机的右边
                //负数→玩家在摄像机的左边

                //如果玩家在摄像机右边
                //摄像机的目标位置 = 玩家位置往左退一个死区半宽(因为摄像机计算的是自己的中心点的坐标)
                //效果：摄像机向右追过去，但追到玩家刚好站在死区右边界就停了。

                //如果玩家在摄像机左边
                //摄像机的目标位置 = 玩家位置往右加一个死区半宽。
                //效果：摄像机向左追过去，追到玩家刚好站在死区左边界就停了。
                newTarget.x = playerPos.x - Mathf.Sign(playerPos.x - targetCameraPosition.x) * deadZoneWidth;
            }

            if (needMoveY)
            {
                //这里的代码含义也是和上面的同理
                newTarget.y = playerPos.y - Mathf.Sign(playerPos.y - targetCameraPosition.y) * deadZoneHeight;
            }

            targetCameraPosition = newTarget;
        }

        // 迷宫边界限制
        targetCameraPosition = ClampToMazeBounds(targetCameraPosition);

        // 保持Z轴
        targetCameraPosition.z = cameraOffset.z;

        // 平滑移动到目标位置
        transform.position = Vector3.Lerp(transform.position , targetCameraPosition , Time.deltaTime * snapSpeed);
    }

    /// <summary>
    /// 限制摄像机位置不超出迷宫边界
    /// </summary>
    private Vector3 ClampToMazeBounds(Vector3 position)
    {
        //如果不需要限制范围或者迷宫数据还没有初始化，就直接返回
        if (!clampToMazeBounds || !MazeDataManager.Instance.IsInitialized())
        {
            return position;
        }

        //获取迷宫的宽高
        int mazeWidth = MazeDataManager.Instance.GetWidth();
        int mazeHeight = MazeDataManager.Instance.GetHeight();

        //计算迷宫左下角的世界坐标
        Vector3 bottomLeft = MazeCoordinateConverter.GridToWorld(0, 0);
        //计算迷宫右上角的世界坐标
        Vector3 topRight = MazeCoordinateConverter.GridToWorld(mazeWidth - 1, mazeHeight - 1);

        //特别注意！这里你必须要认真看一遍
        //这四行代码在计算摄像机中心点的合法活动范围
        //关键前提是:摄像机的position是它的中心点，但它实际看到的是一个 camWidth × camHeight 的矩形区域

        //公式如下：
        //摄像机中心下限 = 迷宫左下角 + 半屏
        //摄像机中心上限 = 迷宫右上角 - 半屏

        //为什么要 ± 半个屏幕？因为摄像机的原点在中间，它天然会向两侧延伸半个屏幕。
        //如果不加这个偏移，直接用迷宫边角来限制，摄像机的一半画面就会拍到迷宫外面。

        //计算摄像机中心点能够到达的最左边界
        float minX = bottomLeft.x + camWidth / 2f;
        //计算摄像机中心的能够到达的最右边界
        float maxX = topRight.x - camWidth / 2f;
        //计算摄像机中心的能够到达的最下边界
        float minY = bottomLeft.y + camHeight / 2f;
        //计算摄像机中心的能够到达的最上边界
        float maxY = topRight.y - camHeight / 2f;

        //文档里面有详细的图解过程，你可以去看一下


        //特殊情况：迷宫比摄像机还小
        //如果迷宫比摄像机视野小，居中
        if (minX > maxX)
        {
            minX = maxX = (bottomLeft.x + topRight.x) / 2f;
        }
        if (minY > maxY)
        {
            minY = maxY = (bottomLeft.y + topRight.y) / 2f;
        }

        //利用Mathf.Clamp限制摄像机的坐标点范围
        position.x = Mathf.Clamp(position.x, minX, maxX);
        position.y = Mathf.Clamp(position.y, minY, maxY);

        return position;
    }

    /// <summary>
    /// 初始化摄像机
    /// </summary>
    private void InitializeCamera()
    {
        if (target != null)
        {
            targetCameraPosition = target.position + cameraOffset;
            transform.position = targetCameraPosition;
        }

        if (autoAdjustSize && MazeDataManager.Instance.IsInitialized())
        {
            AdjustCameraSize();
        }

        //设置摄像机的宽高
        camHeight = cam.orthographicSize * 2f;
        camWidth = camHeight * cam.aspect;
    }

    /// <summary>
    /// 根据迷宫大小自动调整摄像机视野
    /// </summary>
    private void AdjustCameraSize()
    {
        // =================================================================
        // 目标：根据迷宫的实际网格尺寸（列数 × 行数），自动调整正交摄像机的
        //       orthographicSize，使得整个迷宫能完整显示在屏幕上，并留有一定边距。
        // =================================================================

        int mazeWidth = MazeDataManager.Instance.GetWidth();      // 迷宫列数（世界坐标 X 方向）
        int mazeHeight = MazeDataManager.Instance.GetHeight();    // 迷宫行数（世界坐标 Y 方向）

        // -----------------------------------------------------------------
        // 1. 计算屏幕宽高比
        //    aspectRatio = 屏幕宽度 / 屏幕高度，例如 1920/1080 ≈ 1.78
        //    正交摄像机中，可视高度 = orthographicSize × 2
        //    可视宽度 = 可视高度 × aspectRatio
        // -----------------------------------------------------------------
        float aspectRatio = (float)Screen.width / Screen.height;

        // -----------------------------------------------------------------
        // 2. 分别计算"按宽度适配"和"按高度适配"所需的 orthographicSize
        //
        //    【按宽度适配】:
        //      让迷宫宽度 = 可视宽度
        //      mazeWidth = orthographicSize × 2 × aspectRatio
        //      → orthographicSize = mazeWidth / (2 × aspectRatio)
        //
        //    【按高度适配】:
        //      让迷宫高度 = 可视高度
        //      mazeHeight = orthographicSize × 2
        //      → orthographicSize = mazeHeight / 2
        // -----------------------------------------------------------------
        float sizeForWidth = mazeWidth / (2f * aspectRatio);
        float sizeForHeight = mazeHeight / 2f;

        // -----------------------------------------------------------------
        // 3. 取两者的最大值
        //    如果选较小的值，则另一方向会溢出屏幕，出现裁剪。
        //    取 Max 保证 宽度和高度的迷宫内容都装得下。
        //    举例：迷宫宽 20、高 10，屏幕 16:9 → sizeForWidth=5.625, sizeForHeight=5
        //          Max 后取 5.625，宽正好，高有剩余空间（上下黑边/留白）。
        // -----------------------------------------------------------------
        float requiredSize = Mathf.Max(sizeForWidth, sizeForHeight);

        // -----------------------------------------------------------------
        // 4. 加上边缘留白
        //    edgePadding 是用户配置的比例（默认 0.15 = 15%）。
        //    乘以 (1 + edgePadding × 2) 的含义：
        //      上下各加 edgePadding 倍的迷宫高度 → 总共增加 edgePadding × 2 倍
        //      例如 edgePadding=0.15，放大系数 = 1 + 0.3 = 1.3
        //      可视范围变为原来的 1.3 倍，迷宫周围就留出了 15% 的边距。
        // -----------------------------------------------------------------
        requiredSize *= (1f + edgePadding * 2f);

        // -----------------------------------------------------------------
        // 5. 限制范围
        //    将计算出的 orthographicSize 夹在 [minCameraSize, maxCameraSize] 之间，
        //    防止迷宫太大时摄像机拉太远，或迷宫太小时摄像机贴太近。
        // -----------------------------------------------------------------
        float targetOrthoSize = Mathf.Clamp(requiredSize, minCameraSize, maxCameraSize);

        if (cam != null)
        {
            cam.orthographicSize = targetOrthoSize;
        }

        // -----------------------------------------------------------------
        // 6. 同步更新 camHeight / camWidth
        //    camHeight = 摄像机可视的世界高度（orthographicSize × 2）
        //    camWidth  = 摄像机可视的世界宽度（camHeight × aspectRatio）
        //    后续 ClampToMazeBounds 和死区计算都依赖这两个值。
        // -----------------------------------------------------------------
        camHeight = cam.orthographicSize * 2f;
        camWidth = camHeight * cam.aspect;
    }

    #region 外部接口

    /// <summary>
    /// 设置摄像机目标
    /// </summary>
    public void SetTarget(Transform targetTransform)
    {
        target = targetTransform;
        if (target != null)
        {
            targetCameraPosition = target.position + cameraOffset;
            transform.position = targetCameraPosition;
        }
    }

    /// <summary>
    /// 外部调用：立即跳转到玩家位置
    /// </summary>
    public void SnapToTarget()
    {
        if (target != null)
        {
            targetCameraPosition = target.position + cameraOffset;
            targetCameraPosition.z = cameraOffset.z;
            transform.position = targetCameraPosition;
        }
    }

    /// <summary>
    /// 外部调用：刷新迷宫边界
    /// </summary>
    public void RefreshMazeBounds()
    {
        if (autoAdjustSize && MazeDataManager.Instance.IsInitialized())
        {
            AdjustCameraSize();
        }
        SnapToTarget();
    }

    #endregion


}

//该摄像机跟随采用的是“死区模式”,不像传统的摄像机是玩家动一步，摄像机跟一步
//摄像机不会每帧都追着玩家跑，而是只有当玩家走出屏幕中心的一块安全区域时，摄像机才移动一次，把玩家重新框回安全区内。

//打个比方：想象你在一个房间里拍照，你拿着相机不动，被拍的人只有在走到画面边缘快出框的时候，你才转动相机把他拉回来。
//这就是死区模式的核心体验。

//那死区到底是什么呢
//死区是以摄像机中心为原点画出的一个矩形区域。这个矩形比摄像机实际看到的范围要小。
//变量deadZone控制了死区的大小
//deadZone = 0 → 死区撑满整个屏幕 → 玩家永远在死区边缘 → 摄像机每帧都在跟（等效传统跟随）
//deadZone = 0.25 → 死区占屏幕中央的 50% 区域 → 玩家在这个区域内自由移动，摄像机不动

//玩家站在屏幕中央，可以自由移动屏幕宽度一定的范围，摄像机完全不动。
//只有当玩家走到屏幕的一定范围之外时，摄像机才开始追。
