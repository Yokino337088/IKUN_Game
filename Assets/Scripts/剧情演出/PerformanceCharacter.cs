using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using TangmenFramework;
using UnityEngine;

/// <summary>
/// 演出角色控制器 —— 管理演出中所有角色的Sprite显示、移动、表情切换和动画效果。
/// 
/// 【使用方式】
/// 挂载到场景空GameObject上。角色以子GameObject形式注册到 characterMap 中，
/// 每个角色子对象需挂载 SpriteRenderer 组件。
/// 
/// 【注册方式】
/// 1. 手动：在Inspector中将角色GameObject拖入 characterPrefabs 列表
/// 2. 动态：代码调用 RegisterCharacter(name, gameObject)
/// 
/// 【意识流演出中的角色表现】
/// - 角色可以随时淡入淡出，表现记忆/幻觉的闪现
/// - 震动效果模拟内心震撼或外部冲击
/// - 表情切换在实时演算中进行，配合文字推进情绪
/// </summary>
public class PerformanceCharacter : MonoBehaviour
{
    [Tooltip("角色根节点（所有角色放在此节点下）")]
    public Transform characterRoot;

    [Tooltip("预注册的角色列表（GameObject名即角色名）")]
    public List<GameObject> characterPrefabs = new List<GameObject>();

    /// <summary>角色映射表（角色名 → SpriteRenderer）</summary>
    private Dictionary<string, SpriteRenderer> characterMap = new Dictionary<string, SpriteRenderer>();

    private void Awake()
    {
        // 从预注册列表构建映射
        foreach (var go in characterPrefabs)
        {
            if (go == null) continue;
            var sr = go.GetComponent<SpriteRenderer>();
            if (sr != null)
                characterMap[go.name] = sr;
        }

        // 从characterRoot子节点自动注册
        if (characterRoot != null)
        {
            foreach (Transform child in characterRoot)
            {
                var sr = child.GetComponent<SpriteRenderer>();
                if (sr != null && !characterMap.ContainsKey(child.name))
                    characterMap[child.name] = sr;
            }
        }
    }

    /// <summary>手动注册角色</summary>
    public void RegisterCharacter(string name, GameObject characterGO)
    {
        if (characterGO == null) return;
        var sr = characterGO.GetComponent<SpriteRenderer>();
        if (sr != null)
            characterMap[name] = sr;
    }

    /// <summary>获取角色SpriteRenderer</summary>
    public SpriteRenderer GetCharacter(string name)
    {
        characterMap.TryGetValue(name, out var sr);
        return sr;
    }

    /// <summary>
    /// 设置角色显隐（支持淡入淡出过渡）。
    /// duration > 0 时使用DOTween渐变Alpha，= 0 时直接设置。
    /// </summary>
    public async UniTask SetVisibility(string name, bool visible, float duration, CancellationToken ct)
    {
        var sr = GetCharacter(name);
        if (sr == null)
        {
            LogSystem.Debug($"PerformanceCharacter: 未找到角色 [{name}]");
            return;
        }

        float targetAlpha = visible ? 1f : 0f;

        if (duration > 0f)
        {
            Tween tween = sr.DOFade(targetAlpha, duration);
            await UniTask.WaitForSeconds(duration);
            if (ct.IsCancellationRequested)
                tween.Kill();
        }
        else
        {
            Color c = sr.color;
            c.a = targetAlpha;
            sr.color = c;
        }

        sr.gameObject.SetActive(visible || duration <= 0f);
    }

    /// <summary>
    /// 移动角色到指定位置。
    /// characterTargetPos 为屏幕归一化坐标（0-1），内部转换为世界坐标。
    /// </summary>
    public async UniTask MoveTo(string name, Vector2 normalizedPos, float duration, CancellationToken ct)
    {
        var sr = GetCharacter(name);
        if (sr == null)
        {
            LogSystem.Debug($"PerformanceCharacter: 未找到角色 [{name}]");
            return;
        }

        // 归一化坐标转世界坐标
        Vector3 worldPos = NormalizedToWorld(normalizedPos);
        worldPos.z = sr.transform.position.z;

        if (duration > 0f)
        {
            Tween tween = sr.transform.DOMove(worldPos, duration);
            await UniTask.WaitForSeconds(duration);
            if (ct.IsCancellationRequested)
                tween.Kill();
        }
        else
        {
            sr.transform.position = worldPos;
        }
    }

    /// <summary>切换角色表情/姿态Sprite</summary>
    public async UniTask SetExpression(string name, string spriteName, string abName, CancellationToken ct)
    {
        var sr = GetCharacter(name);
        if (sr == null)
        {
            LogSystem.Debug($"PerformanceCharacter: 未找到角色 [{name}]");
            return;
        }

        if (string.IsNullOrEmpty(spriteName))
            return;

        Sprite newSprite = await ABResMgr.Instance.LoadResAsync<Sprite>(abName, spriteName);
        if (newSprite != null)
            sr.sprite = newSprite;
    }

    /// <summary>角色震动效果</summary>
    public async UniTask Shake(string name, float intensity, float duration, CancellationToken ct)
    {
        var sr = GetCharacter(name);
        if (sr == null)
        {
            LogSystem.Debug($"PerformanceCharacter: 未找到角色 [{name}]");
            return;
        }

        Tween tween = sr.transform.DOShakePosition(duration, intensity, 30, 90, false, true);
        await UniTask.WaitForSeconds(duration);
        if (ct.IsCancellationRequested)
            tween.Kill();
    }

    /// <summary>归一化屏幕坐标转世界坐标</summary>
    private Vector3 NormalizedToWorld(Vector2 normalized)
    {
        Camera cam = Camera.main;
        if (cam == null) return Vector3.zero;

        Vector3 screenPos = new Vector3(
            normalized.x * Screen.width,
            normalized.y * Screen.height,
            -cam.transform.position.z
        );
        return cam.ScreenToWorldPoint(screenPos);
    }
}
