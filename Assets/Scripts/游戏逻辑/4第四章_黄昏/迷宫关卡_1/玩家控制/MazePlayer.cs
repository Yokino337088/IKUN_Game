using System.Collections;
using System.Collections.Generic;
using TangmenFramework;
using Unity.VisualScripting;
using UnityEngine;

/// <summary>
/// 迷宫玩家控制器
/// 功能：控制玩家在迷宫中移动，使用格子坐标系统（直接坐标模式）
/// </summary>
public class MazePlayer : MonoBehaviour,IMazeTask//要使用本框架中的有限状态机，必须去继承对于的状态机接口
{
    [Header("========== 调试信息 ==========")]
    [Tooltip("玩家当前所在的格子坐标")]
    public Vector2Int currentGridPosition;

    //玩家的输入检查配置
    private InputConfig inputConfig;

    #region 状态机行为接口

    public Vector2Int playerMazePos { get => MazeCoordinateConverter.WorldToGrid(transform.position); }
    #endregion

    //任务流程管理的状态机，依附于玩家的MonoBehavior脚本
    private MazeStateMachine mazeStateMachine;

    private void Awake()
    {
        RegisterStateMachine();

        AddInputBinding();

        RegisterEvent();
    }

    private void Update()
    {
        //执行状态机的帧更新方法
        mazeStateMachine.UpdateState();
    }


    private void OnDestroy()
    {
        LogOutEvent();

        inputConfig = null;

        mazeStateMachine = null;

    }

    /// <summary>
    /// 添加输入绑定
    /// </summary>
    private void AddInputBinding()
    {
        inputConfig = new InputConfig();
        inputConfig.AddKeyboardBinding(E_EventType.E_Input_Up, KeyCode.W);
        inputConfig.AddKeyboardBinding(E_EventType.E_Input_Down, KeyCode.S);
        inputConfig.AddKeyboardBinding(E_EventType.E_Input_Left, KeyCode.A);
        inputConfig.AddKeyboardBinding(E_EventType.E_Input_Right, KeyCode.D);
        inputConfig.ApplyToInputMgr();
    }

    //注册事件
    private void RegisterEvent()
    {
        EventCenter.Instance.AddEventListener(E_EventType.E_Input_Up, MoveUp);
        EventCenter.Instance.AddEventListener(E_EventType.E_Input_Down, MoveDown);
        EventCenter.Instance.AddEventListener(E_EventType.E_Input_Left, MoveLeft);
        EventCenter.Instance.AddEventListener(E_EventType.E_Input_Right, MoveRight);

        EventCenter.Instance.AddEventListener(MyEventTypeString.MazeTask1Event, Change2Task2);
    }

    //注销事件
    private void LogOutEvent()
    {
        EventCenter.Instance.RemoveEventListener(E_EventType.E_Input_Up, MoveUp);
        EventCenter.Instance.RemoveEventListener(E_EventType.E_Input_Down, MoveDown);
        EventCenter.Instance.RemoveEventListener(E_EventType.E_Input_Left, MoveLeft);
        EventCenter.Instance.RemoveEventListener(E_EventType.E_Input_Right, MoveRight);

        EventCenter.Instance.RemoveEventListener(MyEventTypeString.MazeTask1Event, Change2Task2);
    }

    //注册状态机
    private void RegisterStateMachine()
    {
        mazeStateMachine = new MazeStateMachine(this);
        mazeStateMachine.AddState<MazeTask1State>(E_MazeTaskStateType.Task1State);
        mazeStateMachine.AddState<MazeTask2State>(E_MazeTaskStateType.Task2State);
        mazeStateMachine.AddState<MazeTaskCompletedState>(E_MazeTaskStateType.TaskCompletedState);
        mazeStateMachine.ChangeState(E_MazeTaskStateType.Task1State);
    }

    private void MoveUp()
    {        
        TryMove(Vector2.up);
    }

    private void MoveDown()
    {
        TryMove(Vector2.down);
    }

    private void MoveLeft()
    {
        TryMove(Vector2.left);
    }

    private void MoveRight()
    {
        TryMove(Vector2.right);
    }

    private void Change2Task2()
    {
        mazeStateMachine.ChangeState(E_MazeTaskStateType.Task2State);
    }

    /// <summary>
    /// 尝试向指定方向移动
    /// </summary>
    /// <param name="moveDir">移动方向（Vector2.up/down/left/right）</param>
    /// <returns>是否成功移动</returns>
    public bool TryMove(Vector2 moveDir)
    {
        // 获取玩家当前的格子坐标
        Vector2Int currentPos = MazeCoordinateConverter.GetCharacterGridPosition(transform);

        // 计算目标格子坐标
        Vector2Int targetPos = new Vector2Int(
            currentPos.x + (int)moveDir.x,
            currentPos.y + (int)moveDir.y
        );

        // 检查目标格子是否可行走
        if (MazeCoordinateConverter.IsWalkable(targetPos))
        {
            // 目标可行走，直接设置玩家坐标到目标格子
            MoveToGridInstant(targetPos);
            return true;
        }
        else
        {
            // 目标不可行走，调试信息
            Debug.Log($"无法移动到 {targetPos}，该位置是墙体或超出边界");
            return false;
        }
    }

    /// <summary>
    /// 直接移动到指定格子（瞬间移动）
    /// </summary>
    /// <param name="targetGridPos">目标格子坐标</param>
    public void MoveToGridInstant(Vector2Int targetGridPos)
    {
        if (MazeCoordinateConverter.IsWalkable(targetGridPos))
        {
            Vector3 targetWorldPos = MazeCoordinateConverter.GridToWorld(targetGridPos);
            targetWorldPos.z = transform.position.z;
            transform.position = targetWorldPos;
            currentGridPosition = targetGridPos;
        }
    }

    /// <summary>
    /// 移动到起点
    /// </summary>
    public void MoveToStart()
    {
        MazeCoordinateConverter.MoveCharacterToStart(transform);
        currentGridPosition = MazeCoordinateConverter.GetCharacterGridPosition(transform);
    }

    /// <summary>
    /// 移动到终点
    /// </summary>
    public void MoveToEnd()
    {
        MazeCoordinateConverter.MoveCharacterToEnd(transform);
        currentGridPosition = MazeCoordinateConverter.GetCharacterGridPosition(transform);
    }

    /// <summary>
    /// 移动到随机可行走位置
    /// </summary>
    public void MoveToRandomPosition()
    {
        Vector2Int randomPos = MazeCoordinateConverter.GetRandomWalkablePosition();
        if (randomPos.x >= 0)
        {
            MoveToGridInstant(randomPos);
            LogSystem.Info($"玩家已移动到随机位置：{randomPos}");
        }
    }

    
}
