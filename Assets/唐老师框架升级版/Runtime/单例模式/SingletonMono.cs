using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TangmenFramework
{
/// <summary>
/// 手动式 继承Mono的单例模式基类
/// </summary>
/// <typeparam name="T"></typeparam>
public class SingletonMono<T>: MonoBehaviour where T:MonoBehaviour
{
    private static T instance;

    public static T Instance
    {
        get
        {
            return instance;
        }
    }

    /// <summary>
    /// 销毁单例实例及其挂载的 GameObject。
    /// 调用后 Instance 返回 null，需要重新在场景中放置或创建。
    /// </summary>
    public static void DestroyInstance()
    {
        if (instance != null)
        {
            Destroy(instance.gameObject);
            instance = null;
        }
    }

    protected virtual void Awake()
    {
        //已经存在一个对应的单例模式实例了 需要删除这一个
        if(instance != null)
        {
            Destroy(this);
            return;
        }
        instance = this as T;        
    }
}
}
