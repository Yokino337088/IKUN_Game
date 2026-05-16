using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public enum E_NodeType
{
    /// <summary>
    /// 可行走区域
    /// </summary>
    Walk,
    /// <summary>
    /// 不可行走区域
    /// </summary>
    Stop,
}

public class AStarNode
{
    public int x; // 节点的x坐标
    public int y; // 节点的y坐标

    public float f; // f值 = g值 + h值
    public float g; // 从起点到当前节点的实际成本
    public float h; // 从当前节点到终点的估计成本

    public AStarNode father; // 父节点，用于路径回溯

    public E_NodeType nodeType; // 节点类型，表示是否可行走

    public AStarNode(int x, int y, E_NodeType nodeType)
    {
        this.x = x;
        this.y = y;
        this.nodeType = nodeType;
    }
}
