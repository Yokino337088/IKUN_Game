using DG.Tweening;
using TangmenFramework;
using TMPro;
using UnityEngine;

/// <summary>
/// 浮动加分数字组件。
/// 在掉落物被接到的位置生成，向上飘动并淡出，1 秒后自动回收到框架对象池。
/// 预制体需挂 TMP_Text（TextMeshPro）组件，从 GOPoolMgr 获取和回收。
/// </summary>
public class FloatingScoreText : MonoBehaviour
{
    [Header("动画参数")]
    [SerializeField, Min(0f)]
    [Tooltip("向上飘动的距离（世界单位）。")]
    private float floatDistance = 1.5f;

    [SerializeField, Min(0.1f)]
    [Tooltip("从出现到完全淡出消失的总时长。")]
    private float duration = 1f;

    /// <summary>缓存的 TMP 文本组件，避免每帧 GetComponent。</summary>
    private TMP_Text _scoreText;

    private void Awake()
    {
        _scoreText = GetComponent<TMP_Text>();
    }

    /// <summary>
    /// 在指定世界坐标显示加分数字，自动播放上飘淡出动画，结束后回池。
    /// </summary>
    /// <param name="score">加分数值。</param>
    /// <param name="worldPosition">生成的世界坐标。</param>
    /// <param name="color">文本颜色（不同掉落物类型对应不同颜色）。</param>
    public void Show(int score, Vector3 worldPosition, Color color)
    {
        if (_scoreText == null)
        {
            _scoreText = GetComponent<TMP_Text>();
            if (_scoreText == null)
                return;
        }

        gameObject.SetActive(true);
        transform.position = worldPosition;

        _scoreText.text = $"+{score}";
        _scoreText.color = color;
        _scoreText.alpha = 1f;

        // 上飘 + 淡出，动画结束后交回对象池。
        Vector3 targetPos = worldPosition + Vector3.up * floatDistance;
        transform.DOMove(targetPos, duration).SetEase(Ease.OutQuad);
        _scoreText.DOFade(0f, duration).SetEase(Ease.InQuad)
            .OnComplete(() => PushBackToPool());
    }

    /// <summary>
    /// 供 GOPoolMgr 回收前清理 DOTween 动画，避免下一局残留运动状态。
    /// </summary>
    public void PrepareForPool()
    {
        transform.DOKill();
        if (_scoreText != null)
            _scoreText.DOKill();
    }

    /// <summary>结束动画后将自身交回框架对象池。</summary>
    private void PushBackToPool()
    {
        PrepareForPool();
        GOPoolMgr.Instance.PushObj(gameObject);
    }
}
