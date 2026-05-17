using System;
using System.Collections.Generic;

namespace TangmenFramework
{
    /// <summary>
    /// 玩家数据示例
    /// </summary>
    public class PlayerData : BaseData
    {
        public int Level { get; set; }
        public int Exp { get; set; }
        public int Gold { get; set; }
        public int Diamond { get; set; }

        public List<string> Equipments { get; set; }
        public List<string> Skills { get; set; }
        public List<string> Achievements { get; set; }

        public PlayerData() : base("PlayerData", "玩家数据")
        {
            Equipments = new List<string>();
            Skills = new List<string>();
            Achievements = new List<string>();
        }

        protected override void OnInitialize()
        {
            Level = 1;
            Exp = 0;
            Gold = 1000;
            Diamond = 100;

            Equipments.Add("新手剑");
            Equipments.Add("新手护甲");

            Skills.Add("基础攻击");

            LogSystem.Info("PlayerData initialized");
        }

        protected override void OnReset()
        {
            Level = 1;
            Exp = 0;
            Gold = 1000;
            Diamond = 100;
            Equipments.Clear();
            Skills.Clear();
            Achievements.Clear();

            LogSystem.Info("PlayerData reset");
        }

        public void AddExp(int amount)
        {
            Exp += amount;
            CheckLevelUp();
        }

        public void AddGold(int amount)
        {
            Gold += amount;
        }

        public void AddDiamond(int amount)
        {
            Diamond += amount;
        }

        public void AddEquipment(string equipment)
        {
            if (!Equipments.Contains(equipment))
            {
                Equipments.Add(equipment);
            }
        }

        public void AddSkill(string skill)
        {
            if (!Skills.Contains(skill))
            {
                Skills.Add(skill);
            }
        }

        public void AddAchievement(string achievement)
        {
            if (!Achievements.Contains(achievement))
            {
                Achievements.Add(achievement);
            }
        }

        private void CheckLevelUp()
        {
            int requiredExp = Level * 100;
            if (Exp >= requiredExp)
            {
                Exp -= requiredExp;
                Level++;
                LogSystem.Info($"Player level up to {Level}");
            }
        }
    }
}
