using TangmenFramework;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// 迷宫坐标转换器（静态类）
/// 功能：将世界坐标转换为迷宫格子坐标，或将格子坐标转换为世界坐标
/// 使用 MazeDataManager 获取迷宫数据，降低耦合度
/// </summary>
public static class MazeCoordinateConverter
{
    #region 坐标转换方法

    /// <summary>
    /// 将世界坐标转换为迷宫格子坐标
    /// </summary>
    /// <param name="worldPosition">世界坐标（如角色的transform.position）</param>
    /// <returns>迷宫格子坐标（Vector2Int格式）</returns>
    public static Vector2Int WorldToGrid(Vector3 worldPosition)
    {
        // 检查是否已初始化
        if (!MazeDataManager.Instance.IsInitialized())
        {
            LogSystem.Warning("MazeCoordinateConverter: 迷宫数据尚未初始化！");
            return Vector2Int.zero;
        }

        Tilemap tilemap = MazeDataManager.Instance.GetTilemap();
        if (tilemap == null)
        {
            LogSystem.Warning("MazeCoordinateConverter: Tilemap为空！");
            return Vector2Int.zero;
        }

        // 使用Tilemap的坐标转换功能
        Vector3Int gridPosition = tilemap.WorldToCell(worldPosition);

        // 转换为Vector2Int并返回
        return new Vector2Int(gridPosition.x, gridPosition.y);
    }

    /// <summary>
    /// 将迷宫格子坐标转换为世界坐标（格子中心）
    /// </summary>
    /// <param name="gridPosition">迷宫格子坐标</param>
    /// <returns>世界坐标（格子中心位置）</returns>
    public static Vector3 GridToWorld(Vector2Int gridPosition)
    {
        // 检查是否已初始化
        if (!MazeDataManager.Instance.IsInitialized())
        {
            LogSystem.Warning("MazeCoordinateConverter: 迷宫数据尚未初始化！");
            return Vector3.zero;
        }

        Tilemap tilemap = MazeDataManager.Instance.GetTilemap();
        if (tilemap == null)
        {
            LogSystem.Warning("MazeCoordinateConverter: Tilemap为空！");
            return Vector3.zero;
        }

        // 转换为Vector3Int
        Vector3Int cellPosition = new Vector3Int(gridPosition.x, gridPosition.y, 0);

        // 获取格子的世界坐标（中心位置）
        return tilemap.GetCellCenterWorld(cellPosition);
    }

    /// <summary>
    /// 将迷宫格子坐标转换为世界坐标（格子中心）- 重载
    /// </summary>
    /// <param name="x">格子X坐标</param>
    /// <param name="y">格子Y坐标</param>
    /// <returns>世界坐标（格子中心位置）</returns>
    public static Vector3 GridToWorld(int x, int y)
    {
        return GridToWorld(new Vector2Int(x, y));
    }

    #endregion

    #region 角色位置相关方法

    /// <summary>
    /// 获取角色当前所在的格子坐标
    /// </summary>
    /// <param name="characterTransform">角色的Transform</param>
    /// <returns>当前所在的格子坐标</returns>
    public static Vector2Int GetCharacterGridPosition(Transform characterTransform)
    {
        if (characterTransform == null)
        {
            Debug.LogWarning("MazeCoordinateConverter: characterTransform为空！");
            return Vector2Int.zero;
        }

        return WorldToGrid(characterTransform.position);
    }

    /// <summary>
    /// 将角色移动到指定格子坐标
    /// </summary>
    /// <param name="characterTransform">角色的Transform</param>
    /// <param name="targetGridPosition">目标格子坐标</param>
    public static void MoveCharacterToGrid(Transform characterTransform, Vector2Int targetGridPosition)
    {
        if (characterTransform == null)
        {
            Debug.LogWarning("MazeCoordinateConverter: characterTransform为空！");
            return;
        }

        // 获取目标格子的世界坐标
        Vector3 targetWorldPosition = GridToWorld(targetGridPosition);

        // 保持角色的Z轴不变
        targetWorldPosition.z = characterTransform.position.z;

        // 移动角色
        characterTransform.position = targetWorldPosition;
    }

    /// <summary>
    /// 将角色移动到起点
    /// </summary>
    /// <param name="characterTransform">角色的Transform</param>
    public static void MoveCharacterToStart(Transform characterTransform)
    {
        if (!MazeDataManager.Instance.IsInitialized())
        {
            Debug.LogWarning("MazeCoordinateConverter: 迷宫数据尚未初始化！");
            return;
        }

        Vector2Int startPos = MazeDataManager.Instance.GetStartPosition();
        MoveCharacterToGrid(characterTransform, startPos);
    }

    /// <summary>
    /// 将角色移动到终点
    /// </summary>
    /// <param name="characterTransform">角色的Transform</param>
    public static void MoveCharacterToEnd(Transform characterTransform)
    {
        if (!MazeDataManager.Instance.IsInitialized())
        {
            Debug.LogWarning("MazeCoordinateConverter: 迷宫数据尚未初始化！");
            return;
        }

        Vector2Int endPos = MazeDataManager.Instance.GetEndPosition();
        MoveCharacterToGrid(characterTransform, endPos);
    }

    #endregion

    #region 辅助检查方法

    /// <summary>
    /// 检查指定格子坐标是否在迷宫范围内
    /// </summary>
    /// <param name="gridPosition">格子坐标</param>
    /// <returns>是否在迷宫范围内</returns>
    public static bool IsPositionInMaze(Vector2Int gridPosition)
    {
        return MazeDataManager.Instance.IsInRange(gridPosition);
    }

    /// <summary>
    /// 检查指定格子坐标是否是可行走的地面
    /// </summary>
    /// <param name="gridPosition">格子坐标</param>
    /// <returns>是否是可行走地面</returns>
    public static bool IsWalkable(Vector2Int gridPosition)
    {
        return MazeDataManager.Instance.IsWalkable(gridPosition);
    }

    /// <summary>
    /// 获取随机的可行走格子坐标
    /// </summary>
    /// <returns>随机可行走格子坐标，如果找不到则返回(-1,-1)</returns>
    public static Vector2Int GetRandomWalkablePosition()
    {
        if (!MazeDataManager.Instance.IsInitialized())
        {
            Debug.LogWarning("MazeCoordinateConverter: 迷宫数据尚未初始化！");
            return new Vector2Int(-1, -1);
        }

        int width = MazeDataManager.Instance.GetWidth();
        int height = MazeDataManager.Instance.GetHeight();

        // 尝试找到一个可行走的随机位置
        for (int attempt = 0; attempt < 100; attempt++)
        {
            int randomX = Random.Range(0, width);
            int randomY = Random.Range(0, height);
            Vector2Int randomPos = new Vector2Int(randomX, randomY);

            if (MazeDataManager.Instance.IsWalkable(randomPos))
            {
                return randomPos;
            }
        }

        Debug.LogWarning("MazeCoordinateConverter: 未能找到可行走的随机位置！");
        return new Vector2Int(-1, -1);
    }

    #endregion
}
