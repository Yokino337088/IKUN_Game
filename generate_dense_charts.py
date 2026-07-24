#!/usr/bin/env python3
"""
节奏接箱谱面生成器 v2
- 基于歌曲时长和 BPM 计算节拍数
- 按歌曲结构（intro/verse/pre-chorus/chorus/bridge/outro/finale）生成不同密度和模式的音符
- 全曲无空档，每半拍至少一个音符
- 道具分布：Normal 0, Gold 1, Gem 2, Bomb 3, Shield 4, Magnet 5
- 歌曲时长来源：bang=87s, fantastic_baby=118s, good_boy=118s
"""
import re, os, hashlib

# ============================================================
# 歌曲配置
# ============================================================
SONGS = [
    {
        "name": "bang",
        "filepath": r"d:\Unity_Project\IKUN_Game\Assets\资源统一放置点\歌曲配置\第一章正式歌曲\谱面_bang.asset",
        "bpm": 132, "duration_s": 87, "lane_count": 5,
        "sections": [
            (0, 12, "intro"), (12, 28, "verse"), (28, 44, "pre_chorus"),
            (44, 64, "chorus"), (64, 80, "verse"), (80, 96, "pre_chorus"),
            (96, 116, "chorus"), (116, 132, "bridge"),
            (132, 156, "chorus"), (156, 172, "outro"), (172, 192, "finale"),
        ],
    },
    {
        "name": "fantastic baby",
        "filepath": r"d:\Unity_Project\IKUN_Game\Assets\资源统一放置点\歌曲配置\第一章正式歌曲\谱面_fantastic baby.asset",
        "bpm": 128, "duration_s": 118, "lane_count": 5,
        "sections": [
            (0, 16, "intro"), (16, 36, "verse"), (36, 52, "pre_chorus"),
            (52, 72, "chorus"), (72, 92, "verse"), (92, 108, "pre_chorus"),
            (108, 132, "chorus"), (132, 152, "verse"),
            (152, 168, "pre_chorus"), (168, 192, "chorus"),
            (192, 212, "bridge"), (212, 236, "chorus"), (236, 252, "outro"),
        ],
    },
    {
        "name": "good boy",
        "filepath": r"d:\Unity_Project\IKUN_Game\Assets\资源统一放置点\歌曲配置\第一章正式歌曲\谱面_good boy.asset",
        "bpm": 108, "duration_s": 118, "lane_count": 5,
        "sections": [
            (0, 16, "intro"), (16, 36, "verse"), (36, 52, "pre_chorus"),
            (52, 72, "chorus"), (72, 92, "verse"), (92, 108, "pre_chorus"),
            (108, 128, "chorus"), (128, 144, "bridge"),
            (144, 168, "chorus"), (168, 188, "verse"), (188, 212, "outro"),
        ],
    },
]

# sec_idx: section display index
SEC_IDX = {"intro": 0, "verse": 1, "pre_chorus": 2, "chorus": 3, "bridge": 4, "outro": 5, "finale": 3}


def stable_hash(beat, lane, salt=0):
    """确定性伪随机，同一输入总是相同输出。"""
    key = f"{beat:.2f}_{lane}_{salt}"
    return int(hashlib.md5(key.encode()).hexdigest()[:8], 16) / 0xFFFFFFFF


def get_section(beat, sections):
    for s, e, t in sections:
        if s <= beat < e:
            return t, s, e
    return "intro", 0, sections[-1][1] if sections else 100


def pick_lane(beat, lane_count, prev_lane):
    """基于 beat 确定性选择 lane，避开 prev_lane。"""
    candidates = [l for l in range(lane_count) if l != prev_lane]
    idx = int(stable_hash(beat, prev_lane, 1) * len(candidates))
    return candidates[idx % len(candidates)]


def pick_item(beat, lane, section_type):
    """
    物品类型：Normal=0, Gold=1, Gem=2, Bomb=3, Shield=4, Magnet=5
    炸弹在 chorus 少，bridge 多；护盾分散；磁铁在 verse/chorus。
    """
    h = stable_hash(beat, lane, 2)
    bomb_pct = {"bridge": 9, "pre_chorus": 7, "outro": 5, "verse": 5, "chorus": 4, "intro": 3, "finale": 4}
    bp = bomb_pct.get(section_type, 5)
    # 炸弹
    if h < bp / 100.0:
        return 3
    # 护盾（5% 整体，炸弹多的段落稍高）
    h2 = stable_hash(beat, lane, 3)
    shield_pct = 6 if section_type == "bridge" else 5
    if h2 < shield_pct / 100.0:
        return 4
    # 磁铁（4%，分散在 verse/chorus/pre_chorus）
    h3 = stable_hash(beat, lane, 4)
    if h3 < 4 / 100.0:
        return 5
    # 金块（16%）
    h4 = stable_hash(beat, lane, 5)
    if h4 < 16 / 100.0:
        return 1
    # 宝石（10%）
    h5 = stable_hash(beat, lane, 6)
    if h5 < 10 / 100.0:
        return 2
    return 0


def generate_notes(bpm, lane_count, end_beat, sections):
    """生成全曲连续音符，每 0.5 拍至少 1 个。"""
    notes = []
    prev_lane = -1
    beat = 0.5

    while beat < end_beat:
        sect, s_start, s_end = get_section(beat, sections)
        rel = (beat - s_start) / max(1, s_end - s_start)  # 0.0 ~ 1.0

        # 主音符：每半拍至少一个
        lane = pick_lane(beat, lane_count, prev_lane)
        item = pick_item(beat, lane, sect)
        is_strong = abs(beat - round(beat)) < 0.001
        notes.append({"beat": round(beat, 1), "lane": lane, "itemType": item,
                       "isStrongBeat": is_strong, "fallSpeedMultiplier": 1.0})
        prev_lane = lane

        # 副歌/预副歌：整拍加双音；intro/outro 只在强拍偶尔加
        if abs(beat - round(beat)) < 0.001:
            if sect in ("chorus",):
                # 副歌每整拍都有双音
                lane2 = pick_lane(beat + 0.01, lane_count, lane)
                item2 = pick_item(beat + 0.01, lane2, sect)
                notes.append({"beat": round(beat, 1), "lane": lane2, "itemType": item2,
                               "isStrongBeat": True, "fallSpeedMultiplier": 1.0})
                prev_lane = lane2
            elif sect in ("pre_chorus", "finale") and stable_hash(beat, 0, 7) < 0.6:
                lane2 = pick_lane(beat + 0.02, lane_count, lane)
                item2 = pick_item(beat + 0.02, lane2, sect)
                notes.append({"beat": round(beat, 1), "lane": lane2, "itemType": item2,
                               "isStrongBeat": True, "fallSpeedMultiplier": 1.0})
                prev_lane = lane2
            elif sect in ("verse",) and stable_hash(beat, 0, 8) < 0.35:
                lane2 = pick_lane(beat + 0.03, lane_count, lane)
                item2 = pick_item(beat + 0.03, lane2, sect)
                notes.append({"beat": round(beat, 1), "lane": lane2, "itemType": item2,
                               "isStrongBeat": True, "fallSpeedMultiplier": 1.0})
                prev_lane = lane2

        # 副歌：半拍间加额外音（更高密度）
        if sect in ("chorus",) and abs(beat - round(beat)) > 0.01 and stable_hash(beat, 0, 9) < 0.5:
            lane3 = pick_lane(beat + 0.04, lane_count, prev_lane)
            item3 = pick_item(beat + 0.04, lane3, sect)
            notes.append({"beat": round(beat, 1), "lane": lane3, "itemType": item3,
                           "isStrongBeat": False, "fallSpeedMultiplier": 1.0})

        beat += 0.5
        beat = round(beat, 1)

    # 去重排序
    seen = {}
    unique = []
    for n in notes:
        k = (n["beat"], n["lane"])
        if k not in seen:
            seen[k] = True
            unique.append(n)
    unique.sort(key=lambda x: (x["beat"], x["lane"]))
    return unique


def fmt_note(n):
    return (f'  - beat: {n["beat"]}\n    lane: {n["lane"]}\n'
            f'    itemType: {n["itemType"]}\n    isStrongBeat: {1 if n["isStrongBeat"] else 0}\n'
            f'    fallSpeedMultiplier: {n["fallSpeedMultiplier"]}\n')


def fmt_sections(sections):
    lines = ["  sections:"]
    for s, e, t in sections:
        lines.append(f"  - startBeat: {s}")
        lines.append(f"    section: {SEC_IDX.get(t, 0)}")
    return "\n".join(lines)


def replace_in_file(filepath, new_notes, end_beat, sections):
    if not os.path.exists(filepath):
        print(f"  ERROR: {filepath} not found")
        return False
    with open(filepath, "r", encoding="utf-8") as f:
        content = f.read()

    # Update endBeat
    content = re.sub(r'(\n\s*endBeat:\s*)\d+', f'\n  endBeat: {end_beat}', content)

    # Replace notes + sections
    notes_yaml = "".join(fmt_note(n) for n in new_notes)
    sections_yaml = fmt_sections(sections)
    replacement = f"  notes:\n{notes_yaml}{sections_yaml}"

    # Match from "  notes:" through "  sections:" to end of sections block
    pattern = r'  notes:\n(?:  - beat:.*?\n(?:    .*?\n)*)*  sections:(?:\n  - startBeat:.*?\n    section:.*?)*'
    new_content = re.sub(pattern, replacement, content)
    if new_content == content:
        print(f"  WARNING: Regex didn't match for {filepath}")
        return False

    with open(filepath, "w", encoding="utf-8") as f:
        f.write(new_content)
    return True


def main():
    for song in SONGS:
        end_beat = int(song["duration_s"] * song["bpm"] / 60.0)
        print(f"\n{'='*60}")
        print(f"Song: {song['name']}  BPM={song['bpm']}  Duration={song['duration_s']}s  endBeat={end_beat}")
        notes = generate_notes(song["bpm"], song["lane_count"], end_beat, song["sections"])
        print(f"Total notes: {len(notes)}")

        counts = {}
        for n in notes:
            counts[n["itemType"]] = counts.get(n["itemType"], 0) + 1
        names = {0: "Normal", 1: "Gold", 2: "Gem", 3: "Bomb", 4: "Shield", 5: "Magnet"}
        for t in sorted(counts):
            print(f"  {names.get(t, t):>7}: {counts[t]:>4} ({counts[t]*100/len(notes):5.1f}%)")

        ok = replace_in_file(song["filepath"], notes, end_beat, song["sections"])
        print(f"  {'OK' if ok else 'FAIL'}: {os.path.basename(song['filepath'])}")


if __name__ == "__main__":
    main()
