using System.Collections.Generic;

public static class AStarHeap
{
    /// <summary>
    /// 上浮操作维护最小堆性质
    /// </summary>
    public static void BubbleUp(List<AStarNode> nodes, int index)
    {
        while (index > 0)
        {
            int parent = (index - 1) / 2;
            if (nodes[index].f >= nodes[parent].f)
            {
                break;
            }

            // 交换节点
            AStarNode temp = nodes[index];
            nodes[index] = nodes[parent];
            nodes[parent] = temp;

            index = parent;
        }
    }

    /// <summary>
    /// 下沉操作维护最小堆性质
    /// </summary>
    public static void BubbleDown(List<AStarNode> nodes, int i)
    {
        int n = nodes.Count;
        while (true)
        {
            int left = 2 * i + 1;
            int right = 2 * i + 2;
            int smallest = i;

            if (left < n && nodes[left].f < nodes[smallest].f)
            {
                smallest = left;
            }
            if (right < n && nodes[right].f < nodes[smallest].f)
            {
                smallest = right;
            }
            if (smallest == i)
            {
                break;
            }

            // 交换节点
            AStarNode temp = nodes[i];
            nodes[i] = nodes[smallest];
            nodes[smallest] = temp;

            i = smallest;
        }
    }

    /// <summary>
    /// 向堆中添加元素
    /// </summary>
    public static void PushNode(List<AStarNode> nodes, AStarNode node)
    {
        nodes.Add(node);
        BubbleUp(nodes, nodes.Count - 1);
    }

    /// <summary>
    /// 从堆中取出最小元素
    /// </summary>
    public static AStarNode PopNode(List<AStarNode> nodes)
    {
        if (nodes.Count == 0)
        {
            return null;
        }

        AStarNode minNode = nodes[0];
        int lastIndex = nodes.Count - 1;
        nodes[0] = nodes[lastIndex];
        nodes.RemoveAt(lastIndex);

        if (nodes.Count > 0)
        {
            BubbleDown(nodes, 0);
        }

        return minNode;
    }

    /// <summary>
    /// 查找节点在堆中的索引
    /// </summary>
    public static int FindNodeIndex(List<AStarNode> nodes, AStarNode target)
    {
        for (int i = 0; i < nodes.Count; i++)
        {
            if (nodes[i] == target)
            {
                return i;
            }
        }
        return -1;
    }

    /// <summary>
    /// 更新节点优先级并重新平衡堆
    /// </summary>
    public static void UpdateNodePriority(List<AStarNode> nodes, AStarNode target)
    {
        int index = FindNodeIndex(nodes, target);
        if (index == -1)
        {
            return;
        }

        // 先尝试上浮再下沉，确保堆平衡
        BubbleUp(nodes, index);
        BubbleDown(nodes, index);
    }
}