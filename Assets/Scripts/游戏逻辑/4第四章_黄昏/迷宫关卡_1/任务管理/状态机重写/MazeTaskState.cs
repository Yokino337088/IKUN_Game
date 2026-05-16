using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TangmenFramework;

/// <summary>
/// 迷宫任务流程管理状态机的基类，可以防止泛型参数写错
/// </summary>
public class MazeTaskState : BaseState<E_MazeTaskStateType, IMazeTask>
{
    public MazeTaskState(StateMachine<E_MazeTaskStateType, IMazeTask> machine) : base(machine)
    {
    }

    public override E_MazeTaskStateType StateType => throw new System.NotImplementedException();

    public override void EnterState()
    {
        
    }

    public override void QuitState()
    {
        
    }

    public override void UpdateState()
    {
        
    }
}
