using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using TangmenFramework;

/// <summary>
/// 基于DFS(深度优先搜索)算法的运行时迷宫生成器
/// 功能：自动生成连通迷宫、可控制边缘包围墙、可控制迷宫复杂度、自动设置起点终点
/// 规则：0=可行走地面  1=不可行走墙体
/// </summary>
public class MazeGenerator_DFS : MonoBehaviour
{
    [Header("========== 基础地图设置 ==========")]
    [Tooltip("地图总宽度，推荐使用奇数，迷宫生成更规整")]
    public int mapWidth = 21;

    [Tooltip("地图总高度，推荐使用奇数，迷宫生成更规整")]
    public int mapHeight = 11;

    [Header("========== 边缘包围墙设置 ==========")]
    [Tooltip("地图最外层生成几层墙体，用于包围整个迷宫")]
    public int borderWallLayerCount = 2;

    [Header("========== 迷宫难度设置 ==========")]
    [Tooltip("迷宫复杂度/路径曲折度：0=最简单（直接一条路），1=最复杂（大量分支和死路）")]
    [Range(0, 1)] 
    public float mazeComplexity = 0.5f;

    [Header("========== 瓦片资源设置（直接拖Sprite） ==========")]
    [Tooltip("直接拖入地面图片（Sprite）即可，不用创建Tile")]
    public Sprite groundSprite;   // 改成 Sprite

    [Tooltip("直接拖入墙体图片（Sprite）即可，不用创建Tile")]
    public Sprite wallSprite;     // 改成 Sprite

    [Header("终点对应的预制体")]
    public GameObject endObj;

    [Header("========== 自动生成的起点终点 ==========")]
    [Tooltip("迷宫起点坐标（运行时自动赋值）")]
    public Vector2Int startPosition;

    [Tooltip("迷宫终点坐标（运行时自动赋值）")]
    public Vector2Int endPosition;

    // ====================== 私有变量 ======================
    private Tilemap currentTilemap;

    // 迷宫数据二维数组：存储整个地图的墙/地面信息
    private int[,] mazeDataGrid;

    // 实际使用的地图宽高
    private int actualMapWidth;
    private int actualMapHeight;

    // 内部动态创建的Tile（无需手动创建）
    private Tile groundTile;
    private Tile wallTile;
    private Tile endTile;

    // 上一次寻路路径的原始瓦片记录，用于新寻路前恢复
    private Dictionary<Vector3Int, TileBase> lastPathOriginalTiles;

    [Header("生成的篮球个数")]
    public int ballCount = 4;



    /// <summary>
    /// 游戏启动时自动执行
    /// </summary>
    private void Start()
    {
        // 获取当前物体上的Tilemap组件
        currentTilemap = GetComponent<Tilemap>();

        // 根据Sprite动态生成Tile
        CreateTilesFromSprites();

        // 调用运行时迷宫生成方法
        GenerateMazeAtRuntime();

        //初始化A星寻路的地图
        AStarMgr.Instance.InitMapInfo(mazeDataGrid);

        //注册事件
        RegisterEvent();
    }

    private void Update() 
    {
        
    }

    private void OnDestroy()
    {
        //注销事件
        LogOutEvent();
    }

    //注册事件
    private void RegisterEvent()
    {
        EventCenter.Instance.AddEventListener<Vector2, Vector2>(MyEventTypeString.PlayerFindPathEvent, AstarFindPathWrapper);
        EventCenter.Instance.AddEventListener(MyEventTypeString.MazeTask2InitEvent, SetDestinationTile);
    }

    //注销事件
    private void LogOutEvent()
    {
        EventCenter.Instance.RemoveEventListener<Vector2, Vector2>(MyEventTypeString.PlayerFindPathEvent, AstarFindPathWrapper);
        EventCenter.Instance.RemoveEventListener(MyEventTypeString.MazeTask2InitEvent, SetDestinationTile);
    }

    /// <summary>
    /// 提供给玩家用于寻路的方法，通过事件触发,这里要特别注意，UniTaskVoid并不能转换为C#system库中的Action委托
    /// 所以必须要包裹一层
    /// </summary>
    /// <param name="startPos"></param>
    /// <param name="endPos"></param>
    /// <returns></returns>
    private async UniTaskVoid AstarFindPath(Vector2 startPos,Vector2 endPos)
    {
        // 如果上一次寻路还有没清除的路径，先恢复原始瓦片
        if (lastPathOriginalTiles != null && lastPathOriginalTiles.Count > 0)
        {
            //遍历
            foreach (var kvp in lastPathOriginalTiles)
            {
                //复原瓦片
                Vector3Int tilePosition = kvp.Key;
                TileBase originalTile = kvp.Value;
                if (originalTile != null)
                {
                    currentTilemap.SetTile(tilePosition, originalTile);
                }
                else
                {
                    currentTilemap.SetTile(tilePosition, null);
                }
                //等一帧防止主线程卡顿
                await UniTask.DelayFrame(1);
            }
            lastPathOriginalTiles = null;
        }

        List<AStarNode> path = await AStarMgr.Instance.FindPathAsync(startPos, endPos, false);
        // 检查是否找到路径
        if (path == null || path.Count == 0)
        {
            LogSystem.Warning("未找到从起点到终点的路径！");
            return;
        }

        LogSystem.Info($"找到路径，路径长度：{path.Count}个节点");
        
        // 创建绿色瓦片用于显示路径
        Tile pathTile = CreateColoredTile(groundSprite, Color.green);
        
        // 保存路径上每个位置的原始瓦片，用于之后恢复
        Dictionary<Vector3Int, TileBase> originalTiles = new Dictionary<Vector3Int, TileBase>();

        // 遍历路径上的所有节点，每隔25毫秒将一个路径点设置为绿色
        foreach (AStarNode node in path)
        {
            Vector3Int tilePosition = new Vector3Int(node.x, node.y, 0);

            // 保存原始瓦片
            originalTiles[tilePosition] = currentTilemap.GetTile(tilePosition);
            
            // 设置为绿色路径瓦片
            currentTilemap.SetTile(tilePosition, pathTile);

            // 等待25毫秒
            await UniTask.Delay(25);
        }

        // 将当前路径的原始瓦片记录存到类字段，供下次寻路时恢复
        lastPathOriginalTiles = originalTiles;
        
        // 路径显示完成，等待10秒
        //await UniTask.Delay(10000);
        
        //LogSystem.Info("开始清除路径，恢复原始瓦片...");
        
        //// 清除路径，恢复原始瓦片
        //foreach (var kvp in lastPathOriginalTiles)
        //{
        //    Vector3Int tilePosition = kvp.Key;
        //    TileBase originalTile = kvp.Value;
            
        //    if (originalTile != null)
        //    {
        //        // 恢复原始瓦片
        //        currentTilemap.SetTile(tilePosition, originalTile);
        //    }
        //    else
        //    {
        //        // 如果原来没有瓦片，就移除
        //        currentTilemap.SetTile(tilePosition, null);
        //    }
        //    //防止主线程卡顿，要等一帧
        //    await UniTask.DelayFrame(1);
        //}
        
        //// 清除完毕，清空记录
        //lastPathOriginalTiles.Clear();
        //lastPathOriginalTiles = null;
        //LogSystem.Info("路径清除完成！");
    }

    /// <summary>
    /// 事件系统的包装方法，用于适配 Action<T1, T2> 委托
    /// </summary>
    private void AstarFindPathWrapper(Vector2 startPos, Vector2 endPos)
    {
        AstarFindPath(startPos, endPos).Forget();
    }

    /// <summary>
    /// 根据用户拖入的Sprite，自动创建Tile
    /// 不用手动创建Tile资源，脚本自动处理
    /// </summary>
    private void CreateTilesFromSprites()
    {
        // 创建地面Tile
        groundTile = ScriptableObject.CreateInstance<Tile>();
        groundTile.sprite = groundSprite;

        // 创建墙体Tile
        wallTile = ScriptableObject.CreateInstance<Tile>();
        wallTile.sprite = wallSprite;

        
    }

    /// <summary>
    /// 【总入口】运行时生成完整迷宫（所有步骤的总调度方法）
    /// </summary>
    public void GenerateMazeAtRuntime()
    {
        // 验证地图尺寸是否足够容纳边缘墙体和迷宫
        ValidateMapSize();

        // 1. 初始化实际使用的地图尺寸
        actualMapWidth = mapWidth;
        actualMapHeight = mapHeight;

        // 2. 创建迷宫数据二维数组
        mazeDataGrid = new int[actualMapWidth, actualMapHeight];

        // 步骤1：把整个地图全部填充为墙体
        FillAllMapWithWall();

        // 步骤2：自动设置起点和终点，保证一定可以通行
        // 【重要】必须在DFS之前设置，因为DFS会使用起点位置
        SetStartAndEndPosition();

        // 步骤3：使用DFS算法挖通道路，生成连通的迷宫结构
        // 【重要】DFS会从起点开始生成迷宫
        GenerateMazeByDFS();

        // 步骤3.5：【关键保障】确保起点和终点之间一定有通路
        EnsurePathFromStartToEnd();

        // 步骤4：生成多层边缘包围墙，让地图更美观（不覆盖起点终点）
        CreateBorderWallLayers();

        // 步骤5：将迷宫数据绘制到Unity的瓦片地图上
        DrawMazeToTilemap();

        // 步骤6：注册迷宫数据到数据管理器（供其他系统使用）
        RegisterMazeDataToManager();

        //步骤7:创建篮球
        SelectWalkablePoints();

        //创建玩家
        GeneratePlayerObj();

    }

    /// <summary>
    /// 将迷宫数据注册到MazeDataManager
    /// </summary>
    private void RegisterMazeDataToManager()
    {
        MazeDataManager.Instance.RegisterMazeData(
            mazeDataGrid,
            currentTilemap,
            actualMapWidth,
            actualMapHeight,
            startPosition,
            endPosition
        );
    }

    /// <summary>
    /// 验证地图尺寸是否足够容纳边缘墙体和迷宫
    /// </summary>
    private void ValidateMapSize()
    {
        // 最小迷宫尺寸要求：边缘墙体 * 2 + 3（起点终点和中间格子）
        int minSize = borderWallLayerCount * 2 + 3;

        if (mapWidth < minSize)
        {
            Debug.LogWarning($"地图宽度 {mapWidth} 小于最小要求 {minSize}，已自动调整为 {minSize}");
            mapWidth = minSize;
        }

        if (mapHeight < minSize)
        {
            Debug.LogWarning($"地图高度 {mapHeight} 小于最小要求 {minSize}，已自动调整为 {minSize}");
            mapHeight = minSize;
        }
    }

    /// <summary>
    /// 初始化：将整个地图全部填充为墙体（1）
    /// 原理：先全部堵死，再通过算法挖出路，保证迷宫连通性
    /// </summary>
    private void FillAllMapWithWall()
    {
        // 遍历所有X坐标
        for (int x = 0; x < actualMapWidth; x++)
        {
            // 遍历所有Y坐标
            for (int y = 0; y < actualMapHeight; y++)
            {
                // 全部设置为墙体(1)
                mazeDataGrid[x, y] = 1;
            }
        }
    }

    /// <summary>
    /// 【核心算法】DFS深度优先搜索生成迷宫
    /// 原理：从起点出发，随机向四周挖路，直到无法继续，保证全图连通
    /// </summary>
    private void GenerateMazeByDFS()
    {
        // 栈：用于存储DFS算法的行走路径
        Stack<Vector2Int> positionStack = new Stack<Vector2Int>();

        // 迷宫生成起始点：使用实际的起点位置
        Vector2Int startCell = new Vector2Int(startPosition.x, startPosition.y);

        // 将起始点设置为可行走地面(0)
        mazeDataGrid[startCell.x, startCell.y] = 0;

        // 将起始点压入栈中
        positionStack.Push(startCell);

        // 定义四个方向：上下左右
        // 每次移动2格：保证中间保留一格墙体，形成标准迷宫结构
        Vector2Int[] directions =
        {
            new Vector2Int(2, 0),  // 右
            new Vector2Int(-2, 0), // 左
            new Vector2Int(0, 2),  // 上
            new Vector2Int(0, -2)  // 下
        };

        // 循环：栈不为空，说明还有路径可以探索
        while (positionStack.Count > 0)
        {
            // 取出栈顶的当前单元格（不出栈）
            Vector2Int currentCell = positionStack.Pop();

            // 获取当前单元格所有未访问的邻居
            List<Vector2Int> unvisitedNeighbors = GetUnvisitedNeighbors(currentCell, directions);

            // 如果存在未访问的邻居
            if (unvisitedNeighbors.Count > 0)
            {
                // 将当前单元格重新压回栈
                positionStack.Push(currentCell);

                // 随机选择一个未访问的邻居单元格
                Vector2Int randomNeighbor = unvisitedNeighbors[Random.Range(0, unvisitedNeighbors.Count)];

                // 打通当前单元格与邻居单元格之间的墙体
                RemoveWallBetweenTwoCells(currentCell, randomNeighbor);

                // 将选中的邻居设置为可行走地面
                mazeDataGrid[randomNeighbor.x, randomNeighbor.y] = 0;

                // 复杂度控制：随机决定是否立即回退（让路径更曲折）
                // 值越大，越不容易立即回退 → 探索更深入 → 路径更曲折 → 迷宫更复杂
                if (Random.value < mazeComplexity)
                {
                    // 不立即回退，继续深入探索
                    positionStack.Push(randomNeighbor);
                }
                // 否则：立即回退，形成较短的分支
            }
        }
    }

    /// <summary>
    /// 获取当前单元格周围所有未被访问过的有效邻居
    /// </summary>
    /// <param name="currentCell">当前单元格坐标</param>
    /// <param name="directions">方向数组</param>
    /// <returns>未访问邻居列表</returns>
    private List<Vector2Int> GetUnvisitedNeighbors(Vector2Int currentCell, Vector2Int[] directions)
    {
        // 存储有效邻居的列表
        List<Vector2Int> neighborList = new List<Vector2Int>();

        // 遍历四个方向
        foreach (Vector2Int dir in directions)
        {
            // 计算邻居单元格的X坐标
            int neighborX = currentCell.x + dir.x;

            // 计算邻居单元格的Y坐标
            int neighborY = currentCell.y + dir.y;

            // 判断条件：
            // 1. 坐标在地图范围内
            // 2. 单元格是墙体(1)，代表未被访问
            if (IsPositionInMapRange(neighborX, neighborY) && mazeDataGrid[neighborX, neighborY] == 1)
            {
                // 添加到有效邻居列表
                neighborList.Add(new Vector2Int(neighborX, neighborY));
            }
        }

        // 返回所有有效未访问邻居
        return neighborList;
    }

    /// <summary>
    /// 打通两个单元格之间的墙体
    /// 原理：两个单元格间隔1格，将中间那格设置为地面即可通路
    /// </summary>
    private void RemoveWallBetweenTwoCells(Vector2Int firstCell, Vector2Int secondCell)
    {
        // 计算中间点X坐标
        int middleX = (firstCell.x + secondCell.x) / 2;

        // 计算中间点Y坐标
        int middleY = (firstCell.y + secondCell.y) / 2;

        // 将中间点设置为地面(0)，打通道路
        mazeDataGrid[middleX, middleY] = 0;
    }

    /// <summary>
    /// 生成多层边缘包围墙
    /// 功能：让地图边缘更整齐，防止迷宫贴边（不覆盖起点终点）
    /// </summary>
    private void CreateBorderWallLayers()
    {
        // ========== 生成上下边缘墙体 ==========
        for (int x = 0; x < actualMapWidth; x++)
        {
            // 顶部边缘
            for (int y = 0; y < borderWallLayerCount; y++)
            {
                // 只覆盖墙体，不覆盖起点和终点
                Vector2Int currentPos = new Vector2Int(x, y);
                if (currentPos != startPosition && currentPos != endPosition)
                {
                    mazeDataGrid[x, y] = 1;
                }
            }

            // 底部边缘
            for (int y = actualMapHeight - borderWallLayerCount; y < actualMapHeight; y++)
            {
                Vector2Int currentPos = new Vector2Int(x, y);
                if (currentPos != startPosition && currentPos != endPosition)
                {
                    mazeDataGrid[x, y] = 1;
                }
            }
        }

        // ========== 生成左右边缘墙体 ==========
        for (int y = 0; y < actualMapHeight; y++)
        {
            // 左侧边缘
            for (int x = 0; x < borderWallLayerCount; x++)
            {
                Vector2Int currentPos = new Vector2Int(x, y);
                if (currentPos != startPosition && currentPos != endPosition)
                {
                    mazeDataGrid[x, y] = 1;
                }
            }

            // 右侧边缘
            for (int x = actualMapWidth - borderWallLayerCount; x < actualMapWidth; x++)
            {
                Vector2Int currentPos = new Vector2Int(x, y);
                if (currentPos != startPosition && currentPos != endPosition)
                {
                    mazeDataGrid[x, y] = 1;
                }
            }
        }
    }

    /// <summary>
    /// 自动设置迷宫的起点和终点
    /// 保证：起点和终点一定在地面上，且一定连通
    /// </summary>
    private void SetStartAndEndPosition()
    {
        // 计算迷宫有效区域（考虑边缘墙体）
        int effectiveLeft = borderWallLayerCount;
        int effectiveRight = actualMapWidth - borderWallLayerCount - 1;
        int effectiveBottom = borderWallLayerCount;
        int effectiveTop = actualMapHeight - borderWallLayerCount - 1;

        // 起点：最左下角（确保是奇数坐标，DFS每次移动2格）
        int startX = effectiveLeft;
        if (startX % 2 == 0) startX++; // 确保是奇数

        int startY = effectiveBottom;
        if (startY % 2 == 0) startY++; // 确保是奇数

        // 终点：最右上角（确保是奇数坐标，DFS每次移动2格）
        int endX = effectiveRight;
        if (endX % 2 == 0) endX--; // 确保是奇数

        int endY = effectiveTop;
        if (endY % 2 == 0) endY--; // 确保是奇数

        // 起点：迷宫有效区域左下角
        startPosition = new Vector2Int(startX, startY);

        // 终点：迷宫有效区域右上角
        endPosition = new Vector2Int(endX, endY);

        // 强制将起点设置为地面
        mazeDataGrid[startPosition.x, startPosition.y] = 0;

        // 强制将终点设置为地面
        mazeDataGrid[endPosition.x, endPosition.y] = 0;
    }

    /// <summary>
    /// 将内存中的迷宫数据，渲染到Unity的Tilemap瓦片地图上
    /// </summary>
    private void DrawMazeToTilemap()
    {
        // 清空旧的瓦片
        currentTilemap.ClearAllTiles();
        

        // 遍历整个地图
        for (int x = 0; x < actualMapWidth; x++)
        {
            for (int y = 0; y < actualMapHeight; y++)
            {
                // 瓦片坐标
                Vector3Int tilePosition = new Vector3Int(x, y, 0);

                // 判断当前格子是地面还是墙体
                if (mazeDataGrid[x, y] == 0)
                {                    
                    // 普通地面放置普通瓦片
                    currentTilemap.SetTile(tilePosition, groundTile);     
                }
                else
                {
                    // 1 = 放置墙体瓦片
                    currentTilemap.SetTile(tilePosition, wallTile);
                }
            }
        }
    }

    /// <summary>
    /// 创建带指定颜色的瓦片
    /// </summary>
    private Tile CreateColoredTile(Sprite sprite, Color color)
    {
        Tile tile = ScriptableObject.CreateInstance<Tile>();
        tile.sprite = sprite;
        tile.color = color;
        return tile;
    }

    /// <summary>
    /// 判断坐标是否在合法的地图范围内
    /// </summary>
    private bool IsPositionInMapRange(int posX, int posY)
    {
        // X和Y都必须大于等于0，且小于地图宽高
        return posX >= 0 && posY >= 0 && posX < actualMapWidth && posY < actualMapHeight;
    }

    /// <summary>
    /// 【关键保障】确保起点和终点之间一定有一条通路
    /// 使用BFS检查是否连通，如果不连通就强制打通一条路径
    /// </summary>
    private void EnsurePathFromStartToEnd()
    {
        // 首先用BFS检查起点和终点是否连通
        bool isConnected = CheckIfPositionsConnected(startPosition, endPosition);

        // 如果已经连通，直接返回
        if (isConnected)
        {
            return;
        }

        // 如果不连通，强制打通一条从起点到终点的直线路径
        Debug.Log("起点终点不连通，正在强制打通路径...");
        ForceConnectStartAndEnd();
    }

    /// <summary>
    /// 使用BFS检查两个坐标是否连通
    /// </summary>
    private bool CheckIfPositionsConnected(Vector2Int from, Vector2Int to)
    {
        // 记录访问过的位置
        bool[,] visited = new bool[actualMapWidth, actualMapHeight];
        // BFS队列
        Queue<Vector2Int> queue = new Queue<Vector2Int>();

        // 从起点开始
        queue.Enqueue(from);
        visited[from.x, from.y] = true;

        // 四个方向（每次移动1格）
        Vector2Int[] directions =
        {
            new Vector2Int(1, 0),  // 右
            new Vector2Int(-1, 0), // 左
            new Vector2Int(0, 1),  // 上
            new Vector2Int(0, -1)  // 下
        };

        // BFS循环
        while (queue.Count > 0)
        {
            Vector2Int current = queue.Dequeue();

            // 到达终点，返回true
            if (current == to)
            {
                return true;
            }

            // 遍历四个方向
            foreach (Vector2Int dir in directions)
            {
                Vector2Int next = current + dir;

                // 检查：在范围内、未访问、是地面
                if (IsPositionInMapRange(next.x, next.y) && 
                    !visited[next.x, next.y] && 
                    mazeDataGrid[next.x, next.y] == 0)
                {
                    visited[next.x, next.y] = true;
                    queue.Enqueue(next);
                }
            }
        }

        // BFS结束未找到终点，不连通
        return false;
    }

    /// <summary>
    /// 强制打通一条从起点到终点的直线路径（L型路径）
    /// 先向右走到终点X，再向下走到终点Y
    /// </summary>
    private void ForceConnectStartAndEnd()
    {
        Vector2Int current = startPosition;

        // 第一步：水平方向，从起点走到终点的X坐标
        while (current.x != endPosition.x)
        {
            // 将当前位置设为地面
            mazeDataGrid[current.x, current.y] = 0;
            // 向终点X方向移动
            current.x += (endPosition.x > current.x) ? 1 : -1;
        }

        // 第二步：垂直方向，走到终点的Y坐标
        while (current.y != endPosition.y)
        {
            // 将当前位置设为地面
            mazeDataGrid[current.x, current.y] = 0;
            // 向终点Y方向移动
            current.y += (endPosition.y > current.y) ? 1 : -1;
        }

        // 最后确保终点也是地面
        mazeDataGrid[endPosition.x, endPosition.y] = 0;

        Debug.Log("已强制打通起点终点路径！");
    }

    /// <summary>
    /// 创建篮球
    /// </summary>
    private void GenerateBasketball(Vector2Int pos)
    {
        //先把网格坐标转成世界坐标
        Vector3 ballPos = MazeCoordinateConverter.GridToWorld(pos);
        //加载资源
        ABResMgr.Instance.LoadResAsync<GameObject>(MyAssetBundleName.第四章物体包, "迷宫篮球", (obj) =>
        {
            //实例化预制体
            MazeBall mazeBall = GameObject.Instantiate(obj,ballPos,Quaternion.identity).GetComponent<MazeBall>();
            //注册篮球的坐标数据
            MazeDataManager.Instance.RegisterBallData(pos,mazeBall);
        });
    }

    /// <summary>
    /// 随机选择可行走区域去创建篮球
    /// </summary>
    private void SelectWalkablePoints()
    {
        List<Vector2Int> result = new List<Vector2Int>(actualMapWidth * actualMapHeight / 4);
        // 遍历整个地图
        for (int x = 0; x < actualMapWidth; x++)
        {
            for (int y = 0; y < actualMapHeight; y++)
            {
                // 判断当前格子是地面还是墙体
                if (mazeDataGrid[x, y] == 0)
                {
                    //获取当前坐标
                    Vector2Int currentPos = new Vector2Int(x, y);

                    // 检查是否是起点或终点
                    if (currentPos == startPosition || currentPos == endPosition)
                    {
                        continue;
                    }
                    //加入结果集
                    result.Add(currentPos);
                }
            }
        }

        //生成对应的篮球
        for (int i = 0; i < ballCount; i++)
        {
            GenerateBasketball(result[Random.Range(0, result.Count)]);
        }
    }

    /// <summary>
    /// 设置终点的瓦片
    /// </summary>
    private void SetDestinationTile()
    {
        Instantiate(endObj, MazeCoordinateConverter.GridToWorld(endPosition), Quaternion.identity);

        // 如果上一次寻路还有没清除的路径，先恢复原始瓦片
        if (lastPathOriginalTiles != null && lastPathOriginalTiles.Count > 0)
        {
            //遍历
            foreach (var kvp in lastPathOriginalTiles)
            {
                //复原瓦片
                Vector3Int tilePosition = kvp.Key;
                TileBase originalTile = kvp.Value;
                if (originalTile != null)
                {
                    currentTilemap.SetTile(tilePosition, originalTile);
                }
                else
                {
                    currentTilemap.SetTile(tilePosition, null);
                }                
            }
            lastPathOriginalTiles = null;
        }
    }

    /// <summary>
    /// 生成玩家预制体
    /// </summary>
    private void GeneratePlayerObj()
    {
        //加载资源，注意这里的玩家预制体实例化是在异步回调当中执行的
        ABResMgr.Instance.LoadResAsync<GameObject>(MyAssetBundleName.第四章物体包, "迷宫玩家", (obj) =>
        {
            //实例化预制体
            GameObject playerObj = GameObject.Instantiate(obj, MazeCoordinateConverter.GridToWorld(startPosition), Quaternion.identity);
            //设置摄像机目标
            Camera.main.GetComponent<MazeCameraFollow>().SetTarget(playerObj.transform);
        });

        //玩家预制体实例化完成之后，会立即调用MazePlayer脚本的Awake()函数
        //此时这个脚本上依附的任务管理状态机初始化完成进入MazeTask1State这个状态是会调用一次RefreshNearestBall方法
        //这个时候就会调用IMazeTask中的playerMazePos属性，又会转到MazePlayer当中去调用MazeCoordinateConverter.WorldToGrid(transform.position)
        //那么返回的玩家坐标就会越界

        //为什么会越界？
        //Tilemap 的 SetTiles 刚刚执行完，但 Unity 内部的 Tilemap 数据结构还没刷新完。
        //DrawMazeToTilemap()一次性设置了大量 SetTile 调用。
        //虽然这些调用在逻辑上完成了，
        //但 Unity 的 Tilemap 组件内部可能有延迟刷新——它在当前帧末尾或下一帧开始时才会压缩边界重建内部chunk索引。

        //在AB包加载回调触发的那个时间点：
        //tilemap.WorldToCell(transform.position)可能因为 Tilemap 内部 chunk 还没刷新，返回了越界值

        //这就是为什么 FindPathAsync 打印的日志是" 起点 或终点不在地图范围内"，起点（玩家坐标）被WorldToGrid算错了。

        //所以MazeTask1State中的EnterState()进入该状态时要对RefreshNearestBall()方法进行延迟调用
    }
}