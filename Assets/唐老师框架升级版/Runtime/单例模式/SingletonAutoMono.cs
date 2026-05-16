using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TangmenFramework
{


/// <summary>
/// 自动单例模式 继承Mono的单例模式基类
/// 推荐使用 
/// 因为是自动创建 动态添加 避免出现空引用问题
/// </summary>
/// <typeparam name="T"></typeparam>
public class SingletonAutoMono<T> : MonoBehaviour where T:MonoBehaviour
{
    private static T instance;

    public static T Instance
    {
        get
        {
            if(instance == null)
            {
                //动态创建 动态添加
                //在场景中创建游戏对象
                GameObject obj = new GameObject();
                //获取T脚本的名称 为了方便在编辑器中可以看到正确的对象名称
                //单例模式脚本挂载的GameObject
                obj.name = typeof(T).ToString();
                //动态添加对应的 单例模式脚本
                instance = obj.AddComponent<T>();
                //切换场景时不删除该对象 保证该对象在整个游戏生命周期中都存在
                DontDestroyOnLoad(obj);
            }
            return instance;
        }
    }

}
}
