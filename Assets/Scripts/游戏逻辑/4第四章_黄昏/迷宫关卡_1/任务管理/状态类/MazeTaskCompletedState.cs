using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TangmenFramework;

/// <summary>
/// 走迷宫任务完成的状态
/// </summary>
public class MazeTaskCompletedState : MazeTaskState
{
    public MazeTaskCompletedState(StateMachine<E_MazeTaskStateType, IMazeTask> machine) : base(machine)
    {
    }

    public override E_MazeTaskStateType StateType => E_MazeTaskStateType.TaskCompletedState;

    public override void EnterState()
    {
        base.EnterState();
        ClearScene();
        //切换场景
        SceneMgr.Instance.LoadSceneAsyn("第四章_boss战", () =>
        {

        });
    }

    //清理场景数据
    private void ClearScene()
    {
        MazeDataManager.Instance.ClearData();
        MazeDataManager.Instance.Dispose();
    }
}