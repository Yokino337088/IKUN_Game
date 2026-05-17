using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TangmenFramework;

/// <summary>
/// boss战关卡的全局管理器
/// </summary>
public class BossLevelMgr:BaseManager<BossLevelMgr>
{
    private BossLevelMgr() { }

    // 当前关卡的boss控制器实例
    private BossController _bossController;

    //提供对外访问boss控制器的属性
    public BossController BossController => _bossController;

    // 当前关卡的玩家控制器实例
    private BossPlayerControl _playerControl;

    // 提供对外访问玩家控制器的属性
    public BossPlayerControl PlayerControl => _playerControl;

    /// <summary>
    /// 注册boss数据
    /// </summary>
    /// <param name="bossController"></param>
    public void RegisterBoss(BossController bossController)
    {
        _bossController = bossController;
        LogSystem.Info("BossLevelMgr initialized");
    }

    /// <summary>
    /// 注册玩家数据
    /// </summary>
    /// <param name="playerControl"></param>
    public void RegisterPlayer(BossPlayerControl playerControl)
    {
        _playerControl = playerControl;
        LogSystem.Info("Player registered in BossLevelMgr");
    }

}
