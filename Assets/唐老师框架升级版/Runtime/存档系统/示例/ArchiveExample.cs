using System;
using System.Collections.Generic;
using UnityEngine;

namespace TangmenFramework
{
    /// <summary>
    /// 存档系统使用示例
    /// 展示如何使用存档系统
    /// </summary>
    public class ArchiveExample
    {
        /// <summary>
        /// 初始化存档系统（游戏启动时调用
        /// </summary>
        public void InitArchiveSystem()
        {
            // 方法1：逐个注册存档类型
            ArchiveMgr.Instance.RegisterArchive<PlayerArchive>();
            ArchiveMgr.Instance.RegisterArchive<GameSettingArchive>();
            ArchiveMgr.Instance.RegisterArchive<SaveSlotArchive>();

            // 方法2：一次性初始化（使用类型列表
            // List<Type> archiveTypes = new List<Type>
            // {
            //     typeof(PlayerArchive),
            //     typeof(GameSettingArchive),
            //     typeof(SaveSlotArchive)
            // };
            // ArchiveMgr.Instance.Init(archiveTypes);

            // 加载所有存档数据
            ArchiveMgr.Instance.LoadAllArchives();

            LogSystem.Info("存档系统初始化完成");
        }

        /// <summary>
        /// 使用存档示例
        /// </summary>
        public void ArchiveUsageExample()
        {
            // ==========================================
            // 方式1：直接通过数据类自身的方法进行操作
            // ==========================================

            // 加载玩家存档
            PlayerArchive playerData = PlayerArchive.LoadDataOrDefault();
            
            // 修改数据
            playerData.playerName = "张三";
            playerData.level = 10;
            playerData.gold = 9999;
            playerData.unlockedLevels.Add(5);
            
            // 保存玩家存档
            playerData.SaveData();


            // ==========================================
            // 方式2：通过存档管理器统一管理
            // ==========================================

            // 获取玩家存档
            var playerArchive = ArchiveMgr.Instance.GetArchive<PlayerArchive>();
            if (playerArchive != null)
            {
                playerArchive.gold = 10000;
            }
            
            // 保存所有存档
            ArchiveMgr.Instance.SaveAllArchives();



            // ==========================================
            // 其他常用操作
            // ==========================================

            // 检查是否有存档
            bool hasSave = ArchiveMgr.Instance.HasArchive<PlayerArchive>();

            // 设置存档数据
            var newSetting = new GameSettingArchive();
            newSetting.musicVolume = 0.8f;
            ArchiveMgr.Instance.SetArchive(newSetting);
            newSetting.SaveData();
        }
    }

    // ==========================================
    // 示例存档数据类
    // ==========================================

    /// <summary>
    /// 玩家存档数据
    /// </summary>
    [Serializable]
    public class PlayerArchive : ArchiveData<PlayerArchive>
    {
        public string playerName = "玩家";
        public int level = 1;
        public int gold = 0;
        public List<int> unlockedLevels = new List<int>();
        public DateTime lastPlayTime = DateTime.Now;
    }

    /// <summary>
    /// 游戏设置存档
    /// </summary>
    [Serializable]
    public class GameSettingArchive : ArchiveData<GameSettingArchive>
    {
        public float musicVolume = 1f;
        public float soundVolume = 1f;
        public bool fullScreen = true;
        public int qualityLevel = 3;
    }

    /// <summary>
    /// 存档槽数据
    /// </summary>
    [Serializable]
    public class SaveSlotArchive : ArchiveData<SaveSlotArchive>
    {
        public List<SaveSlotInfo> saveSlots = new List<SaveSlotInfo>();
    }

    [Serializable]
    public class SaveSlotInfo
    {
        public int slotId;
        public string slotName;
        public DateTime saveTime;
        public int playTimeMinutes;
    }
}
