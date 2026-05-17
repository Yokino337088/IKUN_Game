using TangmenFramework;
using UnityEngine;

/// <summary>
/// Boss弹幕工厂类（工厂模式 + 对象池）
/// 封装弹幕的创建、初始化、回收逻辑
/// 内部使用GOPoolMgr管理GameObject生命周期
/// </summary>
public class BulletFactory
{    
    private GameObject bulletPrefab1;
    private GameObject bulletPrefab2;
    private GameObject bulletPrefab3;
    private GameObject laserPrefab;

    private bool _isInitialized;

    /// <summary>
    /// 初始化工厂，传入不同类型子弹的预制体
    /// </summary>
    public void Init(GameObject bullet1Prefab, GameObject bullet2Prefab, GameObject bullet3Prefab = null, GameObject laserPrefab = null)
    {
        bulletPrefab1 = bullet1Prefab;
        bulletPrefab2 = bullet2Prefab;
        bulletPrefab3 = bullet3Prefab;
        this.laserPrefab = laserPrefab;
        _isInitialized = true;
    }

    public BossBullet_1 SpawnBullet1(Vector3 spawnPos, Vector2 direction, float speed)
    {
        BossBullet_1 bullet = GetBullet<BossBullet_1>(bulletPrefab1, spawnPos);
        if (bullet != null)
        {
            bullet.Init(direction, speed);
        }
        return bullet;
    }

    public BossBullet_2 SpawnBullet2(Vector3 spawnPos, Vector2 direction, float speed)
    {
        BossBullet_2 bullet = GetBullet<BossBullet_2>(bulletPrefab2, spawnPos);
        if (bullet != null)
        {
            bullet.Init(direction, speed);
        }
        return bullet;
    }

    /// <summary>
    /// 生成蓄力大火球（自动追踪玩家，到达一定距离后爆炸）
    /// </summary>
    /// <param name="spawnPos">生成位置</param>
    /// <param name="direction">初始方向</param>
    /// <param name="speed">移动速度</param>
    /// <param name="trackingStrength">追踪强度（0~1，默认0.6）</param>
    /// <param name="explosionDistance">爆炸触发距离（默认3）</param>
    public BossBullet_3 SpawnBullet3(Vector3 spawnPos, Vector2 direction, float speed)
    {
        BossBullet_3 bullet = GetBullet<BossBullet_3>(bulletPrefab3, spawnPos);
        if (bullet != null)
        {
            bullet.Init(speed);
        }
        return bullet;
    }

    /// <summary>
    /// 生成环绕旋转的激光
    /// </summary>
    /// <param name="center">环绕中心（Boss的Transform）</param>
    /// <param name="radius">环绕半径</param>
    /// <param name="angle">初始角度（弧度）</param>
    /// <param name="rotationSpeed">旋转速度</param>
    public BossLaser SpawnLaser(Transform center, float radius, float angle, float rotationSpeed)
    {
        if (!_isInitialized || laserPrefab == null)
        {
            LogSystem.Warning("BulletFactory未初始化或激光预制体为空");
            return null;
        }

        GameObject laserGO = GOPoolMgr.Instance.GetObj(laserPrefab);
        BossLaser laser = laserGO.GetComponent<BossLaser>();
        if (laser != null)
        {
            laser.Init(center, radius, angle, rotationSpeed);
        }
        return laser;
    }

    private T GetBullet<T>(GameObject prefab, Vector3 spawnPos) where T : BulletBase
    {
        if (!_isInitialized || prefab == null)
        {
            LogSystem.Warning($"BulletFactory未初始化或预制体为空");
            return null;
        }
        GameObject bulletGO = GOPoolMgr.Instance.GetObj(prefab);
        bulletGO.transform.position = spawnPos;
        return bulletGO.GetComponent<T>();
    }

    public void RecycleBullet(GameObject bulletGO)
    {
        if (bulletGO != null)
        {
            GOPoolMgr.Instance.PushObj(bulletGO);
        }
    }

    public bool IsInitialized => _isInitialized;
}
