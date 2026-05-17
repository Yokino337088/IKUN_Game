using System;
using System.Collections.Generic;
using System.Text;
using TangmenFramework;
using UnityEngine;

public class TeleportEffect:MonoBehaviour
{
    public void OnAnimationEnd()
    {
        GOPoolMgr.Instance.PushObj(gameObject);
    }
}
