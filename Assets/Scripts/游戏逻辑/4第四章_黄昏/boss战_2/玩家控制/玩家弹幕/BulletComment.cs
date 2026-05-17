using TangmenFramework;
using TMPro;
using UnityEngine;

/// <summary>
/// 玩家大招全屏弹幕
/// 从屏幕右边缘生成，向左飞行，超出左边缘后回收到对象池
/// 文字由TMP（TextMeshPro）渲染
/// </summary>
public class BulletComment : MonoBehaviour
{
    [SerializeField]
    private TextMeshPro tmpText;

    /// <summary>
    /// 向左移动的速度
    /// </summary>
    private float moveSpeed;

    /// <summary>
    /// 是否已激活（防止Update在回收后继续执行）
    /// </summary>
    private bool isActive;

    /// <summary>
    /// 主相机引用
    /// </summary>
    private Camera mainCamera;

    /// <summary>
    /// 超出屏幕左边缘的额外余量
    /// </summary>
    private float leftMargin = 0f;

    private int minDamage = 80;
    private int maxDamage = 100;
    private float critChance = 0.5f;
    private float critMultiplier = 1.25f;

    private void Awake()
    {
        mainCamera = Camera.main;
    }

    private void OnEnable()
    {
        // 对象池取出时确保激活
        isActive = true;
    }

    private void OnDisable()
    {
        // 对象池回收时确保停止
        isActive = false;
    }

    /// <summary>
    /// 初始化弹幕文字和速度
    /// </summary>
    /// <param name="text">弹幕文字内容</param>
    /// <param name="speed">向左移动速度</param>
    public void Init(string text, float speed)
    {
        isActive = true;
        tmpText.text = text;
        moveSpeed = speed;
        tmpText.color = GetRandomCommentColor();
    }

    private void Update()
    {
        if (!isActive)
            return;

        // 向左移动
        transform.position += Vector3.left * moveSpeed * Time.deltaTime;

        // 超出屏幕左边缘 → 回收
        if (IsOffLeftEdge())
        {
            Recycle();
        }
    }

    /// <summary>
    /// 检测是否超出屏幕左边缘
    /// </summary>
    private bool IsOffLeftEdge()
    {
        if (mainCamera == null)
            return false;

        Vector3 viewPos = mainCamera.WorldToViewportPoint(transform.position);
        return viewPos.x < -leftMargin;
    }

    /// <summary>
    /// 回收弹幕到对象池
    /// </summary>
    private void Recycle()
    {        
        GOPoolMgr.Instance.PushObj(gameObject);
    }

    /// <summary>
    /// 从彩虹色中随机生成弹幕颜色（HSV色相0~360全覆盖）
    /// </summary>
    private Color GetRandomCommentColor()
    {
        return Color.HSVToRGB(Random.Range(0f, 1f), 0.8f, 1f);
    }

    public int CalculateDamage(out bool isCrit)
    {
        // 在最小值到最大值之间随机取一个基础伤害值
        int baseDamage = UnityEngine.Random.Range(minDamage, maxDamage + 1);

        // 随机判定是否暴击
        isCrit = UnityEngine.Random.value < critChance;

        if (isCrit)
        {
            int critDamage = Mathf.RoundToInt(baseDamage * critMultiplier);
            return critDamage;
        }

        return baseDamage;
    }
}
