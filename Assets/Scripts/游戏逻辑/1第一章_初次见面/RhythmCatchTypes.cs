using System;
using UnityEngine;

/// <summary>节奏接箱中的掉落物类型。</summary>
public enum RhythmCatchItemType { Normal, Gold, Gem, Bomb, Magnet, Shield }

/// <summary>单次接取的节拍判定结果。</summary>
public enum RhythmCatchJudgement { None, Perfect, Great, Good, Miss, Bomb, Shielded }

/// <summary>关卡运行状态。</summary>
public enum RhythmCatchGameState { Ready, Countdown, Playing, Finished, Failed }

/// <summary>策划案中的歌曲段落类型。</summary>
public enum RhythmCatchSectionType { Intro, Verse, PreChorus, Chorus, Bridge, Outro }

/// <summary>
/// 第一章节奏接箱使用的框架事件名。UI 与玩法通过 EventCenter 解耦通信。
/// </summary>
public static class RhythmCatchEventNames
{
    public const string HudChanged = "第一章_节奏接箱_HUD更新";
    public const string JudgementChanged = "第一章_节奏接箱_判定反馈";
    public const string BeatTriggered = "第一章_节奏接箱_节拍触发";
    /// <summary>歌曲切换时触发，参数为歌曲名。</summary>
    public const string SongSelected = "第一章_节奏接箱_歌曲选中";
}

/// <summary>
/// 一条谱面指令。beat 使用“拍”而不是秒，修改 BPM 后仍能保持音乐结构。
/// </summary>
[Serializable]
public sealed class RhythmCatchBeatNote
{
    [Tooltip("物体命中判定线时位于第几拍，可填写 4.5 表示第 4 拍半。")]
    [Min(0f)] public float beat;
    [Tooltip("从左到右的轨道索引。")]
    [Min(0)] public int lane;
    [Tooltip("掉落物类型。")]
    public RhythmCatchItemType itemType = RhythmCatchItemType.Normal;
    [Tooltip("是否为强拍。")]
    public bool isStrongBeat;
    [Tooltip("相对基础下落速度的倍率。")]
    [Min(0.1f)] public float fallSpeedMultiplier = 1f;
}

/// <summary>歌曲段落标记，用于 HUD 和背景表现切换。</summary>
[Serializable]
public sealed class RhythmCatchSectionMark
{
    [Min(0f)] public float startBeat;
    public RhythmCatchSectionType section = RhythmCatchSectionType.Intro;
}

/// <summary>玩法层发送给 HUD 的完整只读快照。</summary>
public struct RhythmCatchHudSnapshot
{
    public RhythmCatchGameState state;
    public RhythmCatchSectionType section;
    public int score;
    public int combo;
    public int maxCombo;
    public float scoreMultiplier;
    public int health;
    public int maxHealth;
    public bool hasShield;
    public float magnetRemaining;
    public float elapsedSeconds;
    public float durationSeconds;
    public float catchRate;
    public string grade;
    public int countdown;
    /// <summary>当前歌曲名（多歌曲模式）。</summary>
    public string songName;
    /// <summary>当前歌曲序号（0-based）。</summary>
    public int songIndex;
    /// <summary>歌曲总数。</summary>
    public int totalSongs;
    /// <summary>跨歌曲累计总分。</summary>
    public int totalAccumulatedScore;
    /// <summary>间奏剩余秒数（0=没有间奏或已结束）。</summary>
    public float interludeRemaining;
}
