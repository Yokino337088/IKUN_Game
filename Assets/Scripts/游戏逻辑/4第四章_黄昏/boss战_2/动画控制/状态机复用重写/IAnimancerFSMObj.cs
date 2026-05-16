﻿using Animancer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using TangmenFramework;

/// <summary>
/// 玩家角色行为接口
/// </summary>
public interface IAnimancerFSMObj : IFSMObj 
{

    /// <summary>
    /// Animancer组件
    /// </summary>
    public AnimancerComponent Animancer { get; }
    
    
    
}