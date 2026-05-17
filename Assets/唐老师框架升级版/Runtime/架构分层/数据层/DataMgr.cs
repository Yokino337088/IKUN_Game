using System;
using System.Collections.Generic;

namespace TangmenFramework
{
    /// <summary>
    /// 数据管理器（纯注册/获取/注销）
    /// 职责：注册自定义数据类 → 按类型或ID获取实例 → 不用时注销
    /// 支持同类型注册多个不同ID的实例
    /// </summary>
    public class DataMgr : BaseManager<DataMgr>
    {
        private class DataRegistry
        {
            public Type Type;
            public IData Instance;
        }

        // ID → 注册信息
        private Dictionary<string, DataRegistry> _dataRegistry = new Dictionary<string, DataRegistry>();

        // ID → 实例
        private Dictionary<string, IData> _dataInstances = new Dictionary<string, IData>();

        // Type → 该类型所有实例的ID列表（支持一对多）
        private Dictionary<Type, List<string>> _typeToIdList = new Dictionary<Type, List<string>>();

        // 全局数据类型集合：标记为全局的类型只能注册一个实例
        private HashSet<Type> _globalDataTypes = new HashSet<Type>();

        private DataMgr()
        {
            LogSystem.Info("DataMgr initialized");
        }

        /// <summary>
        /// 注册数据类（自动创建实例并初始化）
        /// 同类型可注册多个不同ID的实例
        /// </summary>
        public void RegisterData<T>(string id = "", bool autoInitialize = true) where T : IData, new()
        {
            if (string.IsNullOrEmpty(id))
                id = typeof(T).Name;

            if (_dataRegistry.ContainsKey(id))
            {
                LogSystem.Warning($"数据 {id} 已经存在");
                return;
            }

            Type type = typeof(T);
            if (_globalDataTypes.Contains(type))
            {
                LogSystem.Error($"数据类型 {type.Name} 已注册为全局数据，不允许再注册同类型实例");
                return;
            }

            T instance = new T();

            DataRegistry registry = new DataRegistry
            {
                Type = typeof(T),
                Instance = instance
            };

            _dataRegistry[id] = registry;
            _dataInstances[id] = instance;

            // 类型 → ID列表（一对多）
            if (!_typeToIdList.TryGetValue(type, out List<string> idList))
            {
                idList = new List<string>();
                _typeToIdList[type] = idList;
            }
            idList.Add(id);

            if (autoInitialize)
            {
                instance.Initialize();
            }

            LogSystem.Info($"注册数据: {id} ({typeof(T).Name})");
        }

        /// <summary>
        /// 注册数据实例（外部已创建好实例的情况）
        /// 同类型可注册多个不同ID的实例
        /// </summary>
        public void RegisterDataInstance(IData data, bool autoInitialize = true)
        {
            if (_dataRegistry.ContainsKey(data.Id))
            {
                LogSystem.Warning($"数据 {data.Id} 已经注册完成");
                return;
            }

            Type type = data.GetType();
            if (_globalDataTypes.Contains(type))
            {
                LogSystem.Error($"数据类型 {type.Name} 已注册为全局数据，不允许再注册同类型实例");
                return;
            }

            DataRegistry registry = new DataRegistry
            {
                Type = data.GetType(),
                Instance = data
            };

            _dataRegistry[data.Id] = registry;
            _dataInstances[data.Id] = data;

            if (!_typeToIdList.TryGetValue(type, out List<string> idList))
            {
                idList = new List<string>();
                _typeToIdList[type] = idList;
            }
            idList.Add(data.Id);

            if (autoInitialize)
            {
                data.Initialize();
            }

            LogSystem.Info($"注册数据实例: {data.Id} ({data.GetType().Name})");
        }

        /// <summary>
        /// 注册全局数据（全局唯一，同类型只能注册一次）
        /// 与 RegisterData 的区别：调用后该类型被标记为全局，后续任何该类型的注册都会被拒绝
        /// </summary>
        public void RegisterGlobalData<T>(string id = "", bool autoInitialize = true) where T : IData, new()
        {
            Type type = typeof(T);

            if (_globalDataTypes.Contains(type))
            {
                LogSystem.Error($"全局数据 {type.Name} 已经注册，同类型只能有一个全局实例");
                return;
            }

            if (string.IsNullOrEmpty(id))
                id = type.Name;

            if (_dataRegistry.ContainsKey(id))
            {
                LogSystem.Warning($"数据 {id} 已经存在");
                return;
            }

            T instance = new T();

            DataRegistry registry = new DataRegistry
            {
                Type = type,
                Instance = instance
            };

            _dataRegistry[id] = registry;
            _dataInstances[id] = instance;

            if (!_typeToIdList.TryGetValue(type, out List<string> idList))
            {
                idList = new List<string>();
                _typeToIdList[type] = idList;
            }
            idList.Add(id);

            _globalDataTypes.Add(type);

            if (autoInitialize)
            {
                instance.Initialize();
            }

            LogSystem.Info($"注册全局数据: {id} ({type.Name})");
        }

        /// <summary>
        /// 注销数据（按类型——注销该类型的所有实例）
        /// </summary>
        public void UnregisterData<T>() where T : IData
        {
            Type type = typeof(T);
            if (_typeToIdList.TryGetValue(type, out List<string> idList))
            {
                // 复制一份再遍历，避免 Remove 时修改集合
                var idsCopy = new List<string>(idList);
                foreach (string id in idsCopy)
                {
                    UnregisterData(id);
                }
            }
        }

        /// <summary>
        /// 注销数据（按类型+ID——注销该类型下指定ID的单个实例）
        /// </summary>
        public void UnregisterData<T>(string id) where T : IData
        {
            UnregisterData(id);
        }

        /// <summary>
        /// 注销数据（按ID）
        /// 从所有容器中移除该数据的注册信息、实例、类型映射
        /// </summary>
        public void UnregisterData(string id)
        {
            if (!_dataRegistry.ContainsKey(id))
            {
                LogSystem.Warning($"Data {id} 未注册, 无需注销");
                return;
            }

            Type type = _dataRegistry[id].Type;

            _dataRegistry.Remove(id);
            _dataInstances.Remove(id);

            if (_typeToIdList.TryGetValue(type, out List<string> idList))
            {
                idList.Remove(id);
                // 如果该类型下没有更多实例了，清理类型映射条目和全局标记
                if (idList.Count == 0)
                {
                    _typeToIdList.Remove(type);
                    _globalDataTypes.Remove(type);
                }
            }

            LogSystem.Info($"注销数据: {id} ({type.Name})");
        }

        /// <summary>
        /// 获取数据实例（按类型——返回该类型第一个注册的实例）
        /// 仅适用于该类型只有一个实例的情况；多实例请使用 GetData<T>(id)
        /// </summary>
        public T GetData<T>() where T : IData
        {
            Type type = typeof(T);
            if (_typeToIdList.TryGetValue(type, out List<string> idList) && idList.Count > 0)
            {
                return (T)_dataInstances[idList[0]];
            }
            throw new Exception($"数据类型 {typeof(T).Name} 未注册");
        }

        /// <summary>
        /// 获取数据实例（按类型+ID——精确获取同类型下的特定实例）
        /// </summary>
        public T GetData<T>(string id) where T : IData
        {
            if (_dataInstances.TryGetValue(id, out IData data))
            {
                return (T)data;
            }
            throw new Exception($"数据 {id} ({typeof(T).Name}) 未注册");
        }

        /// <summary>
        /// 获取数据实例（按ID）
        /// </summary>
        public IData GetData(string id)
        {
            if (_dataInstances.TryGetValue(id, out IData data))
            {
                return data;
            }
            throw new Exception($"数据 {id} 未注册");
        }

        /// <summary>
        /// 获取指定类型的所有实例
        /// </summary>
        public List<T> GetAllData<T>() where T : IData
        {
            List<T> result = new List<T>();
            Type type = typeof(T);
            if (_typeToIdList.TryGetValue(type, out List<string> idList))
            {
                foreach (string id in idList)
                {
                    result.Add((T)_dataInstances[id]);
                }
            }
            return result;
        }

        /// <summary>
        /// 清空所有已注册的数据实例
        /// </summary>
        public void ClearAllData()
        {
            _dataInstances.Clear();
            _dataRegistry.Clear();
            _typeToIdList.Clear();
            _globalDataTypes.Clear();
            LogSystem.Info("已清空所有数据");
        }

        /// <summary>
        /// 检查数据是否已注册（按ID）
        /// </summary>
        public bool IsDataRegistered(string id)
        {
            return _dataRegistry.ContainsKey(id);
        }

        /// <summary>
        /// 检查数据是否已注册（按类型——至少有一个实例即返回true）
        /// </summary>
        public bool IsDataRegistered<T>() where T : IData
        {
            return _typeToIdList.TryGetValue(typeof(T), out List<string> idList) && idList.Count > 0;
        }

        /// <summary>
        /// 获取所有已注册的数据ID列表
        /// </summary>
        public List<string> GetAllDataIds()
        {
            return new List<string>(_dataRegistry.Keys);
        }

        /// <summary>
        /// 获取所有已注册的数据实例列表
        /// </summary>
        public List<IData> GetAllDataInstances()
        {
            return new List<IData>(_dataInstances.Values);
        }
    }
}
