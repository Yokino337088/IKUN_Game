using System;
using System.Collections.Generic;
using System.Text;
using TangmenFramework;
using TMPro;
using UnityEngine;

public class DadivTaoText:MonoBehaviour
{
    [SerializeField]
    private float moveSpeed = 1f;

    [SerializeField]
    private Material tmpMaterial;

    [SerializeField]
    private TextMeshPro textMeshPro;

    private void Start()
    {
        //设置对应的Shader和材质，防止安卓平台上TextMeshPro材质丢失导致文本不显示的问题
        tmpMaterial.shader = Shader.Find("TextMeshPro/Distance Field");
        textMeshPro.material = tmpMaterial;

        TimerMgr.Instance.CreateTimer(true, 2000, () =>
        {
            Destroy(gameObject);
        });
    }

    private void Update()
    {
        transform.position += Vector3.up * moveSpeed * Time.deltaTime;
    }
}
