using System;

namespace TangmenFramework
{
    /// <summary>
    /// 数据接口
    /// 所有数据类需要实现此接口，即可注册到 DataMgr 中进行管理
    /// </summary>
    public interface IData
    {
        /// <summary>
        /// 数据ID
        /// </summary>
        string Id { get; }

        /// <summary>
        /// 数据名称
        /// </summary>
        string Name { get; set; }

        /// <summary>
        /// 初始化数据
        /// </summary>
        void Initialize();

        /// <summary>
        /// 重置数据
        /// </summary>
        void Reset();
    }
}
