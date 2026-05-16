using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TangmenFramework;
using UnityEngine;

/// <summary>
/// 迷宫任务流程管理状态机对应的行为接口
/// </summary>
public interface IMazeTask : IFSMObj
{    
    /// <summary>
    /// 玩家当前的迷宫坐标
    /// </summary>
    public Vector2Int playerMazePos { get;}
}