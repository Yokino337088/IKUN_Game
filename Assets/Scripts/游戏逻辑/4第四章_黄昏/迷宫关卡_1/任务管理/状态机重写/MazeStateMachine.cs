using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TangmenFramework;

/// <summary>
/// 迷宫任务流程管理状态机对应的状态机管理器，这里只需要走个过场就行了，也不用实际些什么代码
/// </summary>
public class MazeStateMachine : StateMachine<E_MazeTaskStateType, IMazeTask>
{
    public MazeStateMachine(IMazeTask aiObj) : base(aiObj)
    {
    }
}