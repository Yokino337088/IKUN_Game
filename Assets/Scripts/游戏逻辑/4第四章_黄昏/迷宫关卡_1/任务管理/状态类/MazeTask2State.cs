using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TangmenFramework;
using UnityEngine;

public class MazeTask2State : MazeTaskState
{
    public MazeTask2State(StateMachine<E_MazeTaskStateType, IMazeTask> machine) : base(machine)
    {
    }

    public override E_MazeTaskStateType StateType => E_MazeTaskStateType.Task2State;

    public override void EnterState()
    {
        base.EnterState();
        LogSystem.Info("任务1完成，进入任务2");
        //任务2初始化(设置终点的瓦片)
        EventCenter.Instance.EventTrigger(MyEventTypeString.MazeTask2InitEvent);

        //注册事件
        EventCenter.Instance.AddEventListener(MyEventTypeString.AndroidPlayerFindPath, PlayerFindPath);
    }

    public override void UpdateState()
    {
        base.UpdateState();

        //如果检测到玩家到了终点
        if (MazeDataManager.Instance.CheckWhetherAtDestination(AIObj.playerMazePos))
        {
            //那就切换到任务完成状态
            ChangeState(E_MazeTaskStateType.TaskCompletedState);
        }

        //检测到输入，进行A星寻路
        if (Input.GetKeyDown(KeyCode.Space))
        {
            PlayerFindPath();
        }
    }

    public override void QuitState()
    {
        base.QuitState();
        //注销事件
        EventCenter.Instance.RemoveEventListener(MyEventTypeString.AndroidPlayerFindPath, PlayerFindPath);
    }

    /// <summary>
    /// 玩家用于寻路的事件函数
    /// </summary>
    private void PlayerFindPath()
    {
        //注意：这里由于事件注册的参数类型是Vector2，所以这里必须将Vector2Int类型显示转换为Vector2才行
        //要不然的话就会运行时报错
        EventCenter.Instance.EventTrigger(MyEventTypeString.PlayerFindPathEvent, (Vector2)AIObj.playerMazePos, (Vector2)MazeDataManager.Instance.GetEndPosition());
    }
}