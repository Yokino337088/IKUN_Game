using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections.Generic;
using TangmenFramework;
using Cysharp.Threading.Tasks;

/// <summary>
/// 迷宫数据管理器（静态类）
/// 用于管理和提供迷宫数据，降低组件间的耦合度
/// </summary>
public class MazeDataManager:BaseManager<MazeDataManager>
{
    #region 私有数据存储

    // 迷宫数据二维数组（0=地面，1=墙体）
    private int[,] mazeDataGrid;

    // 迷宫的Tilemap引用
    private Tilemap mazeTilemap;

    // 迷宫尺寸
    private int mazeWidth;
    private int mazeHeight;

    // 起点和终点坐标
    private Vector2Int startPosition;
    private Vector2Int endPosition;
   
    //储存篮球的字典
    private Dictionary<Vector2Int, MazeBall> mazeBallDic = new Dictionary<Vector2Int, MazeBall>();

    //当前选择的篮球坐标
    private Vector2Int nowBallPos = Vector2Int.zero;
    

    // 是否已初始化
    private bool isInitialized = false;


    private MazeDataManager()
    {

    }

    #endregion

    #region 数据注册方法

    /// <summary>
    /// 注册迷宫数据
    /// </summary>
    /// <param name="dataGrid">迷宫数据数组</param>
    /// <param name="tilemap">Tilemap引用</param>
    /// <param name="width">迷宫宽度</param>
    /// <param name="height">迷宫高度</param>
    /// <param name="startPos">起点坐标</param>
    /// <param name="endPos">终点坐标</param>
    public void RegisterMazeData(
        int[,] dataGrid,
        Tilemap tilemap,
        int width,
        int height,
        Vector2Int startPos,
        Vector2Int endPos)
    {
        mazeDataGrid = dataGrid;
        mazeTilemap = tilemap;
        mazeWidth = width;
        mazeHeight = height;
        startPosition = startPos;
        endPosition = endPos;
        isInitialized = true;

        LogSystem.Info("迷宫数据已注册到MazeDataManager");
    }

    /// <summary>
    /// 注册篮球的坐标数据
    /// </summary>
    /// <param name="pos"></param>
    public void RegisterBallData(Vector2Int pos,MazeBall mazeBall)
    {        
        mazeBallDic.Add(pos, mazeBall);
    }

    /// <summary>
    /// 清除所有数据
    /// </summary>
    public void ClearData()
    {
        mazeDataGrid = null;
        mazeTilemap = null;
        mazeWidth = 0;
        mazeHeight = 0;
        startPosition = Vector2Int.zero;
        endPosition = Vector2Int.zero;
        isInitialized = false;
    }

    #endregion

    #region 数据获取方法

    /// <summary>
    /// 检查是否已初始化
    /// </summary>
    public bool IsInitialized()
    {
        return isInitialized;
    }

    /// <summary>
    /// 获取迷宫宽度
    /// </summary>
    public int GetWidth()
    {
        return mazeWidth;
    }

    /// <summary>
    /// 获取迷宫高度
    /// </summary>
    public int GetHeight()
    {
        return mazeHeight;
    }

    /// <summary>
    /// 获取起点坐标
    /// </summary>
    public Vector2Int GetStartPosition()
    {
        return startPosition;
    }

    /// <summary>
    /// 获取终点坐标
    /// </summary>
    public Vector2Int GetEndPosition()
    {
        return endPosition;
    }

    /// <summary>
    /// 获取Tilemap引用
    /// </summary>
    public Tilemap GetTilemap()
    {
        return mazeTilemap;
    }

    /// <summary>
    /// 检查坐标是否在迷宫范围内
    /// </summary>
    public bool IsInRange(int x, int y)
    {
        return x >= 0 && x < mazeWidth && y >= 0 && y < mazeHeight;
    }

    /// <summary>
    /// 检查坐标是否在迷宫范围内（Vector2Int版本）
    /// </summary>
    public bool IsInRange(Vector2Int pos)
    {
        return IsInRange(pos.x, pos.y);
    }

    /// <summary>
    /// 检查指定坐标是否是可行走的地面
    /// </summary>
    public bool IsWalkable(int x, int y)
    {
        if (!IsInRange(x, y))
        {
            return false;
        }

        if (mazeDataGrid == null)
        {
            return false;
        }

        return mazeDataGrid[x, y] == 0;
    }

    /// <summary>
    /// 检查指定坐标是否是可行走的地面（Vector2Int版本）
    /// </summary>
    public bool IsWalkable(Vector2Int pos)
    {
        return IsWalkable(pos.x, pos.y);
    }

    /// <summary>
    /// 获取当前选择的篮球坐标,自动选择离玩家最近的篮球
    /// </summary>
    /// <returns></returns>
    private async UniTask<Vector2Int> GetNowBallPos(Vector2Int playerPos)
    {
        //已经有篮球点了，直接返回
        if(nowBallPos != Vector2Int.zero)
            return nowBallPos;

        //定义用于计算的临时变量
        Vector2Int nearestPos = Vector2Int.zero;
        int minPathCount = int.MaxValue;

        //遍历篮球字典
        foreach (var key in mazeBallDic.Keys)
        {
            //进行A星寻路
            var pathList = await AStarMgr.Instance.FindPathAsync(playerPos,key);
            //获取A星寻路的路径点数量
            //如果这里不判空的话，玩家位置到某个篮球之间没有通路，FindPathAsync 返回了 null，代码直接对null调.Count，就炸了
            if (pathList != null)
            {
                int pathCount = pathList.Count;
                //找最小值
                if (pathCount < minPathCount)
                {
                    minPathCount = pathCount;
                    nearestPos = key;
                }
            }
        }

        nowBallPos = nearestPos;
        return nowBallPos;
    }

    /// <summary>
    /// 异步刷新距离玩家最近的篮球（无返回值，可直接调用）
    /// </summary>
    public async UniTaskVoid RefreshNowBallPos(Vector2Int playerPos)
    {
        await GetNowBallPos(playerPos);
    }

    /// <summary>
    /// 获取缓存中的篮球位置（同步方法，不重新计算）
    /// </summary>
    public Vector2Int GetCachedBallPos()
    {
        return nowBallPos;
    }

    public int GetBallCount()
    {
        return mazeBallDic.Count;
    }

    /// <summary>
    /// 检测玩家是否到达当前目标篮球位置（纯坐标检测）
    /// </summary>
    /// <param name="playerPos">玩家当前位置</param>
    /// <returns>是否到达篮球位置</returns>
    public bool CheckPlayerReachedBall(Vector2Int playerPos)
    {        
        // 直接查字典就行了
        return mazeBallDic.ContainsKey(playerPos);
    }
    
   
    /// <summary>
    /// 检测玩家的位置是否位于终点
    /// </summary>
    /// <param name="playerPos"></param>
    /// <returns></returns>
    public bool CheckWhetherAtDestination(Vector2 playerPos)
    {
        Vector2Int playerGridPos = new Vector2Int((int)playerPos.x, (int)playerPos.y);
        
        return playerGridPos == endPosition;
    }

    /// <summary>
    /// 隐藏篮球
    /// </summary>
    /// <param name="ballPos"></param>
    public void HideMazeBall(Vector2Int ballPos)
    {
        //先隐藏
        mazeBallDic[ballPos].HideBall();
        //再移除        
        mazeBallDic.Remove(ballPos);
        //将当前的篮球坐标归零
        nowBallPos = Vector2Int.zero;
    }

    /// <summary>
    /// 检查是否完成了任务1
    /// </summary>
    /// <returns></returns>
    public bool CheckTask1IsCompleted()
    {
        //只需要去检查字典当中的元素数量就行了
        return mazeBallDic.Count == 0;
    }

    #endregion
}
