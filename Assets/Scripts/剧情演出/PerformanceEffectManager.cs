using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using TangmenFramework;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 演出特效管理器 —— 管理剧情演出中的全屏视觉特效。
/// 
/// 【支持的特效类型】
/// - ColorFilter：全屏颜色滤镜（暖色调/冷色调/回忆滤镜等）
/// - ScreenFade：淡入淡出（黑场/白场过渡）
/// - ScreenBlur：画面模糊（模拟眩晕/回忆/意识模糊）
/// - Flash：瞬间闪白/闪黑（心理冲击效果）
/// - Particle：粒子特效生成（氛围粒子/情绪粒子）
/// 
/// 【实现方式】
/// 使用一个覆盖全屏的UI Image（filterOverlay）来应用各种后期效果。
/// 颜色滤镜和淡入淡出通过修改该Image的color实现。
/// 模糊效果需要一个额外的RawImage + RenderTexture方案，或使用Shader。
/// 
/// 【使用方式】
/// 挂载到场景空GameObject上，在PerformanceDirector中引用。
/// 需要场景中存在一个全屏覆盖的Canvas + Image（filterOverlay）。
/// </summary>
public class PerformanceEffectManager : MonoBehaviour
{
    [Header("========== 全屏覆盖层 ==========")]

    [Tooltip("全屏滤镜Image（需设置RaycastTarget=false避免阻挡点击）")]
    public Image filterOverlay;

    [Tooltip("全屏模糊层RawImage（可选，用于模糊效果）")]
    public RawImage blurOverlay;

    [Header("========== 粒子特效 ==========")]

    [Tooltip("粒子特效生成父节点")]
    public Transform particleRoot;

    private Color filterOriginalColor;
    private Material blurMaterial;

    private void Awake()
    {
        if (filterOverlay != null)
        {
            filterOriginalColor = filterOverlay.color;
            // 确保不阻挡射线
            filterOverlay.raycastTarget = false;
            // 初始状态：全透明
            Color c = filterOriginalColor;
            c.a = 0f;
            filterOverlay.color = c;
        }

        if (blurOverlay != null)
        {
            blurOverlay.raycastTarget = false;
            blurOverlay.gameObject.SetActive(false);
        }
    }

    // ============================================================
    //  颜色滤镜
    // ============================================================

    /// <summary>
    /// 设置全屏颜色滤镜。
    /// color=目标颜色（含alpha），duration=过渡时间
    /// 
    /// 使用示例：
    /// - 暖色回忆：new Color(1, 0.85f, 0.7f, 0.3f)
    /// - 冰冷氛围：new Color(0.6f, 0.7f, 1f, 0.3f)
    /// - 压抑暗红：new Color(0.5f, 0.1f, 0.1f, 0.4f)
    /// </summary>
    public async UniTask SetColorFilter(Color color, float duration, CancellationToken ct)
    {
        if (filterOverlay == null)
        {
            LogSystem.Debug("PerformanceEffectManager: filterOverlay 未设置！");
            return;
        }

        filterOverlay.gameObject.SetActive(true);

        if (duration > 0f)
        {
            Tween tween = filterOverlay.DOColor(color, duration);
            await UniTask.WaitForSeconds(duration);
            if (ct.IsCancellationRequested)
                tween.Kill();
        }
        else
        {
            filterOverlay.color = color;
        }
    }

    /// <summary>清除颜色滤镜（恢复透明）</summary>
    public async UniTask ClearColorFilter(float duration, CancellationToken ct)
    {
        if (filterOverlay == null) return;

        Color clear = filterOriginalColor;
        clear.a = 0f;
        await SetColorFilter(clear, duration, ct);
    }

    // ============================================================
    //  淡入淡出
    // ============================================================

    /// <summary>
    /// 屏幕淡入/淡出。
    /// targetAlpha=目标不透明度（0=透明 1=全黑/全白）
    /// </summary>
    public async UniTask ScreenFade(float targetAlpha, float duration, CancellationToken ct)
    {
        if (filterOverlay == null)
        {
            LogSystem.Debug("PerformanceEffectManager: filterOverlay 未设置！");
            return;
        }

        filterOverlay.gameObject.SetActive(true);
        Color target = filterOriginalColor;
        target.a = targetAlpha;

        if (duration > 0f)
        {
            Tween tween = filterOverlay.DOColor(target, duration);
            await UniTask.WaitForSeconds(duration);
            if (ct.IsCancellationRequested)
                tween.Kill();
        }
        else
        {
            filterOverlay.color = target;
        }
    }

    // ============================================================
    //  模糊效果
    // ============================================================

    /// <summary>
    /// 设置画面模糊强度。
    /// 注意：模糊效果需要配合专门的后处理Shader或使用RenderTexture方案。
    /// 默认实现使用DOTween控制blurOverlay的alpha作为简化方案。
    /// 如需真正的高斯模糊，可替换为Unity PostProcessing或自定义Blur Shader。
    /// </summary>
    public async UniTask SetBlur(float strength, float duration, CancellationToken ct)
    {
        if (blurOverlay == null)
        {
            LogSystem.Debug("PerformanceEffectManager: blurOverlay 未设置！");
            return;
        }

        float targetAlpha = Mathf.Clamp01(strength / 10f);
        blurOverlay.gameObject.SetActive(targetAlpha > 0.01f);

        if (duration > 0f)
        {
            Tween tween = blurOverlay.DOFade(targetAlpha, duration);
            await UniTask.WaitForSeconds(duration);
            if (ct.IsCancellationRequested)
                tween.Kill();
        }
        else
        {
            Color c = blurOverlay.color;
            c.a = targetAlpha;
            blurOverlay.color = c;
        }
    }

    // ============================================================
    //  闪白/闪黑
    // ============================================================

    /// <summary>
    /// 画面闪白/闪黑效果。
    /// flashColor=闪光颜色（白色=闪白，黑色=闪黑）
    /// fadeInTime=亮起时间（极短，产生"闪"的感觉）
    /// fadeOutTime=消退时间
    /// </summary>
    public async UniTask Flash(Color flashColor, float fadeInTime, float fadeOutTime, CancellationToken ct)
    {
        if (filterOverlay == null) return;

        filterOverlay.gameObject.SetActive(true);

        // 快速变亮
        Tween tween1 = filterOverlay.DOColor(flashColor, fadeInTime);
        await UniTask.WaitForSeconds(fadeInTime);
        if (ct.IsCancellationRequested)
        {
            tween1.Kill();
            return;
        }
        
        // 缓慢消退
        Color clear = flashColor;
        clear.a = 0f;
        Tween tween2 = filterOverlay.DOColor(clear, fadeOutTime);
        await UniTask.WaitForSeconds(fadeOutTime);
        if (ct.IsCancellationRequested)
            tween2.Kill();
    }

    // ============================================================
    //  粒子特效
    // ============================================================

    /// <summary>
    /// 生成粒子特效。
    /// 从AB包加载粒子预制体并实例化到场景中。
    /// 粒子系统应配置为自动销毁（Stop Action = Destroy）。
    /// </summary>
    public async void SpawnParticle(string resName, string abName, Vector3 worldPos)
    {
        if (string.IsNullOrEmpty(resName))
        {
            LogSystem.Debug("PerformanceEffectManager: 粒子资源名为空！");
            return;
        }

        GameObject prefab = await ABResMgr.Instance.LoadResAsync<GameObject>(abName, resName);
        if (prefab != null)
        {
            Transform parent = particleRoot != null ? particleRoot : transform;
            Instantiate(prefab, worldPos, Quaternion.identity, parent);
        }
    }
}
