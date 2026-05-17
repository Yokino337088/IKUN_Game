using System;
using System.Collections.Generic;
using System.Text;
using TangmenFramework;
using TMPro;
using UnityEngine;

/// <summary>
/// 伤害显示信息（通过事件传递）
/// </summary>
public struct DamageDisplayInfo
{
    public int damage;
    public bool isCrit;

    public DamageDisplayInfo(int damage, bool isCrit)
    {
        this.damage = damage;
        this.isCrit = isCrit;
    }
}

public class DamageText : MonoBehaviour
{
    [SerializeField]
    private TextMeshPro _textMeshPro;

    [SerializeField]
    private float moveSpeed = 1f;

    /// <summary>
    /// 初始化伤害文本
    /// </summary>
    public void Init(DamageDisplayInfo info)
    {
        _textMeshPro.text = info.damage.ToString();
        _textMeshPro.color = info.isCrit ? Color.yellow : Color.red;

        TimerMgr.Instance.CreateTimer(true, 2000, () =>
        {
            GOPoolMgr.Instance.PushObj(gameObject);
        });
    }

    public void SetPos(Vector3 pos)
    {
        transform.position = pos;
    }

    private void Update()
    {
        transform.position += Vector3.up * moveSpeed * Time.deltaTime;
    }
}
