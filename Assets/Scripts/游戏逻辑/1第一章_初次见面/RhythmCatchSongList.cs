using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 第一章所有可选歌曲的列表。
/// 挂在 ChapterOneLevelBootstrap 上，或通过 Resources/AB 加载。
/// 选歌 UI 从此列表读取，选中后传给 GameManager.LoadSong()。
/// </summary>
[CreateAssetMenu(fileName = "歌曲列表_第一章", menuName = "IKUN Game/第一章/歌曲列表")]
public class RhythmCatchSongList : ScriptableObject
{
    [Header("歌曲列表")]
    [Tooltip("本章所有可挑战的歌曲配置。")]
    public List<RhythmCatchSongConfig> songs = new List<RhythmCatchSongConfig>();

    /// <summary>
    /// 获取歌曲数量。
    /// </summary>
    public int Count => songs != null ? songs.Count : 0;

    /// <summary>
    /// 按索引获取歌曲配置，越界返回 null。
    /// </summary>
    public RhythmCatchSongConfig GetSong(int index)
    {
        if (songs == null || index < 0 || index >= songs.Count)
            return null;
        return songs[index];
    }

    /// <summary>
    /// 获取第一首有效歌曲，没有则返回 null。
    /// </summary>
    public RhythmCatchSongConfig GetFirstValidSong()
    {
        if (songs == null)
            return null;

        foreach (RhythmCatchSongConfig song in songs)
        {
            if (song != null && song.HasBeatMap)
                return song;
        }

        return null;
    }
}
