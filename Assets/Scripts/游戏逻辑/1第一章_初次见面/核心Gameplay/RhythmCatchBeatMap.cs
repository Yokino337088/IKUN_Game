using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 一首音乐对应一份节奏接箱谱面。
/// 运行时只读取数据，不在场景脚本中硬编码音符，后续可直接扩展可视化谱面编辑器。
/// </summary>
[CreateAssetMenu(fileName = "节奏接箱_谱面", menuName = "IKUN Game/第一章/节奏接箱谱面")]
public class RhythmCatchBeatMap : ScriptableObject
{
    [Header("Music")]
    [Tooltip("每分钟节拍数。所有音符的秒时间都由 BPM 动态换算。")]
    [Min(1f)] public float bpm = 128f;
    [Tooltip("每小节拍数，当前白盒默认使用 4/4 拍。")]
    [Min(1)] public int beatsPerBar = 4;

    [Header("Lanes")]
    [Range(3, 8)] public int laneCount = 5;
    [Min(0.5f)] public float laneSpacing = 2f;

    [Header("Timing")]
    [Tooltip("物体从出生点落到判定线需要的基础拍数。")]
    [Min(0.5f)] public float travelBeats = 4f;
    [Tooltip("关卡结束拍。需要大于最后一个有效音符拍点。")]
    [Min(0f)] public float endBeat = 104f;

    [Header("Chart")]
    public List<RhythmCatchBeatNote> notes = new List<RhythmCatchBeatNote>();
    public List<RhythmCatchSectionMark> sections = new List<RhythmCatchSectionMark>();

    /// <summary>
    /// 单拍时长。使用 double 是为了与 AudioSettings.dspTime 保持同级精度。
    /// </summary>
    public double SecondsPerBeat => 60d / Mathf.Max(1f, bpm);

    /// <summary>
    /// 谱面的总时长，包含最后一个物体的完整下落时间。
    /// </summary>
    public float DurationSeconds
    {
        get
        {
            float lastBeat = endBeat;
            if (notes != null && notes.Count > 0)
                lastBeat = Mathf.Max(lastBeat, notes[notes.Count - 1].beat + travelBeats);
            return (float)(lastBeat * SecondsPerBeat);
        }
    }

    /// <summary>
    /// 将轨道索引转换为以场景中心为零点的世界坐标。
    /// </summary>
    public float GetLaneWorldX(int lane)
    {
        int clampedLane = Mathf.Clamp(lane, 0, Mathf.Max(0, laneCount - 1));
        return (clampedLane - (laneCount - 1) * 0.5f) * laneSpacing;
    }

    /// <summary>
    /// 评级所使用的安全箱数量。宝石只计 Combo，炸弹需要躲避，因此二者不进入接取率分母。
    /// </summary>
    public int CountRatedNotes()
    {
        if (notes == null)
            return 0;

        int count = 0;
        foreach (RhythmCatchBeatNote note in notes)
        {
            if (note.itemType != RhythmCatchItemType.Gem && note.itemType != RhythmCatchItemType.Bomb)
                count++;
        }
        return count;
    }

    /// <summary>
    /// Inspector 值变更时自动修复空引用，避免运行时 NullReferenceException。
    /// </summary>
    private void OnValidate()
    {
        if (notes == null)
            notes = new List<RhythmCatchBeatNote>();
        if (sections == null)
            sections = new List<RhythmCatchSectionMark>();

        // 在资源保存阶段就清洗数据，避免运行时出现非法轨道或零下落速度。
        foreach (RhythmCatchBeatNote note in notes)
        {
            note.lane = Mathf.Clamp(note.lane, 0, Mathf.Max(0, laneCount - 1));
            note.fallSpeedMultiplier = Mathf.Max(0.1f, note.fallSpeedMultiplier);
        }

        // 生成器依赖时间顺序逐条读取，因此始终保持列表有序。
        notes.Sort((left, right) => left.beat.CompareTo(right.beat));
        sections.Sort((left, right) => left.startBeat.CompareTo(right.startBeat));
    }
}
