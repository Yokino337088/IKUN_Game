using System;
using UnityEngine;

/// <summary>
/// 单个掉落物的运行时行为（2D版本）。
/// 位置严格根据 AudioSettings.dspTime 计算，而不是累加 Time.deltaTime，
/// 即使某一帧卡顿，物体下一帧也会自动回到正确的音乐时间位置。
/// 2D 模式下使用 SpriteRenderer 渲染，通过调整 sprite color 区分箱子类型。
/// </summary>
public class RhythmCatchFallingItem : MonoBehaviour
{
    [Header("2D Visual")]
    [SerializeField] private Transform visualRoot;
    [SerializeField] private SpriteRenderer visualRenderer;
    [SerializeField] private Transform landingShadow;
    [SerializeField] private SpriteRenderer shadowRenderer;

    [Header("Visual Config")]
    [Tooltip("不同箱子类型对应的 Sprite 图片配置。所有掉落物共享此配置。")]
    [SerializeField] private RhythmCatchItemVisualConfig visualConfig;

    [Header("Movement")]
    [SerializeField, Min(0.1f)] private float missedFallSpeed = 4f;
    [SerializeField, Min(0f)] private float magnetFollowSpeed = 9f;

    private RhythmCatchBeatNote _note;
    private RhythmCatchPlayerController _player;
    private RhythmCatchGameManager _gameManager;
    private RhythmCatchSpawner _spawner;
    private double _spawnDspTime;
    private double _hitDspTime;
    private float _spawnY;
    private float _hitY;
    private float _baseVisualScale = 1f;
    private bool _isRunning;
    private bool _isResolved;
    private BoxCollider2D _itemCollider;
    private Rigidbody2D _rb;

    private void Awake()
    {
        _itemCollider = GetComponent<BoxCollider2D>();
        if (_itemCollider == null)
            _itemCollider = gameObject.AddComponent<BoxCollider2D>();
        _itemCollider.isTrigger = true;

        // 必须挂 Rigidbody2D（Kinematic），否则两个 Trigger 之间不会触发 OnTriggerEnter2D。
        _rb = GetComponent<Rigidbody2D>();
        if (_rb == null)
            _rb = gameObject.AddComponent<Rigidbody2D>();
        _rb.bodyType = RigidbodyType2D.Kinematic;
        _rb.simulated = true;
    }

    public RhythmCatchBeatNote Note => _note;
    public RhythmCatchItemType ItemType => _note != null ? _note.itemType : RhythmCatchItemType.Normal;

    /// <summary>
    /// 从对象池取出时写入本次音符的全部时序数据。
    /// </summary>
    public void Initialize(
        RhythmCatchBeatNote note,
        Vector3 spawnPosition,
        float targetY,
        double hitDspTime,
        double travelDuration,
        RhythmCatchPlayerController player,
        RhythmCatchGameManager gameManager,
        RhythmCatchSpawner spawner)
    {
        _note = note;
        _player = player;
        _gameManager = gameManager;
        _spawner = spawner;
        _hitDspTime = hitDspTime;
        _spawnDspTime = hitDspTime - Math.Max(0.05d, travelDuration);
        _spawnY = spawnPosition.y;
        _hitY = targetY;

        transform.position = spawnPosition;
        transform.rotation = Quaternion.identity;
        _isResolved = false;
        ApplyAppearance();
        // 隐藏地面落点阴影
        if (landingShadow != null)
            landingShadow.gameObject.SetActive(false);
        _isRunning = true;
        gameObject.SetActive(true);
    }

    /// <summary>
    /// 供 GOPoolMgr 回收前清理场景对象引用，避免跨局残留。
    /// </summary>
    public void PrepareForPool()
    {
        _isRunning = false;
        _note = null;
        _player = null;
        _gameManager = null;
        _spawner = null;
    }

    /// <summary>
    /// 供白盒预制体生成逻辑绑定 2D 模型和落点影子。
    /// </summary>
    public void Configure(Transform itemVisual, SpriteRenderer itemRenderer, Transform shadow, SpriteRenderer shadowVisual)
    {
        visualRoot = itemVisual;
        visualRenderer = itemRenderer;
        landingShadow = shadow;
        shadowRenderer = shadowVisual;
    }

    private void Update()
    {
        if (!_isRunning || _gameManager == null)
            return;

        double dspTime = AudioSettings.dspTime;
        double travelLength = Math.Max(0.05d, _hitDspTime - _spawnDspTime);
        float travelProgress = Mathf.Clamp01((float)((dspTime - _spawnDspTime) / travelLength));

        Vector3 position = transform.position;

        // 按 DSP 时间驱动物体下落；超过命中拍后继续下落作为视觉反馈。
        position.y = dspTime <= _hitDspTime
            ? Mathf.Lerp(_spawnY, _hitY, travelProgress)
            : _hitY - (float)(dspTime - _hitDspTime) * missedFallSpeed;

        // 磁铁吸附。
        if (_gameManager.IsMagnetActive && ItemType != RhythmCatchItemType.Bomb && ItemType != RhythmCatchItemType.Magnet && _player != null && dspTime < _hitDspTime)
            position.x = Mathf.MoveTowards(position.x, _player.transform.position.x, magnetFollowSpeed * Time.deltaTime);

        transform.position = position;
        transform.Rotate(0f, 0f, GetRotationSpeed() * Time.deltaTime, Space.Self);
        UpdateLandingShadow(travelProgress);

        // 超出 GOOD 窗口仍未接住 → 按 MISS / 炸弹躲避处理。
        if (!_isResolved && dspTime > _hitDspTime + _gameManager.GoodWindowSeconds)
        {
            _isResolved = true;
            if (ItemType == RhythmCatchItemType.Bomb)
                _gameManager.ResolveBombAvoided(this);
            else
                _gameManager.ResolveMiss(this);
        }
    }

    /// <summary>
    /// 玩家碰撞体触碰到掉落物时触发接取判定。
    /// 替换了原来的手动 bounds.Intersects 判定线机制。
    /// </summary>
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!_isRunning || _isResolved || _gameManager == null || _player == null)
            return;

        if (other == _player.CatchCollider)
        {
            _isResolved = true;
            double dspTime = AudioSettings.dspTime;
            _gameManager.ResolveCatch(this, (float)Math.Abs(dspTime - _hitDspTime));
        }
    }

    /// <summary>
    /// 根据箱子类型设置 Sprite 图片（来自 visualConfig）和缩放。
    /// 不再覆盖颜色 —— 图片使用自身原始颜色。
    /// </summary>
    private void ApplyAppearance()
    {
        _baseVisualScale = 1f;

        switch (ItemType)
        {
            case RhythmCatchItemType.Gold:
                _baseVisualScale = 1.1f;
                break;
            case RhythmCatchItemType.Gem:
                _baseVisualScale = 0.58f;
                break;
            case RhythmCatchItemType.Bomb:
                _baseVisualScale = 0.92f;
                break;
            // Normal / Magnet / Shield 保持默认 1f
        }

        // 强拍微微放大（叠加在类型缩放之上）。
        if (_note != null && _note.isStrongBeat)
            _baseVisualScale *= 1.08f;

        if (visualRoot != null)
            visualRoot.localScale = Vector3.one * _baseVisualScale;

        // 从视觉配置中获取对应类型的 Sprite 图片，未配置时保持默认方块。
        if (visualRenderer != null && visualConfig != null)
        {
            Sprite typeSprite = visualConfig.GetSprite(ItemType);
            if (typeSprite != null)
                visualRenderer.sprite = typeSprite;
        }
    }

    private void UpdateLandingShadow(float progress)
    {
        if (landingShadow == null)
            return;

        Vector3 itemPosition = transform.position;
        landingShadow.position = new Vector3(itemPosition.x, _hitY - 0.18f, 0.18f);
        float scale = Mathf.Lerp(0.2f, 1.05f, progress) * _baseVisualScale;
        landingShadow.localScale = new Vector3(scale, 0.08f, scale);

        // 2D 模式直接设置 SpriteRenderer.color 控制透明度。
        if (shadowRenderer != null)
            shadowRenderer.color = new Color(0f, 0f, 0f, Mathf.Lerp(0.08f, 0.35f, progress));
    }

    private float GetRotationSpeed()
    {
        switch (ItemType)
        {
            case RhythmCatchItemType.Gem:
                return 150f;
            case RhythmCatchItemType.Bomb:
                return -90f;
            default:
                return 45f;
        }
    }
}
