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
