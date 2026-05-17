using UnityEngine;

/// <summary>
/// 螺旋弹幕子弹（螺旋曲线运动）
/// 继承 BossBulletBase，实现真正的螺旋轨迹
/// </summary>
public class BossBullet_2 : BulletBase
{
    /// <summary>
    /// 初始发射方向（归一化后的单位向量）
    /// </summary>
    private Vector2 initialDirection;

    /// <summary>
    /// 螺旋半径基数：决定螺旋的宽度
    /// </summary>
    private float spiralRadius = 0.5f;

    /// <summary>
    /// 螺旋频率：决定旋转的快慢，值越大每秒钟转的圈数越多
    /// </summary>
    private float spiralFrequency = 1f;

    /// <summary>
    /// 发射时的起始位置（世界坐标）
    /// </summary>
    private Vector2 spawnPosition;

    /// <summary>
    /// 初始化螺旋子弹
    /// </summary>
    /// <param name="direction">子弹前进方向</param>
    /// <param name="speed">前进速度</param>
    public void Init(Vector2 direction, float speed)
    {
        base.Init(speed);
        initialDirection = direction.normalized;
        spawnPosition = transform.position;

        // 随机螺旋半径和频率，让不同子弹有不同的螺旋形态
        spiralRadius = Random.Range(1f, 3f);
        spiralFrequency = Random.Range(2f, 5f);
    }

    /// <summary>
    /// 螺旋曲线运动
    /// 
    /// 数学原理（阿基米德螺旋变体）：
    ///   1. 中心路径：沿初始方向匀速前进
    ///      center(t) = spawnPos + initialDir * speed * t
    /// 
    ///   2. 旋转偏移：围绕中心路径做圆周旋转，半径随时间扩大
    ///      angle(t) = t * frequency * 2π
    ///      radius(t) = spiralRadius * (1 + t * 0.3)
    ///      offset(t) = { cos(angle(t)), sin(angle(t)) } * radius(t)
    /// 
    ///   3. 最终位置：
    ///      position(t) = center(t) + offset(t)
    /// 
    /// 视觉效果：子弹边前进边绕圈，圈子越来越大，形成螺旋弹幕
    /// </summary>
    protected override void MoveLogic(float deltaTime)
    {
        // 计算当前旋转角度（timer是累计时间，随帧增长）
        float angle = timer * spiralFrequency * 2f * Mathf.PI;

        // 半径随时间扩大：初始为 spiralRadius，每秒扩大30%
        float radius = spiralRadius * (1f + timer * 0.3f);

        // 螺旋偏移量：在垂直于前进方向的平面内做圆周运动
        Vector2 spiralOffset = new Vector2(Mathf.Cos(angle) , Mathf.Sin(angle)) * radius;

        // 中心位置：沿初始方向匀速前进
        Vector2 centerPos = spawnPosition + initialDirection * moveSpeed * timer;

        // 最终位置 = 中心路径 + 螺旋偏移
        transform.position = centerPos + spiralOffset;
    }
}
