using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TangmenFramework;
using UnityEngine;

/// <summary>
/// 迷宫任务1
/// </summary>
public class MazeTask1State : MazeTaskState
{
    private bool _hasProcessedCurrentBall = false;
    
    public MazeTask1State(StateMachine<E_MazeTaskStateType, IMazeTask> machine) : base(machine)
    {
    }

    public override E_MazeTaskStateType StateType => E_MazeTaskStateType.Task1State;

    public override void EnterState()
    {
        base.EnterState();
        _hasProcessedCurrentBall = false;

        //这里必须延迟执行，如果不延迟执行的话就会有一个“[ERROR] 起点或终点不在地图范围内”的运行时报错
        //为什么延迟 500ms 就好了？
        //500ms 远远大于一帧的时间（60fps 下一帧 ≈ 16ms），这期间：
        //1.Unity 已经跑了几十帧
        //2.Tilemap 的内部数据已经完全刷新完毕
        //3.tilemap.WorldToCell() 能正确工作了
        //4.玩家坐标转换得到正确的迷宫格子坐标
        //5.AStar 寻路顺利找到路径

        //不是玩家坐标没初始化，而是 DrawMazeToTilemap() 刚画完瓦片，Tilemap 内部还没来得及刷新。
        //Instantiate 的 Awake 回调在同一个执行栈里就触发了 WorldToCell 
        //读到一个还没刷新完的 Tilemap，拿到错误的越界坐标，然后传给 AStar 就炸了。
        //延迟 500ms 给了 Tilemap 足够的刷新时间。

        //bug讲解详见文档当中的bug解析
        TimerMgr.Instance.CreateTimer(true, 500, () =>
        {
            RefreshNearestBall();
        });

        //注册事件
        EventCenter.Instance.AddEventListener(MyEventTypeString.AndroidPlayerFindPath, PlayerFindPath);

    }

    public override void UpdateState()
    {
        base.UpdateState();

        CheckBallCollection();

        //寻路检测
        if (Input.GetKeyDown(KeyCode.Space))
        {
            PlayerFindPath();
        }
    }

    public override void QuitState()
    {
        base.QuitState();
        _hasProcessedCurrentBall = false;

        //注销事件
        EventCenter.Instance.RemoveEventListener(MyEventTypeString.AndroidPlayerFindPath, PlayerFindPath);
    }

    /// <summary>
    /// 异步刷新距离玩家最近的篮球
    /// </summary>
    private void RefreshNearestBall()
    {
        //这里是一个fire-and-forget的异步操作，Forget()意味着方法立刻返回UniTaskVoid
        //但真正的计算在后台异步进行
        //关键是 GetNowBallPos() 内部要等 AStar 寻路全部算完（对每个篮球都走一遍 AStar），
        //才会把nowBallPos赋值成有效的篮球坐标。在此之前，nowBallPos一直是初始值 Vector2Int.zero 即(0, 0)
        MazeDataManager.Instance.RefreshNowBallPos(AIObj.playerMazePos).Forget();
    }

    /// <summary>
    /// 检测玩家坐标是否在篮球上面
    /// </summary>
    private void CheckBallCollection()
    {
        if (MazeDataManager.Instance.CheckPlayerReachedBall(AIObj.playerMazePos))
        {
            //防止重复处理
            if (_hasProcessedCurrentBall) 
                return;
            _hasProcessedCurrentBall = true;
            ProcessBallCollection();
        }
    }

    /// <summary>
    /// 处理篮球收集
    /// </summary>
    private void ProcessBallCollection()
    {
        //重置状态
        _hasProcessedCurrentBall = false;
        //隐藏篮球
        MazeDataManager.Instance.HideMazeBall(AIObj.playerMazePos);

        //检测任务1是否完成
        if (MazeDataManager.Instance.CheckTask1IsCompleted())
        {
            LogSystem.Info("任务1完成：收集完所有篮球！");
            //切换到任务2
            ChangeState(E_MazeTaskStateType.Task2State);
        }
        else
        {
            //刷新篮球
            RefreshNearestBall();
            LogSystem.Info($"目标切换到距离最近的篮球，剩余: {MazeDataManager.Instance.GetBallCount()}");
        }
    }

    /// <summary>
    /// 玩家用于寻路的事件函数
    /// </summary>
    private void PlayerFindPath()
    {
        //获取坐标
        Vector2Int cachedBallPos = MazeDataManager.Instance.GetCachedBallPos();
        //触发事件进行寻路，注意，由于事件注册的是Vector2类型，所以这里必须将Vector2Int类型显示转换为Vector2才行
        //要不然的话就会运行时报错
        EventCenter.Instance.EventTrigger(MyEventTypeString.PlayerFindPathEvent, (Vector2)AIObj.playerMazePos, (Vector2)cachedBallPos);
    }
}
