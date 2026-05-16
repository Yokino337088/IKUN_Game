using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TangmenFramework
{
    /// <summary>
    /// 存档数据基类
    /// 所有需要存档的数据类都继承此类
    /// </summary>
    /// <typeparam name="T">数据类型</typeparam>
    public abstract class ArchiveData<T> where T : class, new()
    {
        /// <summary>
        /// 类名
        /// </summary>
        private string _name = typeof(T).Name;

        /// <summary>
        /// 保存数据到本地
        /// </summary>
        public void SaveData()
        {
            //调用数据管理器进行存档
            JsonDataMgr.Instance.Save(this, _name);
        }

        /// <summary>
        /// 从本地加载数据
        /// </summary>
        /// <returns>加载的数据对象</returns>
        public static T LoadData()
        {
            //调用数据管理器进行加载
            return JsonDataMgr.Instance.Load<T>(typeof(T).Name);
        }

        /// <summary>
        /// 从本地加载数据，如果不存在则创建新实例
        /// </summary>
        /// <returns>加载的数据对象</returns>
        public static T LoadDataOrDefault()
        {
            var data = JsonDataMgr.Instance.Load<T>(typeof(T).Name);
            return data ?? new T();
        }

        /// <summary>
        /// 检查是否有存档数据
        /// </summary>
        /// <returns>是否存在存档</returns>
        public static bool HasSaveData()
        {
            return JsonDataMgr.Instance.HasSavedData(typeof(T).Name);
        }
    }
}
