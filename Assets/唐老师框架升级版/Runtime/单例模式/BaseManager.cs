using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace TangmenFramework
{
/// <summary>
/// 单例模式基类 主要目的是保证子类的唯一性 实现了懒汉式单例模式
/// </summary>
/// <typeparam name="T"></typeparam>
public abstract class BaseManager<T> where T:class//,new()
{
    private static T instance;

    //判断单例模式实例 是否为null
    protected bool InstanceisNull => instance == null;

    //用于加锁的对象
    protected static readonly object lockObj = new object();

    //懒加载的方式
    public static T Instance
    {
        get
        {
            if(instance == null)
            {
                lock (lockObj)
                {
                    if (instance == null)
                    {
                        //instance = new T();
                        //通过反射获取无参非公共的构造函数 用于创建实例
                        Type type = typeof(T);
                        ConstructorInfo info = type.GetConstructor(BindingFlags.Instance | BindingFlags.NonPublic,
                                                                    null,
                                                                    Type.EmptyTypes,
                                                                    null);
                        if (info != null)
                            instance = info.Invoke(null) as T;
                        else
                            LogSystem.Error("没有找到对应的无参私有构造函数");

                        //instance = Activator.CreateInstance(typeof(T), true) as T;
                    }
                }
            }
            return instance;
        }
    }


    public virtual void Dispose()
    {
        instance = null;
    }
}
}
