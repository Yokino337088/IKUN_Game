using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using TangmenFramework;
using UnityEngine;

/// <summary>
/// 玩家大招全屏弹幕管理器
/// 从JsonDataMgr读取弹幕配置数据，随机选取文字，
/// 在屏幕右边缘随机Y位置生成弹幕，弹幕向左飞行超出左边缘后自动回收
/// </summary>
public class BulletCommentMgr : MonoBehaviour
{
    [SerializeField]
    [Header("弹幕预制体（挂载BulletComment组件 + TMP）")]
    private GameObject commentPrefab;

    [SerializeField]
    [Header("弹幕移动速度范围")]
    private Vector2 speedRange = new Vector2(3f, 5f);

    [SerializeField]
    [Header("弹幕在Y轴上的生成范围（视口坐标0~1，默认全屏高度）")]
    private Vector2 ySpawnRange = new Vector2(0.05f, 0.95f);

    /// <summary>
    /// 弹幕文字配置数据缓存
    /// </summary>
    private List<string> commentTexts;

    /// <summary>
    /// 主相机
    /// </summary>
    private Camera mainCamera;

    private void Awake()
    {
        mainCamera = Camera.main;
        LoadCommentData();
    }

    /// <summary>
    /// 从JsonDataMgr加载弹幕文字配置数据
    /// </summary>
    private void LoadCommentData()
    {
        commentTexts = new List<string>();

        //调用API获取弹幕配置表数据
        JsonDataMgr.Instance.LoadTableFromAB<T_BulletCommentsContainer, T_BulletComments>();
        var container = JsonDataMgr.Instance.GetTable<T_BulletCommentsContainer>();
        //如果数据表为空或加载失败，使用默认弹幕文字
        if (container == null || container.dataDic == null || container.dataDic.Count == 0)
        {
            LogSystem.Warning("弹幕配置表为空或加载失败，使用默认弹幕文字");
            commentTexts.Add("IKUN万岁！");
            commentTexts.Add("鸡你太美");
            return;
        }

        //遍历数据表，提取弹幕文字信息
        foreach (var kvp in container.dataDic)
        {
            if (!string.IsNullOrEmpty(kvp.Value.textInfo))
            {
                //将弹幕文字添加到列表中
                commentTexts.Add(kvp.Value.textInfo);
            }
        }

        LogSystem.Info($"弹幕文字加载完成，共 {commentTexts.Count} 条");
    }

    /// <summary>
    /// 生成全屏弹幕
    /// </summary>
    /// <param name="count">弹幕数量</param>
    public void SpawnComments(int count)
    {
        if (commentPrefab == null)
        {
            LogSystem.Error("弹幕预制体为空，无法生成弹幕");
            return;
        }

        if (commentTexts == null || commentTexts.Count == 0)
        {
            LogSystem.Error("弹幕文字数据为空，无法生成弹幕");
            return;
        }

        for (int i = 0; i < count; i++)
        {
            SpawnSingleComment(i * 0.08f).Forget();
        }
    }

    /// <summary>
    /// 通过UniTask延迟生成单条弹幕，实现错落有致的效果
    /// </summary>
    private async UniTaskVoid SpawnSingleComment(float delaySeconds)
    {
        // 延迟生成，避免所有弹幕同时出现
        if (delaySeconds > 0)
            await UniTask.Delay((int)(delaySeconds * 1000));

        // 从对象池获取弹幕实例
        GameObject commentGO = GOPoolMgr.Instance.GetObj(commentPrefab);
        BulletComment comment = commentGO.GetComponent<BulletComment>();
        if (comment == null)
        {
            LogSystem.Error("弹幕预制体上未挂载BulletComment组件");
            GOPoolMgr.Instance.PushObj(commentGO);
            return;
        }

        // 随机选取弹幕文字
        string randomText = commentTexts[Random.Range(0, commentTexts.Count)];

        // 随机速度
        float randomSpeed = Random.Range(speedRange.x, speedRange.y);

        // 计算生成位置：屏幕右边缘外侧，随机Y坐标
        Vector3 spawnPos = GetSpawnPosition();

        commentGO.transform.position = spawnPos;
        comment.Init(randomText, randomSpeed);
    }

    /// <summary>
    /// 计算弹幕生成位置：屏幕右边缘外侧 + 随机Y坐标
    /// </summary>
    private Vector3 GetSpawnPosition()
    {
        // 右边缘外侧（viewport x = 1.1，即超出屏幕右边一点）
        float viewportX = 1.1f;
        float viewportY = Random.Range(ySpawnRange.x, ySpawnRange.y);

        Vector3 worldPos = mainCamera.ViewportToWorldPoint(new Vector3(viewportX, viewportY, 0));
        worldPos.z = 0;
        return worldPos;
    }
}
