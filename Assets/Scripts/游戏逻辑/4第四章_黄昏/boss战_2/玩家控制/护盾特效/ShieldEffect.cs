using UnityEngine;

/// <summary>
/// 护盾特效控制器
/// 控制护盾材质上的受击波纹参数（_HitColor, _HitStrength, _HitPosition）
/// 每帧自动衰减 _HitStrength，让波纹从碰撞点向外扩散并逐渐消失
/// </summary>
public class ShieldEffect : MonoBehaviour
{
    [SerializeField]
    [Header("护盾材质（使用 ForceFieldShield Shader）")]
    private Material shieldMaterial;

    [SerializeField]
    [Header("波纹衰减速度（1→0所需秒数）")]
    private float decaySpeed = 3f;

    private float _hitStrength;
    private static readonly int HitStrengthId = Shader.PropertyToID("_HitStrength");
    private static readonly int HitPositionId = Shader.PropertyToID("_HitPosition");

    private void Awake()
    {
        // 如果没手动赋值材质，自动从 Renderer 获取实例
        if (shieldMaterial == null)
        {
            var renderer = GetComponentInChildren<Renderer>();
            if (renderer != null)
                shieldMaterial = renderer.material;
        }
        else
        {
            // 已手动赋值：获取 Renderer 的实例化材质，确保 SetFloat/SetVector 修改的是实例而非原始资源
            var renderer = GetComponentInChildren<Renderer>();
            if (renderer != null && renderer.sharedMaterial == shieldMaterial)
                shieldMaterial = renderer.material;
        }
    }

    private void Update()
    {
        // 每帧衰减 HitStrength，直到归零
        if (_hitStrength > 0f)
        {
            _hitStrength = Mathf.Max(0f, _hitStrength - Time.deltaTime * decaySpeed);
            shieldMaterial.SetFloat(HitStrengthId, _hitStrength);
        }
    }

    /// <summary>
    /// 播放受击波纹效果（外部在子弹碰撞时调用）
    /// </summary>
    /// <param name="worldHitPos">子弹碰撞点的世界坐标</param>
    public void PlayHitEffect(Vector3 worldHitPos)
    {
        if (shieldMaterial == null) return;

        _hitStrength = 1f;
        shieldMaterial.SetFloat(HitStrengthId, 1f);
        shieldMaterial.SetVector(HitPositionId, worldHitPos);
    }

    private void OnDestroy()
    {
        // 清理材质状态
        if (shieldMaterial != null)
        {
            shieldMaterial.SetFloat(HitStrengthId, 0f);
        }
    }
}
