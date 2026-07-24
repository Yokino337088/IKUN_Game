using UnityEngine;

/// <summary>
/// 一首歌曲的完整配置 —— 将谱面、音乐资源、显示信息打包在一起。
/// GameManager 通过 LoadSong() 动态切换歌曲，无需修改 Inspector 硬编码引用。
/// </summary>
[CreateAssetMenu(fileName = "歌曲配置_", menuName = "IKUN Game/第一章/歌曲配置")]
public class RhythmCatchSongConfig : ScriptableObject
{
    [Header("显示信息")]
    [Tooltip("在选歌界面显示的歌曲名。")]
    public string songDisplayName = "未命名歌曲";

    [Tooltip("歌曲封面/图标（可选）。")]
    public Sprite songIcon;

    [Tooltip("歌曲难度标签（如 EASY / NORMAL / HARD）。")]
    public string difficultyTag = "NORMAL";

    [Header("谱面")]
    [Tooltip("该歌曲对应的节奏接箱谱面。")]
    public RhythmCatchBeatMap beatMap;

    [Header("音乐资源 — 方式一：直接拖曳（优先）")]
    [Tooltip("直接从 Inspector 拖入 AudioClip，无需 AB 加载。优先级高于 AB 方式。")]
    public AudioClip directMusicClip;

    [Header("音乐资源 — 方式二：AB 包加载（备选）")]
    [Tooltip("框架 AssetBundle 名称，如 music_chapter_one。")]
    public string musicAssetBundleName;

    [Tooltip("AB 包中的 AudioClip 资源名。")]
    public string musicResourceName;

    /// <summary>
    /// 是否有有效的音乐资源配置（直接拖曳或 AB 加载任一有效即为 true）。
    /// </summary>
    public bool HasMusic => directMusicClip != null
                         || (!string.IsNullOrWhiteSpace(musicAssetBundleName)
                          && !string.IsNullOrWhiteSpace(musicResourceName));

    /// <summary>
    /// 是否有有效的谱面。
    /// </summary>
    public bool HasBeatMap => beatMap != null;

    /// <summary>
    /// 谱面 BPM（便捷访问，来自 beatMap.bpm）。
    /// </summary>
    public float BPM => beatMap != null ? beatMap.bpm : 120f;

    /// <summary>
    /// 歌曲总时长（秒），来自谱面的 DurationSeconds。
    /// </summary>
    public float DurationSeconds => beatMap != null ? beatMap.DurationSeconds : 0f;
}
