/// <summary>
/// 平台判断工具类 —— 将平台宏判断收敛到此处，项目其他地方统一用 IsMobile / IsPC
/// 
/// 无需挂载到场景，直接用 PlatformHelper.IsMobile / PlatformHelper.IsPC 即可
/// </summary>
public static class PlatformHelper
{
    /// <summary>
    /// 当前是否为移动端（安卓、微信小游戏、QQ小游戏）
    /// </summary>
    public static bool IsMobile
    {
        get
        {
#if UNITY_ANDROID || (UNITY_WEBGL && WX_MINI_GAME) || (UNITY_WEBGL && QQ_MINI_GAME)
            return true;
#elif UNITY_STANDALONE_WIN || UNITY_STANDALONE_OSX || UNITY_STANDALONE_LINUX || UNITY_WEBGL
            return false;
#endif
        }
    }

    /// <summary>
    /// 当前是否为PC端（Windows/Mac/Linux/桌面浏览器WebGL）
    /// </summary>
    public static bool IsPC => !IsMobile;
}
