# 迷宫生成算法详解：DFS 与 BFS

> 本文档带你从零开始，看懂 [MazeGenerator_DFS.cs](file:///d:/Unity_Project/IKUN_Game/Assets/Scripts/游戏逻辑/4第四章_黄昏/迷宫关卡_1/迷宫生成/MazeGenerator_DFS.cs) 是如何用算法自动生成一个完整迷宫的。

---

## 一、先搞懂两个核心概念

在开始分析代码之前，我们先用生活中最直观的例子来理解 DFS 和 BFS。

### 1.1 什么是 DFS（深度优先搜索）？

想象你走进一个巨大的地下停车场找车。你选了**最左边那条路**，一直走到头，发现不是。于是你退回上一个岔路口，再选下一条路走到底，如此反复。

**一句话总结：一条路走到黑，撞墙了再回头。**  **递归实现，回溯再试，这个和树的前序遍历是一样的**

```
停车场示例：
        [入口]
       /  |  \
     A路  B路  C路
    / \      / \
  A1  A2   C1  C2

DFS走法：入口 → A路 → A1（到底了，回头）→ A2（到底了，回头）
        → B路（到底了，回头）→ C路 → C1（到底了，回头）→ C2（找到车！）
```

**关键特征：** 用"栈"来记录退路。每走一步就记下来，走不通了就回到最近的那个点继续走。

DFS 在这里的作用是**挖迷宫**——像一条蛇在地图上随机钻来钻去，钻出弯弯曲曲的路。

---

### 1.2 什么是 BFS（广度优先搜索）？

现在想象你在一个商场里找人。你不是一条路走到黑，而是**一层一层往外找**：先找完一层所有店铺，再去下一层找。

**一句话总结：一圈一圈扩散，像石头扔进水里泛起的涟漪。** **迭代实现，层层扩散，这个和树的层序遍历是一样的**

```
商场示例：
        你
       / | \
     1号 2号 3号     ← 第一圈，先找完这一层
    /|\  /|\  /|\
   4 5 6 7 8 9 A B C ← 第二圈，再找这一层

BFS走法：你 → 1号 → 2号 → 3号（第一圈找完）
        → 4号 → 5号 → 6号 → ...（再找第二圈）
```

**关键特征：** 用"队列"来排队。谁先来的谁先查，保证由近到远、一层层查。

BFS 在这里的作用是**检查连通性**——从起点出发，看看能不能走到终点。

---

### 1.3 栈 vs 队列：一句话区分

| | 栈（Stack） | 队列（Queue） |
|---|---|---|
| **口诀** | 后进先出（叠盘子） | 先进先出（排队伍） |
| **用什么** | DFS | BFS |
| **对应生活** | 书桌上的文件，后放的先拿 | 食堂排队，先来的先打饭 |
| **迷宫作用** | 挖路（钻出一条弯曲通道） | 查路（看两点间能不能走通） |

---

## 二、迷宫生成的总流程

[GenerateMazeAtRuntime](file:///d:/Unity_Project/IKUN_Game/Assets/Scripts/游戏逻辑/4第四章_黄昏/迷宫关卡_1/迷宫生成/MazeGenerator_DFS.cs#L213-L254) 是整个迷宫的"总调度"，它按顺序做了以下 7 件事：

```
                  开始
                   │
    ┌──────────────┴──────────────┐
    │  1. 把整张地图全部填成墙       │
    │     （相当于先造一堵实心墙）    │
    └──────────────┬──────────────┘
                   │
    ┌──────────────┴──────────────┐
    │  2. 设置起点（左下角）和       │
    │     终点（右上角）            │
    └──────────────┬──────────────┘
                   │
    ┌──────────────┴──────────────┐
    │  3. 【DFS算法】从起点开始      │
    │      随机挖路，形成迷宫        │
    └──────────────┬──────────────┘
                   │
    ┌──────────────┴──────────────┐
    │  4. 【BFS算法】检查起点和      │
    │     终点是否连通              │
    │     不通就强行打通一条路       │
    └──────────────┬──────────────┘
                   │
    ┌──────────────┴──────────────┐
    │  5. 给迷宫加上边缘围墙          │
    └──────────────┬──────────────┘
                   │
    ┌──────────────┴──────────────┐
    │  6. 把数据画到Tilemap上        │
    └──────────────┬──────────────┘
                   │
    ┌──────────────┴──────────────┐
    │  7. 放置篮球、生成玩家         │
    └──────────────┬──────────────┘
                   │
                  结束
```

**为什么不先挖路再加墙？** 因为先把整个地图全填成墙（全部设为1），再在这个"实心砖块"里面挖路（把部分1改成0），就天然保证了迷宫有边界。

---

## 三、DFS 挖路：迷宫的核心生成算法

### 3.1 视觉化理解

想象你在玩"贪吃蛇"，但这条蛇的规则不同：

1. 蛇每次跳 2 格（不是1格）
2. 蛇跳过去之后，会把"跳过的中间那格"也吃掉（变成路）
3. 蛇面前有 4 个方向可以跳，但只跳向没去过的地方
4. 如果蛇面前 4 个方向都去过了，它就退回到上一步重试
5. 重复以上步骤，直到所有能去的地方都去过了

```
蛇每次跳2格的原因：
  跳前：  [墙][墙][墙]     蛇在位置A
           A
 
  跳1格：[墙][路][墙]      A跳到C，中间B变成路
              B  
               
  跳后：  [墙][路][路]     
               B   C（蛇新位置）
  
  这样就能保证：路和路之间始终隔着一格墙，形成规整的迷宫结构。
```

### 3.2 代码逐步解析

**[GenerateMazeByDFS](file:///d:/Unity_Project/IKUN_Game/Assets/Scripts/游戏逻辑/4第四章_黄昏/迷宫关卡_1/迷宫生成/MazeGenerator_DFS.cs#L315-L373)** 方法：

```csharp
// 第一步：创建栈（用来记录"回去的路"）
Stack<Vector2Int> positionStack = new Stack<Vector2Int>();

// 第二步：从起点出发，把起点挖成路（设0）
Vector2Int startCell = new Vector2Int(startPosition.x, startPosition.y);
mazeDataGrid[startCell.x, startCell.y] = 0;

// 第三步：把起点压入栈
positionStack.Push(startCell);

// 第四步：定义四个方向，每次跳2格
Vector2Int[] directions =
{
    new Vector2Int(2, 0),  // 右
    new Vector2Int(-2, 0), // 左
    new Vector2Int(0, 2),  // 上
    new Vector2Int(0, -2)  // 下
};

// 第五步：循环挖路
while (positionStack.Count > 0)
{
    // 从栈顶取出当前位置
    Vector2Int currentCell = positionStack.Pop();

    // 找出当前位置周围"还没去过"的邻居
    List<Vector2Int> unvisitedNeighbors = GetUnvisitedNeighbors(currentCell, directions);

    if (unvisitedNeighbors.Count > 0)
    {
        // 把当前位置重新压栈（留作退路）
        positionStack.Push(currentCell);

        // 随机选一个没去过的邻居
        Vector2Int randomNeighbor = unvisitedNeighbors[Random.Range(0, unvisitedNeighbors.Count)];

        // 打通当前位置和邻居之间的墙
        RemoveWallBetweenTwoCells(currentCell, randomNeighbor);

        // 把邻居挖成路
        mazeDataGrid[randomNeighbor.x, randomNeighbor.y] = 0;

        // 【复杂度控制】随机决定是否继续深入
        if (Random.value < mazeComplexity)
        {
            positionStack.Push(randomNeighbor); // 继续深入 → 路径更长
        }
        // 否则立即回退 → 形成短分支
    }
    // 如果邻居都去过了，不压栈，自然回退
}
```

### 3.3 复杂度控制是怎么工作的？

```csharp
if (Random.value < mazeComplexity)
{
    positionStack.Push(randomNeighbor); // 继续深入
}
```

| mazeComplexity 值 | 行为 | 迷宫效果 |
|---|---|---|
| 0 | 几乎从不继续深入，挖一步就回退 | 分支很短，迷宫简单 |
| 0.5 | 一半概率继续深入 | 适中复杂 |
| 1 | 每次都继续深入 | 一条路走到底，分支又多又长，迷宫最复杂 |

### 3.4 程序运行的逐步演示

假设有一个 5×5 的小迷宫（实际会大得多）：

```
初始状态（全是墙）：
  # # # # #
  # # # # #
  # # # # #
  # # # # #
  # # # # #

第1步：起点(1,1)设为路，压入栈
  # # # # #
  # . # # #
  # # # # #
  # # # # #
  # # # # #

第2步：随机选到→右(3,1)，打通中间(2,1)
  # # # # #
  # . . . #
  # # # # #
  # # # # #
  # # # # #

第3步：继续从(3,1)探索，随机选→上(3,3)，打通中间(3,2)
  # # # # #
  # . . . #
  # # # . #
  # # # . #
  # # # # #
 
...持续挖，直到所有奇数坐标都被访问过...
```

### 3.5 DFS 算法的优缺点

**优点：**
- 生成的迷宫路径蜿蜒曲折，分支多，可玩性好
- 保证所有路都是连通的（从起点可以走到任何挖开的地方）

**缺点：**
- 理论上起点和终点之间**可能不连通**（虽然概率极低）
- 因此代码中加了 BFS 来兜底检查

---

## 四、BFS 检查：保证起点到终点一定通

### 4.1 为什么还需要 BFS？

DFS 虽然能保证"所有挖开的路都是连通的"，但由于迷宫复杂度控制、边缘墙等因素，**起点和终点之间可能恰好不连通**（概率很低但存在）。

所以 [EnsurePathFromStartToEnd](file:///d:/Unity_Project/IKUN_Game/Assets/Scripts/游戏逻辑/4第四章_黄昏/迷宫关卡_1/迷宫生成/MazeGenerator_DFS.cs#L578-L592) 先用 BFS 检查，不通就强行打通。

### 4.2 BFS 是怎么检查的？

**[CheckIfPositionsConnected](file:///d:/Unity_Project/IKUN_Game/Assets/Scripts/游戏逻辑/4第四章_黄昏/迷宫关卡_1/迷宫生成/MazeGenerator_DFS.cs#L597-L646)** 方法：

```
BFS检查过程（一层一层扩散）：
  起点 S
   │
  ┌┴┐            ← 第1层：检查S的上下左右4个邻居
  1 2
  │ │
 ┌┴┐│            ← 第2层：检查1的邻居、2的邻居...
 3 4 5
 │ │ │
┌┴┐│ │           ← 第3层：继续扩散...
6 7 8 9
     │
     E（终点！）  ← 找到！连通！

如果在扩散过程中找到了终点 → 连通 ✓
如果扩散完所有地方都没找到终点 → 不连通 ✗
```

**关键代码：**

```csharp
bool[,] visited = new bool[actualMapWidth, actualMapHeight];
Queue<Vector2Int> queue = new Queue<Vector2Int>();

queue.Enqueue(from);          // 起点入队
visited[from.x, from.y] = true;

while (queue.Count > 0)
{
    Vector2Int current = queue.Dequeue(); // 队头出队

    if (current == to) return true;       // 找到终点！

    foreach (Vector2Int dir in directions)
    {
        Vector2Int next = current + dir;
        // 在范围内 + 没去过 + 是路(0)
        if (范围内 && !visited && 是路)
        {
            visited[next.x, next.y] = true;
            queue.Enqueue(next);          // 新节点入队，排队等待
        }
    }
}
return false; // 队列空了还没找到 → 不连通
```

### 4.3 如果不通怎么办？

[ForceConnectStartAndEnd](file:///d:/Unity_Project/IKUN_Game/Assets/Scripts/游戏逻辑/4第四章_黄昏/迷宫关卡_1/迷宫生成/MazeGenerator_DFS.cs#L652-L678) 使用最简单粗暴的方式——**走 L 型路径**：

```
起点S ──────────────────┐
                         │  ← 先向右走到底
                         │
                         │  ← 再向下走到终点
                        终点E
```

先横着挖到终点的X坐标，再竖着挖到终点的Y坐标，保证一定通。

---

## 五、程序完整运行时间线

```
Start() 被调用
    │
    ├── 1. 获取 Tilemap 组件
    │
    ├── 2. 根据Sprite创建Tile（墙Tile、路Tile）
    │
    ├── 3. GenerateMazeAtRuntime()  ← 核心！
    │       │
    │       ├── 3.1 验证地图尺寸
    │       ├── 3.2 创建迷宫数据二维数组
    │       ├── 3.3 FillAllMapWithWall()       全部填成墙（1）
    │       ├── 3.4 SetStartAndEndPosition()    左下起点，右上终点
    │       ├── 3.5 GenerateMazeByDFS()          DFS挖路！
    │       ├── 3.6 EnsurePathFromStartToEnd()   BFS检查+兜底
    │       ├── 3.7 CreateBorderWallLayers()     加边缘墙
    │       ├── 3.8 DrawMazeToTilemap()          画到屏幕上
    │       ├── 3.9 RegisterMazeDataToManager()  注册数据
    │       ├── 3.10 SelectWalkablePoints()      放篮球
    │       └── 3.11 GeneratePlayerObj()         生成玩家
    │
    ├── 4. 初始化A星寻路地图
    │
    └── 5. 注册事件（寻路事件、任务事件）
```

---

## 六、关键数据结构一览

| 结构 | 值 | 含义 |
|------|-----|------|
| `mazeDataGrid[x, y]` | `0` | 可行走的地面（路） |
| `mazeDataGrid[x, y]` | `1` | 不可行走的墙体（墙） |
| `Stack<Vector2Int>` | DFS用 | 记录"退路"，后进先出 |
| `Queue<Vector2Int>` | BFS用 | 排队扩散，先进先出 |
| 每次移动距离 | `2` 格 | 保证路之间留1格墙，形成标准迷宫 |

---

## 七、总结：一句话记住两种算法

| 算法 | 作用 | 数据结构 | 记忆口诀 |
|------|------|----------|----------|
| **DFS** | 挖迷宫 | 栈（Stack） | 一条路走到黑，撞墙了再回头 |
| **BFS** | 查连通 | 队列（Queue） | 一圈一圈往外扩，像水波纹 |

在这个迷宫生成器中，**DFS 负责创造，BFS 负责验证**。两者配合，保证了生成的迷宫既有趣（随机蜿蜒）又可用（起点终点一定通）。
