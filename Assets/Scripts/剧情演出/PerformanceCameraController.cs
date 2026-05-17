using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;

/// <summary>
/// 演出摄像机控制器 —— 管理2D剧情演出中的摄像机效果。
/// 
/// 【支持的镜头语言】
/// - Shake：震动（冲击、爆炸、情绪波动）
/// - Zoom：缩放（聚焦特写/拉远全景）
/// - Pan：平移（视线转移/跟拍）
/// - Rotate：Z轴旋转（眩晕/意识模糊/空间扭曲感——意识流核心运镜）
/// - Reset：恢复初始状态
/// 
/// 【2D演出的镜头特性】
/// 2D游戏使用正交投影（Orthographic），Zoom通过调整orthographicSize实现，
/// Pan通过移动Camera的Transform实现，旋转绕Z轴。
/// 
/// 【使用方式】
/// 挂载到场景中的主摄像机上，在PerformanceDirector中引用。
/// </summary>
public class PerformanceCameraController : MonoBehaviour
{
    [Header("========== 初始参数（演出结束后恢复至此）==========")]

    [Tooltip("初始正交大小")]
    public float defaultOrthoSize = 5f;

    [Tooltip("初始位置")]
    public Vector3 defaultPosition;

    private Camera cam;
    private float originalOrthoSize;
    private Vector3 originalPosition;

    private void Awake()
    {
        cam = GetComponent<Camera>();
        if (cam != null)
        {
            originalOrthoSize = cam.orthographicSize;
            originalPosition = transform.position;
        }
        else
        {
            originalOrthoSize = defaultOrthoSize;
            originalPosition = defaultPosition;
        }
    }

    /// <summary>
    /// 摄像机震动。
    /// strength=震动幅度，duration=持续时间，vibrato=震动频率
    /// </summary>
    public void Shake(float strength = 0.5f, float duration = 0.3f, int vibrato = 20)
    {
        if (cam == null) return;

        // 只震动位置，不震动旋转（2D不需要旋转震动）
        transform.DOShakePosition(duration, strength, vibrato, 90, false, true);
    }

    /// <summary>
    /// 摄像机缩放（调整orthographicSize）。
    /// targetSize=目标正交大小，duration=过渡时间
    /// </summary>
    public async UniTask ZoomTo(float targetSize, float duration, CancellationToken ct)
    {
        if (cam == null) return;

        Tween tween = cam.DOOrthoSize(targetSize, duration);
        await UniTask.WaitForSeconds(duration);
        if (ct.IsCancellationRequested)
            tween.Kill();
    }

    /// <summary>
    /// 摄像机平移。
    /// offset=相对于初始位置的世界坐标偏移量
    /// </summary>
    public async UniTask PanTo(Vector3 offset, float duration, CancellationToken ct)
    {
        if (cam == null) return;

        Vector3 targetPos = originalPosition + offset;
        Tween tween = transform.DOMove(targetPos, duration);
        await UniTask.WaitForSeconds(duration);
        if (ct.IsCancellationRequested)
            tween.Kill();
    }

    /// <summary>
    /// 摄像机Z轴旋转。
    /// 意识流核心运镜——轻微旋转产生眩晕/不安感，大幅旋转表现空间扭曲。
    /// angle=目标Z轴欧拉角（如5=微倾，15=明显倾斜，90=完全侧转）
    /// </summary>
    public async UniTask RotateTo(float angle, float duration, CancellationToken ct)
    {
        if (cam == null) return;

        Vector3 targetEuler = new Vector3(0, 0, angle);
        Tween tween = transform.DORotate(targetEuler, duration);
        await UniTask.WaitForSeconds(duration);
        if (ct.IsCancellationRequested)
            tween.Kill();
    }

    /// <summary>恢复摄像机到初始状态</summary>
    public void ResetCamera()
    {
        if (cam == null) return;

        cam.orthographicSize = originalOrthoSize;
        transform.position = originalPosition;

        // 杀掉所有DOTween动画
        transform.DOKill();
        cam.DOKill();
    }

    private void OnDestroy()
    {
        transform.DOKill();
        if (cam != null) cam.DOKill();
    }
}
