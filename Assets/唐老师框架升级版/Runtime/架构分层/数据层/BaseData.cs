using System;

namespace TangmenFramework
{
    /// <summary>
    /// 基础数据类
    /// 所有数据类的基类，提供 Id/Name/Initialize/Reset 的默认实现
    /// </summary>
    public abstract class BaseData : IData
    {
        public string Id { get; protected set; }

        public string Name { get; set; }

        public bool IsInitialized { get; protected set; }

        protected BaseData(string id, string name = null)
        {
            Id = id;
            Name = name ?? id;
            IsInitialized = false;
        }

        public virtual void Initialize()
        {
            if (!IsInitialized)
            {
                OnInitialize();
                IsInitialized = true;
            }
        }

        public virtual void Reset()
        {
            OnReset();
        }

        /// <summary>
        /// 初始化时调用（子类重写此方法设置初始值）
        /// </summary>
        protected virtual void OnInitialize() { }

        /// <summary>
        /// 重置时调用（子类重写此方法清空/还原数据）
        /// </summary>
        protected virtual void OnReset() { }

        /// <summary>
        /// 检查数据是否已初始化，未初始化则抛异常
        /// </summary>
        protected void CheckInitialized()
        {
            if (!IsInitialized)
            {
                throw new Exception($"Data {Id} is not initialized");
            }
        }

        public override string ToString()
        {
            return $"{GetType().Name} [Id: {Id}, Name: {Name}]";
        }
    }
}
