using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Cysharp.Threading.Tasks;
using TangmenFramework;

public class AStarMgr : BaseManager<AStarMgr>
{
    private AStarNode[,] nodes; // 存储所有节点的二维数组   

    private List<AStarNode> openList = new List<AStarNode>(); // 开放列表，存储待检查的节点
    private List<AStarNode> closedList = new List<AStarNode>(); // 闭合列表，存储已检查的节点

    private int mapWidth; // 地图宽度
    private int mapHeight; // 地图高度


    private AStarMgr() { }

    public void InitMapInfo(int w, int h)
    {
        mapWidth = w;
        mapHeight = h;
        // 初始化节点数组
        nodes = new AStarNode[w, h];

        for (int x = 0; x < w; x++)
        {
            for (int y = 0; y < h; y++)
            {
                // 默认所有节点都是可行走的
                // 这里可以根据实际情况设置节点类型
                nodes[x, y] = new AStarNode(x, y, E_NodeType.Walk);
            }
        }
    }

    /// <summary>
    /// 重载，传入2维数组进行地图的创建
    /// </summary>
    /// <param name="mapInfo"></param>
    public void InitMapInfo(int[,] mapInfo)
    {
        mapWidth = mapInfo.GetLength(0);
        mapHeight = mapInfo.GetLength(1);
        nodes = new AStarNode[mapWidth, mapHeight];

        for (int x = 0; x < mapWidth; x++)
        {
            for (int y = 0; y < mapHeight; y++)
            {
                if (mapInfo[x, y] == 0)
                    nodes[x, y] = new AStarNode(x, y, E_NodeType.Walk);
                else
                    nodes[x, y] = new AStarNode(x, y, E_NodeType.Stop);
            }
        }
    }

    /// <summary>
    /// 真正的寻路函数（异步版本，避免大地图卡顿）
    /// </summary>
    /// <param name="startPos">起点</param>
    /// <param name="endPos">终点</param>
    /// <param name="isOptimize">是否需要优化路径</param>
    /// <returns></returns>
    public async UniTask<List<AStarNode>> FindPathAsync(Vector2 startPos, Vector2 endPos, bool isOptimize = false,bool isUseChebyshev = false)
    {
        //首先判断 传入的两个点 是否合法
        //1.首先 要在地图范围内

        //如果不合法 应该直接 返回null 意味着不能寻路
        if (startPos.x < 0 || startPos.x >= mapWidth || startPos.y < 0 || startPos.y >= mapHeight
          || endPos.x < 0 || endPos.x >= mapWidth || endPos.y < 0 || endPos.y >= mapHeight)
        {
            LogSystem.Error("起点或终点不在地图范围内");
            return null;
        }

        //2.要不是阻挡
        //应该得到起点和终点 对应的格子
        AStarNode start = nodes[(int)startPos.x, (int)startPos.y];
        AStarNode end = nodes[(int)endPos.x, (int)endPos.y];
        if (start.nodeType == E_NodeType.Stop)
        {
            LogSystem.Error($"起点是阻挡,起点坐标 X:{start.x},Y:{start.y}");
            return null;
        }

        if (end.nodeType == E_NodeType.Stop)
        {
            LogSystem.Error($"终点是阻挡,终点坐标 X:{end.x},Y:{end.y}");
            return null;
        }

        //清理上一次寻路的数据，避免影响本次寻路
        openList.Clear(); // 清空开放列表
        closedList.Clear(); // 清空闭合列表

        //把起点放入关闭列表中
        start.father = null; // 起点没有父节点
        start.g = 0; // 起点的g值为0
        start.h = isUseChebyshev ? CalculateChebyshevDistance(start, end) : CalculateManhattanDistance(start,end); // 计算启发值
        start.f = start.g + start.h; // 计算f值
        AStarHeap.PushNode(openList, start); // 将起点添加到开放列表中

        int iterationCount = 0; // 迭代计数器

        while (true)
        {
            //死路判断，如果开放列表为空还没有找到终点，说明没有可行走的路径
            if (openList.Count == 0)
            {
                UnityEngine.Debug.Log("没有可行走的路径，开放列表为空");
                return null;
            }

            //从开放列表中获取f值最小的节点
            AStarNode current = AStarHeap.PopNode(openList);
            closedList.Add(current); // 将当前节点添加到闭合列表中

            //如果这个点已经是终点了 那么得到最终结果返回出去
            if (current == end)
            {
                List<AStarNode> path = RetracePath(start, end);
                // 路径优化
                path = isOptimize ? OptimizePath(path) : path;
                return path; // 返回找到的路径    
            }

            //从当前节点开始 找周围的点 并放入开启列表中
            //左上
            //FindNearlyNode2OpenList(current.x - 1, current.y - 1, 1.4f, current, end,isUseChebyshev);
            //上
            FindNearlyNode2OpenList(current.x, current.y - 1, 1f, current, end, isUseChebyshev);
            //右上
            //FindNearlyNode2OpenList(current.x + 1, current.y - 1, 1.4f, current, end, isUseChebyshev);
            //左
            FindNearlyNode2OpenList(current.x - 1, current.y, 1f, current, end, isUseChebyshev);
            //右
            FindNearlyNode2OpenList(current.x + 1, current.y, 1f, current, end, isUseChebyshev);
            //左下
            //FindNearlyNode2OpenList(current.x - 1, current.y + 1, 1.4f, current, end, isUseChebyshev);
            //下
            FindNearlyNode2OpenList(current.x, current.y + 1, 1f, current, end, isUseChebyshev);
            //右下
            //FindNearlyNode2OpenList(current.x + 1, current.y + 1, 1.4f, current, end, isUseChebyshev);

            // 每迭代一定次数后让出主线程，避免卡顿
            iterationCount++;
            if (iterationCount % 100 == 0)
            {
                await UniTask.Yield();
            }
        }
    }

    /// <summary>
    /// 同步版本的寻路函数（保留兼容性）
    /// </summary>
    public List<AStarNode> FindPath(Vector2 startPos, Vector2 endPos, bool isOptimize = false, bool isUseChebyshev = false)
    {
        //首先判断 传入的两个点 是否合法
        //1.首先 要在地图范围内

        //如果不合法 应该直接 返回null 意味着不能寻路
        if (startPos.x < 0 || startPos.x >= mapWidth || startPos.y < 0 || startPos.y >= mapHeight
          || endPos.x < 0 || endPos.x >= mapWidth || endPos.y < 0 || endPos.y >= mapHeight)
        {
            UnityEngine.Debug.Log("起点或终点不在地图范围内");
            return null;
        }

        //2.要不是阻挡
        //应该得到起点和终点 对应的格子
        AStarNode start = nodes[(int)startPos.x, (int)startPos.y];
        AStarNode end = nodes[(int)endPos.x, (int)endPos.y];
        if (start.nodeType == E_NodeType.Stop || end.nodeType == E_NodeType.Stop)
        {
            UnityEngine.Debug.Log("起点或终点是阻挡");
            return null;
        }

        //清理上一次寻路的数据，避免影响本次寻路
        openList.Clear(); // 清空开放列表
        closedList.Clear(); // 清空闭合列表

        //把起点放入关闭列表中
        start.father = null; // 起点没有父节点
        start.g = 0; // 起点的g值为0
        start.h = isUseChebyshev ? CalculateChebyshevDistance(start, end) : CalculateManhattanDistance(start, end); // 计算启发值
        start.f = start.g + start.h; // 计算f值
        AStarHeap.PushNode(openList, start); // 将起点添加到开放列表中

        while (true)
        {
            //死路判断，如果开放列表为空还没有找到终点，说明没有可行走的路径
            if (openList.Count == 0)
            {
                UnityEngine.Debug.Log("没有可行走的路径，开放列表为空");
                return null;
            }

            //从开放列表中获取f值最小的节点
            AStarNode current = AStarHeap.PopNode(openList);
            closedList.Add(current); // 将当前节点添加到闭合列表中

            //如果这个点已经是终点了 那么得到最终结果返回出去
            if (current == end)
            {
                List<AStarNode> path = RetracePath(start, end);
                // 路径优化
                path = isOptimize ? OptimizePath(path) : path;
                return path; // 返回找到的路径    
            }

            //从当前节点开始 找周围的点 并放入开启列表中
            //左上
            FindNearlyNode2OpenList(current.x - 1, current.y - 1, 1.4f, current, end, isUseChebyshev);
            //上
            FindNearlyNode2OpenList(current.x, current.y - 1, 1f, current, end, isUseChebyshev);
            //右上
            FindNearlyNode2OpenList(current.x + 1, current.y - 1, 1.4f, current, end, isUseChebyshev);
            //左
            FindNearlyNode2OpenList(current.x - 1, current.y, 1f, current, end, isUseChebyshev);
            //右
            FindNearlyNode2OpenList(current.x + 1, current.y, 1f, current, end, isUseChebyshev);
            //左下
            FindNearlyNode2OpenList(current.x - 1, current.y + 1, 1.4f, current, end, isUseChebyshev);
            //下
            FindNearlyNode2OpenList(current.x, current.y + 1, 1f, current, end, isUseChebyshev);
            //右下
            FindNearlyNode2OpenList(current.x + 1, current.y + 1, 1.4f, current, end, isUseChebyshev);
        }
    }

    /// <summary>
    /// 回溯路径
    /// </summary>
    private List<AStarNode> RetracePath(AStarNode start, AStarNode end)
    {
        List<AStarNode> path = new List<AStarNode>();
        AStarNode current = end;

        while (current != start)
        {
            path.Add(current);
            current = current.father;
        }
        path.Add(start);
        path.Reverse();
        return path;
    }

    /// <summary>
    /// 路径优化
    /// </summary>
    private List<AStarNode> OptimizePath(List<AStarNode> path)
    {
        if (path.Count <= 2)
            return path;

        // 第一步：去除共线节点
        List<AStarNode> optimizedPath = ReduceCollinearNodes(path);

        // 第二步：双指针剪枝
        optimizedPath = DoublePointPruning(optimizedPath);

        return optimizedPath;
    }

    /// <summary>
    /// 去除共线节点
    /// </summary>
    private List<AStarNode> ReduceCollinearNodes(List<AStarNode> path)
    {
        List<AStarNode> result = new List<AStarNode>();
        if (path.Count <= 2)
            return path;

        result.Add(path[0]);

        // 计算初始方向向量
        int lastDx = path[1].x - path[0].x;
        int lastDy = path[1].y - path[0].y;

        for (int i = 2; i < path.Count; i++)
        {
            int currentDx = path[i].x - path[i - 1].x;
            int currentDy = path[i].y - path[i - 1].y;

            // 如果方向发生变化，记录前一个节点
            if (currentDx != lastDx || currentDy != lastDy)
            {
                result.Add(path[i - 1]);
                lastDx = currentDx;
                lastDy = currentDy;
            }
        }

        // 添加终点
        result.Add(path[path.Count - 1]);
        return result;
    }

    /// <summary>
    /// 双指针剪枝
    /// </summary>
    private List<AStarNode> DoublePointPruning(List<AStarNode> path)
    {
        if (path.Count <= 3)
            return path;

        List<AStarNode> result = new List<AStarNode>();
        int startIndex = 0;

        while (startIndex < path.Count - 1)
        {
            result.Add(path[startIndex]);
            int endIndex = path.Count - 1;

            while (endIndex > startIndex + 1)
            {
                if (CanConnectDirectly(path[startIndex], path[endIndex]))
                {
                    break;
                }
                endIndex--;
            }

            startIndex = endIndex;
        }

        // 添加终点
        if (result[result.Count - 1] != path[path.Count - 1])
        {
            result.Add(path[path.Count - 1]);
        }

        return result;
    }

    /// <summary>
    /// 判断两点之间是否可以直接相连（无障碍物）
    /// </summary>
    private bool CanConnectDirectly(AStarNode start, AStarNode end)
    {
        int x1 = start.x;
        int y1 = start.y;
        int x2 = end.x;
        int y2 = end.y;

        // 计算两点之间的方向向量
        int dx = x2 - x1;
        int dy = y2 - y1;

        // 情况1：两点在同一列
        if (x1 == x2)
        {
            int minY = Mathf.Min(y1, y2);
            int maxY = Mathf.Max(y1, y2);

            // 检查中间的所有节点
            for (int newY = minY + 1; newY < maxY; newY++)
            {
                // 如果遇到障碍物，返回false
                if (nodes[x1, newY].nodeType == E_NodeType.Stop)
                {
                    return false;
                }
            }
            return true;
        }

        // 情况2：两点在同一行
        if (y1 == y2)
        {
            int minX = Mathf.Min(x1, x2);
            int maxX = Mathf.Max(x1, x2);

            // 检查中间的所有节点
            for (int newX = minX + 1; newX < maxX; newX++)
            {
                // 如果遇到障碍物，返回false
                if (nodes[newX, y1].nodeType == E_NodeType.Stop)
                {
                    return false;
                }
            }
            return true;
        }

        // 情况3：两点在标准正方形的对角线上
        if (dx == dy || dx == -dy)
        {
            // 处理不同方向的对角线
            if (dx > 0 && dy > 0) // 右下斜
            {
                int newX = x1;
                int newY = y1;
                while (true)
                {
                    // 检查对角线经过的节点
                    if (nodes[newX, newY].nodeType == E_NodeType.Stop ||
                        nodes[newX, newY + 1].nodeType == E_NodeType.Stop ||
                        nodes[newX + 1, newY].nodeType == E_NodeType.Stop)
                    {
                        return false;
                    }

                    // 移动到下一个对角线位置
                    newX += 1;
                    newY += 1;

                    // 到达终点
                    if (newX == x2 && newY == y2)
                    {
                        return true;
                    }
                }
            }

            if (dx > 0 && dy < 0) // 左下斜
            {
                int newX = x1;
                int newY = y1;
                while (true)
                {
                    // 检查对角线经过的节点
                    if (nodes[newX, newY].nodeType == E_NodeType.Stop ||
                        nodes[newX, newY - 1].nodeType == E_NodeType.Stop ||
                        nodes[newX + 1, newY].nodeType == E_NodeType.Stop)
                    {
                        return false;
                    }

                    // 移动到下一个对角线位置
                    newX += 1;
                    newY -= 1;

                    // 到达终点
                    if (newX == x2 && newY == y2)
                    {
                        return true;
                    }
                }
            }

            if (dx < 0 && dy < 0) // 左上斜
            {
                int newX = x2;
                int newY = y2;
                while (true)
                {
                    // 检查对角线经过的节点
                    if (nodes[newX, newY].nodeType == E_NodeType.Stop ||
                        nodes[newX, newY + 1].nodeType == E_NodeType.Stop ||
                        nodes[newX + 1, newY].nodeType == E_NodeType.Stop)
                    {
                        return false;
                    }

                    // 移动到下一个对角线位置
                    newX += 1;
                    newY += 1;

                    // 到达起点
                    if (newX == x1 && newY == y1)
                    {
                        return true;
                    }
                }
            }

            if (dx < 0 && dy > 0) // 右上斜
            {
                int newX = x2;
                int newY = y2;
                while (true)
                {
                    // 检查对角线经过的节点
                    if (nodes[newX, newY].nodeType == E_NodeType.Stop ||
                        nodes[newX, newY - 1].nodeType == E_NodeType.Stop ||
                        nodes[newX + 1, newY].nodeType == E_NodeType.Stop)
                    {
                        return false;
                    }

                    // 移动到下一个对角线位置
                    newX += 1;
                    newY -= 1;

                    // 到达起点
                    if (newX == x1 && newY == y1)
                    {
                        return true;
                    }
                }
            }
        }

        // 情况4：一般直线（y = kx + b）
        // 计算直线方程的参数
        float fx1 = x1 + 0.5f;  // 节点中心坐标
        float fy1 = y1 + 0.5f;
        float fx2 = x2 + 0.5f;
        float fy2 = y2 + 0.5f;

        float k = (fy1 - fy2) / (fx1 - fx2);
        float b = fy1 - (k * fx1);

        // 确定遍历范围
        int smallX = Mathf.Min(x1, x2);
        int smallY = Mathf.Min(y1, y2);
        int diffX = Mathf.Abs(x1 - x2);
        int diffY = Mathf.Abs(y1 - y2);

        // 沿X方向检查
        for (int i = 0; i < diffX; i++)
        {
            float dynamicY = (k * (smallX + i + 1)) + b;

            // 检查是否经过网格交点
            if (dynamicY - Mathf.Floor(dynamicY) <= 0.01f)
            {
                // 处于交点，检查周围4个节点
                int yInt = Mathf.FloorToInt(dynamicY);
                if (nodes[smallX + i, yInt].nodeType == E_NodeType.Stop ||
                    nodes[smallX + i + 1, yInt].nodeType == E_NodeType.Stop ||
                    nodes[smallX + i, yInt - 1].nodeType == E_NodeType.Stop ||
                    nodes[smallX + i + 1, yInt - 1].nodeType == E_NodeType.Stop)
                {
                    return false;
                }
            }
            else
            {
                // 不处于交点，检查周围2个节点
                int yInt = Mathf.FloorToInt(dynamicY);
                if (nodes[smallX + i, yInt].nodeType == E_NodeType.Stop ||
                    nodes[smallX + i + 1, yInt].nodeType == E_NodeType.Stop)
                {
                    return false;
                }
            }
        }

        // 沿Y方向检查
        for (int i = 0; i < diffY; i++)
        {
            float dynamicX = (smallY + i + 1 - b) / k;

            // 检查是否经过网格交点
            if (dynamicX - Mathf.Floor(dynamicX) <= 0.01f)
            {
                // 处于交点，检查周围4个节点
                int xInt = Mathf.FloorToInt(dynamicX);
                if (nodes[xInt, smallY + i].nodeType == E_NodeType.Stop ||
                    nodes[xInt, smallY + i + 1].nodeType == E_NodeType.Stop ||
                    nodes[xInt - 1, smallY + i].nodeType == E_NodeType.Stop ||
                    nodes[xInt - 1, smallY + i + 1].nodeType == E_NodeType.Stop)
                {
                    return false;
                }
            }
            else
            {
                // 不处于交点，检查周围2个节点
                int xInt = Mathf.FloorToInt(dynamicX);
                if (nodes[xInt, smallY + i].nodeType == E_NodeType.Stop ||
                    nodes[xInt, smallY + i + 1].nodeType == E_NodeType.Stop)
                {
                    return false;
                }
            }
        }

        // 所有检查都通过，两点可以直接相连
        return true;
    }

    /// <summary>
    /// 将周围的节点添加到开放列表中
    /// </summary>
    /// <param name="x"></param>
    /// <param name="y"></param>
    /// <param name="g"></param>
    /// <param name="father"></param>
    /// <param name="end"></param>
    private void FindNearlyNode2OpenList(int x, int y, float g, AStarNode father, AStarNode end, bool isUseChebyshev = false)
    {
        // 检查边界
        if (x < 0 || x >= mapWidth || y < 0 || y >= mapHeight)
            return;

        // 获取当前节点
        AStarNode currentNode = nodes[x, y];

        // 如果是阻挡 或者 已经在关闭列表中 则直接返回
        if (currentNode == null || currentNode.nodeType == E_NodeType.Stop || closedList.Contains(currentNode))
            return;

        // 计算新的g值
        float newG = father.g + g;

        // 如果节点不在开放列表中，或者找到了更优的路径
        if (!openList.Contains(currentNode) || newG < currentNode.g)
        {
            currentNode.father = father; // 设置父节点
            currentNode.g = newG; // 更新g值
            currentNode.h = isUseChebyshev ? CalculateChebyshevDistance(currentNode, end) : CalculateManhattanDistance(currentNode,end); // 计算启发值
            currentNode.f = currentNode.g + currentNode.h; // 更新f值

            if (!openList.Contains(currentNode))
            {
                AStarHeap.PushNode(openList, currentNode); // 添加到开放列表
            }
            else
            {
                AStarHeap.UpdateNodePriority(openList, currentNode); // 更新优先级
            }
        }
    }

    /// <summary>
    /// 计算切比雪夫距离
    /// </summary>
    private float CalculateChebyshevDistance(AStarNode node, AStarNode end)
    {
        float dx = Mathf.Abs(node.x - end.x);
        float dy = Mathf.Abs(node.y - end.y);

        if (dx == 0 || dy == 0)
        {
            return dx + dy;
        }
        else
        {
            return dx + dy - (2 - Mathf.Sqrt(2)) * Mathf.Min(dx, dy);
        }
    }

    /// <summary>
    /// 计算曼哈顿距离
    /// </summary>
    /// <param name="node"></param>
    /// <param name="end"></param>
    /// <returns></returns>
    private float CalculateManhattanDistance(AStarNode node, AStarNode end)
    {
        return Mathf.Abs(node.x - end.x) + Mathf.Abs(node.y - end.y);
    }
}