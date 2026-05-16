using UnityEngine;
using TangmenFramework;

public class MazeBall : MonoBehaviour
{    
    /// <summary>
    /// 隐藏篮球（用于收集效果）
    /// </summary>
    public void HideBall()
    {
        gameObject.SetActive(false);
    }
}