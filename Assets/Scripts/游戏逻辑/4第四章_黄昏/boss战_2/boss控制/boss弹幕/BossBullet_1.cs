using UnityEngine;

/// <summary>
/// 圆形散射弹幕子弹（直线运动）
/// 继承 BossBulletBase，实现匀速直线移动
/// </summary>
public class BossBullet_1 : BulletBase
{
    //子弹的移动方向
    private Vector2 moveDirection;

    /// <summary>
    /// 初始化直线子弹
    /// </summary>
    /// <param name="direction">发射方向（会被归一化）</param>
    /// <param name="speed">移动速度</param>
    public void Init(Vector2 direction, float speed)
    {
        base.Init(speed);
        moveDirection = direction.normalized;
    }

    /// <summary>
    /// 直线运动：每帧沿固定方向匀速移动
    /// 数学公式：P(t) = P(0) + dir * speed * t
    /// </summary>
    protected override void MoveLogic(float deltaTime)
    {
        transform.Translate(moveDirection * moveSpeed * deltaTime, Space.World);
    }
}
