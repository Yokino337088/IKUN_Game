using System;
using System.Collections.Generic;
using System.Linq;

namespace TangmenFramework
{
    /// <summary>
    /// 存档管理器
    /// 统一管理所有存档数据的加载和保存
    /// </summary>
    public class ArchiveMgr : BaseManager<ArchiveMgr>
    {
        /// <summary>
        /// 管理数据的字典，记住了，凡是需要管理长期储存的数据都用字典
        /// </summary>
        private Dictionary<string, object> _archiveDataDic = new Dictionary<string, object>();

        /// <summary>
        /// 构造函数
        /// </summary>
        private ArchiveMgr()
        {

        }

        /// <summary>
        /// 注册存档数据类型，将数据塞到字典中就行了
        /// </summary>
        /// <typeparam name="T">存档数据类型</typeparam>
        public void RegisterArchive<T>() where T : class, new()
        {
            string typeName = typeof(T).Name;
            if (!_archiveDataDic.ContainsKey(typeName))
            {
                //空值标记法，先用一个空值来占位
                _archiveDataDic[typeName] = null;               
                LogSystem.Info($"注册存档类型: {typeName}");
            }
        }

        /// <summary>
        /// 加载所有已注册的存档数据
        /// </summary>
        public void LoadAllArchives()
        {
            LogSystem.Info("开始加载所有存档数据...");
            foreach (var kvp in _archiveDataDic.ToList())
            {
                // 使用反射调用对应的 LoadDataOrDefault 方法，这里只能通过string获取类型
                Type type = Type.GetType(kvp.Key);
                if (type != null)
                {
                    //反射获取方法
                    var method = type.GetMethod("LoadDataOrDefault", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                    if (method != null)
                    {
                        var data = method.Invoke(null, null);
                        _archiveDataDic[kvp.Key] = data;
                        LogSystem.Info($"加载存档: {kvp.Key}");
                    }
                }
            }
            LogSystem.Info("所有存档数据加载完成");
        }

        /// <summary>
        /// 保存所有已注册的存档数据
        /// </summary>
        public void SaveAllArchives()
        {
            LogSystem.Info("开始保存所有存档数据...");
            foreach (var kvp in _archiveDataDic)
            {
                if (kvp.Value != null)
                {
                    // 使用反射调用对应的 SaveData 方法
                    var method = kvp.Value.GetType().GetMethod("SaveData");
                    if (method != null)
                    {
                        method.Invoke(kvp.Value, null);
                        LogSystem.Info($"保存存档: {kvp.Key}");
                    }
                }
            }
            LogSystem.Info("所有存档数据保存完成");
        }

        /// <summary>
        /// 保存单个数据
        /// </summary>
        /// <typeparam name="T">存档数据类型</typeparam>
        public void SaveOneArchive<T>() where T : class, new()
        {
            //先获取名字
            string name = typeof(T).Name;
            //再查询字典
            if (_archiveDataDic.TryGetValue(name, out var data))
            {
                //反射获取方法
                var method = data.GetType().GetMethod("SaveData");
                if (method != null)
                {
                    //调用data对象的储存方法
                    method.Invoke(data, null);
                    LogSystem.Info($"保存存档: {name}");
                }
            }
            else
            {
                LogSystem.Warning($"存档数据未注册: {name}");
            }
        }

        /// <summary>
        /// 加载单个数据
        /// </summary>
        /// <typeparam name="T">存档数据类型</typeparam>
        public void LoadOneArchive<T>() where T : class, new()
        {
            //先获取名字
            string name = typeof(T).Name;
            //再查询字典
            if (_archiveDataDic.ContainsKey(name))
            {
                // 调用静态方法 LoadDataOrDefault
                var method = typeof(T).GetMethod("LoadDataOrDefault", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                if (method != null)
                {
                    //直接调用就行了，不用传对象
                    var data = method.Invoke(null, null);
                    //加载出来之后塞到字典里面
                    _archiveDataDic[name] = data;
                    LogSystem.Info($"加载存档: {name}");
                }                
            }
            else
            {
                LogSystem.Warning($"存档数据未注册: {name}");
            }
        }

        /// <summary>
        /// 获取存档数据
        /// </summary>
        /// <typeparam name="T">存档数据类型</typeparam>
        /// <returns>存档数据对象</returns>
        public T GetArchive<T>() where T : class, new()
        {
            //先获取名字
            string typeName = typeof(T).Name;
            //再查询字典
            if (_archiveDataDic.TryGetValue(typeName, out var data))
            {
                return data as T;
            }
            LogSystem.Warning($"存档数据未注册或未加载: {typeName}");
            return null;
        }

        /// <summary>
        /// 设置存档数据
        /// </summary>
        /// <typeparam name="T">存档数据类型</typeparam>
        /// <param name="data">存档数据对象</param>
        public void SetArchive<T>(T data) where T : class, new()
        {
            string typeName = typeof(T).Name;
            _archiveDataDic[typeName] = data;
        }

        /// <summary>
        /// 清理所有存档数据（内存中）
        /// </summary>
        public void ClearAllArchives()
        {
            _archiveDataDic.Clear();
            LogSystem.Info("已清理所有存档数据（内存中）");
        }

        /// <summary>
        /// 检查指定类型是否有存档数据
        /// </summary>
        /// <typeparam name="T">存档数据类型</typeparam>
        /// <returns>是否有存档</returns>
        public bool HasArchive<T>() where T : class, new()
        {
            return ArchiveData<T>.HasSaveData();
        }
        
    }
}
