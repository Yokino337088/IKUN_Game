using TangmenFramework;
using UnityEngine;

/// <summary>
/// Boss激光弹幕
/// 环绕Boss自身旋转，像能量环一样
/// 激光以Boss中心为圆心，在指定半径的圆周上均匀分布并旋转
/// </summary>
public class BossLaser : BulletBase
{
    private Transform orbitCenter;
    private float orbitRadius;
    private float currentAngle;
    private float rotationSpeed;
    private bool initialized;

    public void Init(Transform center, float radius, float angle, float speed)
    {
        base.Init(0f);

        orbitCenter = center;
        orbitRadius = radius;
        currentAngle = angle;
        rotationSpeed = speed;
        lifetime = 5f;
        initialized = true;

        UpdateTransform();
    }

    protected override void MoveLogic(float deltaTime)
    {
        if (!initialized || orbitCenter == null)
            return;

        currentAngle += rotationSpeed * deltaTime;
        UpdateTransform();
    }

    private void UpdateTransform()
    {
        float x = orbitCenter.position.x + Mathf.Cos(currentAngle) * orbitRadius;
        float y = orbitCenter.position.y + Mathf.Sin(currentAngle) * orbitRadius;
        transform.position = new Vector3(x, y, 0);

        Vector2 tangent = new Vector2(-Mathf.Sin(currentAngle), Mathf.Cos(currentAngle));
        float zRotation = Mathf.Atan2(tangent.y, tangent.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, zRotation);
    }

    public new void Recycle()
    {
        initialized = false;
        orbitCenter = null;
        base.Recycle();
    }
}
