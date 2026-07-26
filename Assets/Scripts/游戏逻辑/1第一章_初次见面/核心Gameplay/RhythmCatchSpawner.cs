using System.Collections.Generic;
using TangmenFramework;
using UnityEngine;

/// <summary>
/// 根据谱面生成掉落物，并使用唐老师框架的 GOPoolMgr 复用 GameObject。
/// 该脚本只负责“何处创建、何处回收”，具体的 DSP 移动和判定由掉落物与游戏管理器负责。
/// </summary>
public class RhythmCatchSpawner : MonoBehaviour
{
    [SerializeField] private RhythmCatchFallingItem itemPrefab;
    [SerializeField] private Transform activeRoot;
    [SerializeField] private float spawnY = 5.3f;
    [SerializeField] private float hitY = -2.75f;
    [SerializeField, Min(3)] private int laneCount = 5;
    [SerializeField, Min(0.5f)] private float laneSpacing = 2f;

    // GOPoolMgr 负责真正的对象缓存；此集合仅记录当前关卡还在场上的对象数量。
    private readonly HashSet<RhythmCatchFallingItem> _activeItems = new HashSet<RhythmCatchFallingItem>();

    public int ActiveCount => _activeItems.Count;
    public float HitY => hitY;

    private void Awake()
    {
        if (activeRoot == null)
            activeRoot = transform;
    }

    /// <summary>
    /// 从框架对象池获取一个掉落物并写入本次谱面指令。
    /// </summary>
    public RhythmCatchFallingItem Spawn(
        RhythmCatchBeatNote note,
        double hitDspTime,
        double travelDuration,
        RhythmCatchPlayerController player,
        RhythmCatchGameManager gameManager)
    {
        if (itemPrefab == null)
        {
            LogSystem.Error("RhythmCatchSpawner 缺少掉落物预制体。");
            return null;
        }

        // 使用框架 GOPoolMgr，而不是频繁 Instantiate/Destroy，避免高密度谱面的 GC 和卡顿。
        GameObject pooledObject = GOPoolMgr.Instance.GetObj(itemPrefab.gameObject);
        RhythmCatchFallingItem item = pooledObject != null
            ? pooledObject.GetComponent<RhythmCatchFallingItem>()
            : null;

        if (item == null)
        {
            LogSystem.Error("节奏接箱对象池返回的预制体缺少 RhythmCatchFallingItem 组件。");
            return null;
        }

        float worldX = (Mathf.Clamp(note.lane, 0, laneCount - 1) - (laneCount - 1) * 0.5f) * laneSpacing;
        item.transform.SetParent(activeRoot, false);
        item.Initialize(
            note,
            new Vector3(worldX, spawnY, 0f),
            hitY,
            hitDspTime,
            travelDuration,
            player,
            gameManager,
            this);

        _activeItems.Add(item);
        return item;
    }

    /// <summary>
    /// 将已经结算的掉落物交还给框架对象池。
    /// </summary>
    public void Despawn(RhythmCatchFallingItem item)
    {
        if (item == null || !_activeItems.Remove(item))
            return;

        item.PrepareForPool();
        GOPoolMgr.Instance.PushObj(item.gameObject);
    }

    /// <summary>
    /// 重开、失败或离开关卡时一次性回收所有在场物体。
    /// </summary>
    public void DespawnAll()
    {
        if (_activeItems.Count == 0)
            return;

        RhythmCatchFallingItem[] items = new RhythmCatchFallingItem[_activeItems.Count];
        _activeItems.CopyTo(items);
        foreach (RhythmCatchFallingItem item in items)
            Despawn(item);
    }

    /// <summary>
    /// 供白盒搭建逻辑写入预制体与轨道参数。
    /// </summary>
    public void Configure(
        RhythmCatchFallingItem prefab,
        Transform activeItemsRoot,
        float topY,
        float targetY,
        int lanes,
        float spacing)
    {
        itemPrefab = prefab;
        activeRoot = activeItemsRoot;
        spawnY = topY;
        hitY = targetY;
        laneCount = Mathf.Max(3, lanes);
        laneSpacing = Mathf.Max(0.5f, spacing);
    }
}
