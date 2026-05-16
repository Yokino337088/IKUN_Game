using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace TangmenFramework
{
    /// <summary>
    /// 计时器对象 用于存储计时器信息
    /// </summary>
    public class TimerItem : IPoolObject
    {
        /// <summary>
        /// 唯一ID
        /// </summary>
        public int keyID;
        /// <summary>
        /// 计时器结束时的回调
        /// </summary>
        public Action overCallBack;
        /// <summary>
        /// 每隔一段时间去执行的回调
        /// </summary>
        public Action callBack;

        /// <summary>
        /// 表示计时器总共的计时时间 单位：1s = 1000ms
        /// </summary>
        public int allTime;
        /// <summary>
        /// 记录一开始的总计时时间 用于计时器重置
        /// </summary>
        public int maxAllTime;

        /// <summary>
        /// 间隔执行回调的时间 单位：1s = 1000ms
        /// </summary>
        public int intervalTime;
        /// <summary>
        /// 记录一开始的间隔时间
        /// </summary>
        public int maxIntervalTime;

        /// <summary>
        /// 是否正在进行计时
        /// </summary>
        public bool isRuning;

        /// <summary>
        /// 初始化计时器对象
        /// </summary>
        /// <param name="keyID">唯一ID</param>
        /// <param name="allTime">总时间</param>
        /// <param name="overCallBack">计时器结束时调用的回调</param>
        /// <param name="intervalTime">间隔执行的时间</param>
        /// <param name="callBack">间隔执行时调用的回调</param>
        public void InitInfo(int keyID, int allTime, Action overCallBack, int intervalTime = 0, Action callBack = null)
        {
            this.keyID = keyID;
            this.maxAllTime = this.allTime = allTime;
            this.overCallBack = overCallBack;
            this.maxIntervalTime = this.intervalTime = intervalTime;
            this.callBack = callBack;
            this.isRuning = true;
        }

        /// <summary>
        /// 重置计时器
        /// </summary>
        public void ResetTimer()
        {
            this.allTime = this.maxAllTime;
            this.intervalTime = this.maxIntervalTime;
            this.isRuning = true;
        }

        /// <summary>
        /// 放入对象池时 清空所有回调
        /// </summary>
        public void ResetInfo()
        {
            overCallBack = null;
            callBack = null;
        }
    }
}
