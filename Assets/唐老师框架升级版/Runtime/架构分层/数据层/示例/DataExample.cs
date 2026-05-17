using System;
using UnityEngine;

namespace TangmenFramework
{
    /// <summary>
    /// 数据层使用示例
    /// 演示 Register → Get → 使用 → Unregister 的完整生命周期
    /// </summary>
    public class DataExample : MonoBehaviour
    {
        private void Start()
        {
            // 演示完整的数据使用流程

            // 1. 注册数据（自动创建实例并初始化）
            RegisterData();

            // 2. 获取并使用数据
            UsePlayerData();

            // 3. 不再需要时注销数据
            UnregisterData();
        }

        private void RegisterData()
        {
            // 注册玩家数据（autoInitialize 默认为 true，注册即初始化）
            DataMgr.Instance.RegisterData<PlayerData>("PlayerData");

            // 注册其他数据类的示例：
            // DataMgr.Instance.RegisterData<BossData>("BossData");
            // DataMgr.Instance.RegisterData<SettingsData>("SettingsData");
        }

        private void UsePlayerData()
        {
            // 按类型获取实例
            PlayerData playerData = DataMgr.Instance.GetData<PlayerData>();

            Debug.Log($"初始数据: Level={playerData.Level}, Gold={playerData.Gold}, Diamond={playerData.Diamond}");
            Debug.Log($"初始装备: {string.Join(", ", playerData.Equipments)}");

            // 也可以按ID获取
            // IData data = DataMgr.Instance.GetData("PlayerData");
            // PlayerData sameData = data as PlayerData;

            // 修改数据
            playerData.AddExp(150);
            playerData.AddGold(500);
            playerData.AddDiamond(50);
            playerData.AddEquipment("中级剑");
            playerData.AddSkill("火球术");
            playerData.AddAchievement("第一次升级");

            Debug.Log($"修改后: Level={playerData.Level}, Gold={playerData.Gold}, Diamond={playerData.Diamond}");

            // 重置数据
            // playerData.Reset();
        }

        private void UnregisterData()
        {
            // 按类型注销
            DataMgr.Instance.UnregisterData<PlayerData>();

            // 或按ID注销
            // DataMgr.Instance.UnregisterData("PlayerData");

            // 注销后再次 GetData 会抛异常
            // DataMgr.Instance.GetData<PlayerData>(); // ❌ Exception
        }

        private void OnDestroy()
        {
            // 场景退出时统一清空
            DataMgr.Instance.ClearAllData();
        }
    }
}
