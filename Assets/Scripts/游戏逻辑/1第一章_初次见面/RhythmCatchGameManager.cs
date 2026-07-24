using System;
using System.Collections.Generic;
using TangmenFramework;
using UnityEngine;

/// <summary>
/// 第一章"节奏接箱"的核心玩法管理器（多歌曲连播版）。
/// 负责 DSP 节拍调度、谱面生成、判定、Combo、生命、道具、评级，并通过 EventCenter 驱动 UI。
/// </summary>
public class RhythmCatchGameManager : MonoBehaviour
{
    public static RhythmCatchGameManager Instance { get; private set; }

    [Header("Scene References")]
    [SerializeField] private RhythmCatchBeatMap beatMap;
    [SerializeField] private RhythmCatchPlayerController player;
    [SerializeField] private RhythmCatchSpawner spawner;
    [SerializeField] private AudioSource musicSource;

    [Header("Music - 方式一：直接拖曳（优先）")]
    [Tooltip("直接从 Inspector 拖入 AudioClip，无需 AB 加载。优先级最高。")]
    [SerializeField] private AudioClip directMusicClip;

    [Header("Music - 方式二：AB 包加载（备选）")]
    [Tooltip("如需正式音乐，可填写框架 AssetBundle 名称；留空时白盒静默运行。")]
    [SerializeField] private string musicAssetBundleName;
    [Tooltip("AssetBundle 中的 AudioClip 资源名。")]
    [SerializeField] private string musicResourceName;

    [Header("Level Timing")]
    [Tooltip("场景加载后是否自动开始。第一章改为 false，由剧情对话结束后的 InitLevel 事件驱动。")]
    [SerializeField] private bool autoStart = false;
    [SerializeField, Min(0f)] private float countdownSeconds = 3f;
    [SerializeField, Min(0.001f)] private float perfectWindowSeconds = 0.04f;
    [SerializeField, Min(0.001f)] private float greatWindowSeconds = 0.09f;
    [SerializeField, Min(0.001f)] private float goodWindowSeconds = 0.15f;

    [Header("Rules")]
    [SerializeField, Min(1)] private int maxHealth = 5;
    [SerializeField, Min(1)] private int normalBaseScore = 100;
    [SerializeField, Min(0f)] private float magnetDurationSeconds = 5f;

    [Header("Multi-Song Playlist (第一章多曲连播)")]
    [Tooltip("多首歌曲顺序挑战的歌曲列表。留空则退化为单曲模式。")]
    [SerializeField] private RhythmCatchSongList songList;
    [Tooltip("歌曲间奏等待秒数。")]
    [SerializeField, Min(1f)] private float interludeSeconds = 4f;

    [Header("Level Transition")]
    [Tooltip("全部歌曲完成后，跳转到下一个关卡的场景名称（留空则只显示回到开始场景）。")]
    [SerializeField] 
    private string nextLevelSceneName = "";

    [Header("Sound Effects")]
    [Tooltip("音效所在的 AssetBundle 名称（如 sound_general）。")]
    [SerializeField] private string sfxBundleName = "sound_general";

    [Range(0f, 1f)]
    [Tooltip("开局时自动设置到框架 MusicMgr 的全局音效音量。")]
    [SerializeField] private float sfxVolume = 1f;

    [Header("SFX — 直接拖曳（优先）")]
    [Tooltip("接到普通箱子时播放的音效")]
    [SerializeField] private AudioClip sfxNormal;
    [Tooltip("接到 Gold 箱子时播放的音效")]
    [SerializeField] private AudioClip sfxGold;
    [Tooltip("接到 Gem 宝石时播放的音效")]
    [SerializeField] private AudioClip sfxGem;
    [Tooltip("接到 Magnet 磁铁时播放的音效")]
    [SerializeField] private AudioClip sfxMagnet;
    [Tooltip("接到 Shield 护盾时播放的音效")]
    [SerializeField] private AudioClip sfxShield;
    [Tooltip("接到炸弹时播放的音效")]
    [SerializeField] private AudioClip sfxBombHit;
    [Tooltip("Miss 时播放的音效")]
    [SerializeField] private AudioClip sfxMiss;

    [Header("SFX — AB 包加载（备选，直接拖曳为空时生效）")]
    [Tooltip("普通箱子音效资源名")]
    [SerializeField] private string sfxNormalName = "";
    [Tooltip("Gold 箱子音效资源名")]
    [SerializeField] private string sfxGoldName = "";
    [Tooltip("Gem 宝石音效资源名")]
    [SerializeField] private string sfxGemName = "";
    [Tooltip("Magnet 磁铁音效资源名")]
    [SerializeField] private string sfxMagnetName = "";
    [Tooltip("Shield 护盾音效资源名")]
    [SerializeField] private string sfxShieldName = "";
    [Tooltip("接到炸弹时播放的音效资源名")]
    [SerializeField] private string sfxBombHitName = "";
    [Tooltip("Miss 时播放的音效资源名")]
    [SerializeField] private string sfxMissName = "";

    /// <summary>当前歌曲序号（0-based，单曲模式为 0）。</summary>
    private int _currentSongIndex = 0;
    /// <summary>间奏结束 DSP 时间。</summary>
    private double _interludeEndDspTime;
    /// <summary>跨歌曲累计总分。</summary>
    private int _totalAccumulatedScore;
    /// <summary>当前歌曲的实际时长（秒）。有音乐时取音乐长度，无音乐时取谱面时长。</summary>
    private float _songDuration;

    private readonly List<RhythmCatchBeatNote> _runtimeNotes = new List<RhythmCatchBeatNote>();

    private RhythmCatchGameState _state = RhythmCatchGameState.Ready;
    private RhythmCatchSectionType _currentSection = RhythmCatchSectionType.Intro;
    private double _songStartDspTime;
    private double _magnetEndDspTime;
    private int _nextNoteIndex;
    private int _lastBeatIndex = -1;
    private int _score;
    private int _combo;
    private int _maxCombo;
    private int _health;
    private int _caughtRatedNotes;
    private int _totalRatedNotes;
    private bool _hasShield;
    private bool _isLoadingMusic;
    private float _nextHudRefreshTime;

    public RhythmCatchGameState State => _state;
    public RhythmCatchBeatMap BeatMap => beatMap;
    public float GoodWindowSeconds => goodWindowSeconds;
    public bool IsMagnetActive => AudioSettings.dspTime < _magnetEndDspTime;
    /// <summary>当前歌曲名（多曲模式显示）。</summary>
    public string CurrentSongName { get; private set; }
    /// <summary>当前歌曲序号（0-based）。</summary>
    public int CurrentSongIndex => _currentSongIndex;
    /// <summary>歌曲总数。</summary>
    public int TotalSongs => songList != null ? songList.Count : 0;
    /// <summary>跨歌曲累计总分。</summary>
    public int TotalAccumulatedScore => _totalAccumulatedScore;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void Start()
    {
        if (autoStart)
            StartLevel();
        else
            BroadcastHud();
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    private void Update()
    {
        double dspTime = AudioSettings.dspTime;

        // 歌曲间奏倒计时：等待结束后自动切到下一首。
        if (_state == RhythmCatchGameState.Finished && dspTime >= _interludeEndDspTime)
        {
            AdvanceToNextSong();
            return;
        }

        if (_state != RhythmCatchGameState.Countdown && _state != RhythmCatchGameState.Playing)
            return;

        // 倒计时期间不生成任何掉落物，等音乐正式开始后再统一生成。
        if (dspTime >= _songStartDspTime)
        {
            if (_state == RhythmCatchGameState.Countdown)
            {
                SetState(RhythmCatchGameState.Playing);
                // 倒计时结束，一次性补生成所有应在倒计时期间生成的音符。
                // 位置由各音符自身的 dspTime 插值计算，不会错位。
                SpawnDueNotes(dspTime);
            }

            // 游戏进行中持续生成后续音符。
            SpawnDueNotes(dspTime);

            float songBeat = (float)((dspTime - _songStartDspTime) / beatMap.SecondsPerBeat);
            ProcessBeatPulse(songBeat);
            ProcessSection(songBeat);

            // 所有音符已生成、场上没有残留物、且歌曲时长已到 → 结算。
            float elapsedSeconds = (float)(dspTime - _songStartDspTime);
            if (_nextNoteIndex >= _runtimeNotes.Count && spawner.ActiveCount == 0 && elapsedSeconds >= _songDuration)
                FinishLevel();
        }

        // HUD 时间每 0.1 秒刷新即可，避免每帧通过 EventCenter 广播完整快照。
        if (Time.unscaledTime >= _nextHudRefreshTime)
        {
            _nextHudRefreshTime = Time.unscaledTime + 0.1f;
            BroadcastHud();
        }
    }

    /// <summary>
    /// 开始关卡。多歌曲模式从第一首开始；单曲模式使用 Inspector 中配置的 beatMap。
    /// </summary>
    public void StartLevel()
    {
        // 开局时将本地配置的音量同步到框架 MusicMgr 的全局音效音量。
        MusicMgr.Instance.ChangeSoundValue(sfxVolume);

        // 多歌曲列表模式：从第一首开始顺序挑战。
        if (songList != null && songList.Count > 0)
        {
            _totalAccumulatedScore = 0;
            LoadCurrentSong();
            return;
        }

        // 单曲兼容模式。
        if (beatMap == null || player == null || spawner == null)
        {
            LogSystem.Error("节奏接箱无法开始：BeatMap、Player 或 Spawner 引用缺失。");
            return;
        }

        if (_isLoadingMusic)
            return;

        CurrentSongName = beatMap.name;
        _currentSongIndex = 0;

        // 直接拖曳：立即可用，设完 clip 后继续走 BeginLevelInternal。
        TryUseDirectMusicClip();

        // AB 异步加载：如果发起了异步请求则 return，回调中会调用 BeginLevelInternal。
        if (TryLoadMusicFromAB(musicAssetBundleName, musicResourceName))
            return;

        BeginLevelInternal();
    }

    /// <summary>
    /// 如果 Inspector 中直接拖入了 AudioClip，直接赋给 musicSource。
    /// 直接拖曳是同步操作，不阻断后续流程（与 AB 异步不同）。
    /// </summary>
    private bool TryUseDirectMusicClip()
    {
        if (musicSource != null && directMusicClip != null)
        {
            musicSource.clip = directMusicClip;
            return true;
        }
        return false;
    }

    /// <summary>
    /// 尝试通过 AB 包异步加载音乐。返回 true 表示已发起异步加载。
    /// </summary>
    private bool TryLoadMusicFromAB(string abName, string resName)
    {
        if (musicSource == null || musicSource.clip != null)
            return false;

        if (string.IsNullOrWhiteSpace(abName) || string.IsNullOrWhiteSpace(resName))
            return false;

        _isLoadingMusic = true;
        ABResMgr.Instance.LoadResAsync<AudioClip>(abName, resName, clip =>
        {
            _isLoadingMusic = false;
            if (this == null) return;
            if (clip == null)
                LogSystem.Warning("第一章节奏接箱音乐加载失败，将继续使用静默白盒节拍。");
            else
                musicSource.clip = clip;
            BeginLevelInternal();
        });
        return true;
    }

    /// <summary>
    /// 从歌单加载当前序号对应的歌曲配置到 Inspector 字段中。
    /// </summary>
    private void LoadCurrentSong()
    {
        if (songList == null || _currentSongIndex < 0 || _currentSongIndex >= songList.Count)
        {
            // 歌单播放完毕。
            SetState(RhythmCatchGameState.Finished);
            BroadcastHud();
            return;
        }

        RhythmCatchSongConfig song = songList.GetSong(_currentSongIndex);
        if (song == null || song.beatMap == null)
        {
            LogSystem.Error($"歌曲列表第 {_currentSongIndex} 首无效，跳过。");
            _currentSongIndex++;
            LoadCurrentSong();
            return;
        }

        // 将歌曲配置写入 Inspector 字段，后续逻辑不变。
        beatMap = song.beatMap;
        musicAssetBundleName = song.musicAssetBundleName;
        musicResourceName = song.musicResourceName;
        directMusicClip = song.directMusicClip;
        CurrentSongName = song.songDisplayName;

        // 通知 UI 歌曲切换。
        EventCenter.Instance.EventTrigger<string>(RhythmCatchEventNames.SongSelected, song.songDisplayName);

        // 直接拖曳：立即可用，设完 clip 后继续走 BeginLevelInternal。
        TryUseDirectMusicClip();

        // AB 异步加载：如果发起了异步请求则 return，回调中会调用 BeginLevelInternal。
        if (TryLoadMusicFromAB(song.musicAssetBundleName, song.musicResourceName))
            return;

        BeginLevelInternal();
    }

    /// <summary>
    /// 间奏结束后自动切到下一首。
    /// </summary>
    private void AdvanceToNextSong()
    {
        if (songList != null && _currentSongIndex + 1 < songList.Count)
        {
            _currentSongIndex++;
            LoadCurrentSong();
        }
        else
        {
            // 全部歌曲完成，弹出关卡切换面板。
            player.SetControlEnabled(false);
            SetState(RhythmCatchGameState.Finished);
            _interludeEndDspTime = double.MaxValue;
            BroadcastHud();
            ShowLevelSwitchPanel();
        }
    }

    /// <summary>
    /// 掉落物到达判定线并覆盖玩家时调用。
    /// </summary>
    public void ResolveCatch(RhythmCatchFallingItem item, float timingErrorSeconds)
    {
        if (item == null || (_state != RhythmCatchGameState.Playing && _state != RhythmCatchGameState.Countdown))
            return;

        if (item.ItemType == RhythmCatchItemType.Bomb)
        {
            ResolveBombHit(item);
            return;
        }

        // 根据掉落物类型播放对应的接取音效。
        PlayCatchSound(item.ItemType);

        RhythmCatchJudgement judgement = GetJudgement(timingErrorSeconds);
        _combo++;
        _maxCombo = Mathf.Max(_maxCombo, _combo);

        float judgementMultiplier = GetJudgementMultiplier(judgement);
        float comboMultiplier = GetComboMultiplier(_combo);
        _score += Mathf.RoundToInt(GetBaseScore(item.ItemType) * judgementMultiplier * comboMultiplier);

        // 宝石只积累 Combo，不进入策划案评级所使用的"接取率"。
        if (item.ItemType != RhythmCatchItemType.Gem)
            _caughtRatedNotes++;

        if (item.ItemType == RhythmCatchItemType.Magnet)
        {
            // 已有磁铁 buff 时，再次接到只延长 1 秒；否则获得完整持续时间。
            if (IsMagnetActive)
                _magnetEndDspTime += 1d;
            else
                _magnetEndDspTime = AudioSettings.dspTime + magnetDurationSeconds;
        }
        else if (item.ItemType == RhythmCatchItemType.Shield)
            _hasShield = true;

        spawner.Despawn(item);
        BroadcastJudgement(judgement);
        BroadcastHud();
    }

    /// <summary>
    /// 安全物体超过 GOOD 窗口仍未接到时，Combo 立即归零。
    /// </summary>
    public void ResolveMiss(RhythmCatchFallingItem item)
    {
        if (item == null)
            return;

        _combo = 0;
        PlayMissSound();
        spawner.Despawn(item);
        BroadcastJudgement(RhythmCatchJudgement.Miss);
        BroadcastHud();
    }

    /// <summary>
    /// 炸弹未被玩家覆盖代表成功躲避，只回收物体，不打断 Combo。
    /// </summary>
    public void ResolveBombAvoided(RhythmCatchFallingItem item)
    {
        if (item != null)
            spawner.Despawn(item);
    }

    // ──────────────────────────────────────────────
    // 关卡切换面板
    // ──────────────────────────────────────────────

    /// <summary>
    /// 当前关卡（歌曲列表）全部播放完毕后，弹出通用关卡切换面板。
    /// 面板通过 UIMgr 从 AB 包加载，加载后自动设置下一关场景名称。
    /// 预制体路径：Assets/资源统一放置点/UI/UI面板/关卡切换/LevelSwitchPanel.prefab
    /// AB 包名：ui_panel_load（过场景UI面板包）
    /// </summary>
    private void ShowLevelSwitchPanel()
    {
        // 从 AB 包异步加载关卡切换面板，面板加载完成后自动显示。
        // 如果 AB 包尚未包含此预制体，UIMgr 会打印警告，不影响正常游戏逻辑。
        UIMgr.Instance.ShowPanel<LevelSwitchPanel>(MyAssetBundleName.过场景UI面板包, (panel) =>
        {
            if (panel != null)
            {
                // 将 Inspector 中配置的下一关场景名称传递给面板。
                if (!string.IsNullOrWhiteSpace(nextLevelSceneName))
                    panel.SetNextLevelSceneName(nextLevelSceneName);
            }
        }, E_UILayer.Top);
    }

    /// <summary>
    /// 供白盒场景生成器注入场景引用。
    /// </summary>
    public void Configure(
        RhythmCatchBeatMap map,
        RhythmCatchPlayerController playerController,
        RhythmCatchSpawner itemSpawner,
        AudioSource source)
    {
        beatMap = map;
        player = playerController;
        spawner = itemSpawner;
        musicSource = source;
    }

    private void BeginLevelInternal()
    {
        spawner.DespawnAll();
        MusicMgr.Instance.StopBKMusic();

        if (musicSource != null)
            musicSource.Stop();

        _runtimeNotes.Clear();
        if (beatMap.notes != null)
            _runtimeNotes.AddRange(beatMap.notes);
        _runtimeNotes.Sort((left, right) => left.beat.CompareTo(right.beat));

        _nextNoteIndex = 0;
        _lastBeatIndex = -1;
        _score = 0;
        _combo = 0;
        _maxCombo = 0;
        _health = maxHealth;
        _caughtRatedNotes = 0;
        _totalRatedNotes = beatMap.CountRatedNotes();
        _hasShield = false;
        _magnetEndDspTime = 0d;
        _currentSection = RhythmCatchSectionType.Intro;

        // 游戏时长 = 音乐实际长度（优先）或谱面计算时长（无音乐时回退）。
        _songDuration = (musicSource != null && musicSource.clip != null)
            ? musicSource.clip.length
            : (beatMap != null ? beatMap.DurationSeconds : 60f);

        // 给倒计时和 AudioSource.PlayScheduled 使用同一个 DSP 起点，保证画面与音乐共享时间基准。
        _songStartDspTime = AudioSettings.dspTime + countdownSeconds;
        if (musicSource != null && musicSource.clip != null)
        {
            musicSource.loop = false;
            musicSource.time = 0f;
            musicSource.PlayScheduled(_songStartDspTime);
        }

        player.SetControlEnabled(true);
        SetState(RhythmCatchGameState.Countdown);
        BroadcastHud();
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="dspTime"></param>
    private void SpawnDueNotes(double dspTime)
    {
        while (_nextNoteIndex < _runtimeNotes.Count)
        {
            RhythmCatchBeatNote note = _runtimeNotes[_nextNoteIndex];
            double hitDspTime = _songStartDspTime + note.beat * beatMap.SecondsPerBeat;
            double travelDuration = beatMap.travelBeats * beatMap.SecondsPerBeat / Math.Max(0.1f, note.fallSpeedMultiplier);
            double spawnDspTime = hitDspTime - travelDuration;

            if (dspTime + 0.005d < spawnDspTime)
                break;

            spawner.Spawn(note, hitDspTime, travelDuration, player, this);
            _nextNoteIndex++;
        }
    }

    private void ProcessBeatPulse(float songBeat)
    {
        int beatIndex = Mathf.FloorToInt(songBeat);
        if (beatIndex < 0 || beatIndex == _lastBeatIndex)
            return;

        _lastBeatIndex = beatIndex;
        int beatInBar = beatIndex % Mathf.Max(1, beatMap.beatsPerBar);
        bool isStrongBeat = beatInBar == 0 || beatInBar == 2;
        EventCenter.Instance.EventTrigger<int, bool>(RhythmCatchEventNames.BeatTriggered, beatIndex, isStrongBeat);
    }

    private void ProcessSection(float songBeat)
    {
        RhythmCatchSectionType section = RhythmCatchSectionType.Intro;
        if (beatMap.sections != null)
        {
            foreach (RhythmCatchSectionMark mark in beatMap.sections)
            {
                if (songBeat < mark.startBeat)
                    break;
                section = mark.section;
            }
        }

        if (section == _currentSection)
            return;

        _currentSection = section;
        BroadcastHud();
    }


    // ──────────────────────────────────────────────
    // 状态 & 判定助手
    // ──────────────────────────────────────────────

    private void SetState(RhythmCatchGameState newState)
    {
        _state = newState;
    }

    private void FinishLevel()
    {
        player.SetControlEnabled(false);
        SetState(RhythmCatchGameState.Finished);
        BroadcastHud();
    }

    private RhythmCatchJudgement GetJudgement(float errorSeconds)
    {
        if (errorSeconds <= perfectWindowSeconds) return RhythmCatchJudgement.Perfect;
        if (errorSeconds <= greatWindowSeconds) return RhythmCatchJudgement.Great;
        return RhythmCatchJudgement.Good;
    }

    private float GetJudgementMultiplier(RhythmCatchJudgement judgement)
    {
        switch (judgement)
        {
            case RhythmCatchJudgement.Perfect: return 1.2f;
            case RhythmCatchJudgement.Great: return 1.0f;
            case RhythmCatchJudgement.Good: return 0.6f;
            default: return 0f;
        }
    }

    private float GetComboMultiplier(int combo)
    {
        if (combo >= 50) return 2.0f;
        if (combo >= 20) return 1.5f;
        if (combo >= 10) return 1.2f;
        return 1.0f;
    }

    private int GetBaseScore(RhythmCatchItemType itemType)
    {
        switch (itemType)
        {
            case RhythmCatchItemType.Gold: return Mathf.RoundToInt(normalBaseScore * 2f);
            case RhythmCatchItemType.Gem: return 0;
            default: return normalBaseScore;
        }
    }

    /// <summary>
    /// 向 EventCenter 发送 HUD 快照，驱动 UI 更新。
    /// </summary>
    private void BroadcastHud()
    {
        double dspTime = AudioSettings.dspTime;
        float elapsedSeconds = _songStartDspTime > 0 ? (float)(dspTime - _songStartDspTime) : 0f;
        float magnetRemaining = IsMagnetActive ? (float)(_magnetEndDspTime - dspTime) : 0f;
        int countdownRemaining = _state == RhythmCatchGameState.Countdown
            ? Mathf.Max(0, Mathf.CeilToInt((float)(_songStartDspTime - dspTime)))
            : 0;

        EventCenter.Instance.EventTrigger(RhythmCatchEventNames.HudChanged, new RhythmCatchHudSnapshot
        {
            score = _score + _totalAccumulatedScore,
            combo = _combo,
            maxCombo = _maxCombo,
            health = _health,
            maxHealth = maxHealth,
            elapsedSeconds = Mathf.Max(0f, elapsedSeconds),
            durationSeconds = _songDuration,
            state = _state,
            countdown = countdownRemaining,
            hasShield = _hasShield,
            magnetRemaining = magnetRemaining,
            grade = GetGrade(),
            catchRate = _totalRatedNotes > 0 ? (float)_caughtRatedNotes / _totalRatedNotes : 1f,
        });
    }

    private void BroadcastJudgement(RhythmCatchJudgement judgement)
    {
        EventCenter.Instance.EventTrigger(RhythmCatchEventNames.JudgementChanged, judgement);
    }

    private string GetGrade()
    {
        float rate = _totalRatedNotes > 0 ? (float)_caughtRatedNotes / _totalRatedNotes : 1f;
        if (rate >= 0.95f) return "S";
        if (rate >= 0.85f) return "A";
        if (rate >= 0.70f) return "B";
        if (rate >= 0.50f) return "C";
        return "D";
    }

    // ──────────────────────────────────────────────
    // 音效播放（使用框架 MusicMgr）
    // ──────────────────────────────────────────────

    /// <summary>
    /// 接到炸弹：清空 Combo，消耗护盾（如有）。
    /// 音效通过框架 MusicMgr 统一播放，音量由 MusicMgr 的 soundValue 全局控制。
    /// </summary>
    private void ResolveBombHit(RhythmCatchFallingItem item)
    {
        // 播放炸弹音效：优先用直接拖曳的 clip，否则从 AB 加载。
        PlayCatchClip(sfxBombHit, sfxBundleName, sfxBombHitName);

        _combo = 0;
        RhythmCatchJudgement judgement;

        if (_hasShield)
        {
            _hasShield = false;
            judgement = RhythmCatchJudgement.Shielded;
        }
        else
        {
            judgement = RhythmCatchJudgement.Bomb;
        }

        spawner.Despawn(item);
        BroadcastJudgement(judgement);
        BroadcastHud();
    }

    /// <summary>
    /// 根据掉落物类型播放对应的接取音效。
    /// 所有音效均通过框架 MusicMgr 统一播放。
    /// </summary>
    private void PlayCatchSound(RhythmCatchItemType itemType)
    {
        switch (itemType)
        {
            case RhythmCatchItemType.Normal:
                PlayCatchClip(sfxNormal, sfxBundleName, sfxNormalName);
                break;
            case RhythmCatchItemType.Gold:
                PlayCatchClip(sfxGold, sfxBundleName, sfxGoldName);
                break;
            case RhythmCatchItemType.Gem:
                PlayCatchClip(sfxGem, sfxBundleName, sfxGemName);
                break;
            case RhythmCatchItemType.Magnet:
                PlayCatchClip(sfxMagnet, sfxBundleName, sfxMagnetName);
                break;
            case RhythmCatchItemType.Shield:
                PlayCatchClip(sfxShield, sfxBundleName, sfxShieldName);
                break;
        }
    }

    private void PlayMissSound()
    {
        PlayCatchClip(sfxMiss, sfxBundleName, sfxMissName);
    }

    /// <summary>
    /// 通过框架 MusicMgr 播放音效。
    /// 优先级：直接拖曳的 AudioClip > AB 包异步加载。
    /// MusicMgr 统一管理音量、对象池和场景切换安全。
    /// </summary>
    private void PlayCatchClip(AudioClip directClip, string bundleName, string soundName)
    {
        if (directClip != null)
        {
            // 直接拖曳的 clip：使用 MusicMgr 的 PlaySound(AudioClip) 重载。
            MusicMgr.Instance.PlaySound(directClip);
            return;
        }

        if (string.IsNullOrWhiteSpace(soundName) || string.IsNullOrWhiteSpace(bundleName))
            return;

        // AB 包加载：使用 MusicMgr 的 PlaySound(abName, soundName) 重载。
        MusicMgr.Instance.PlaySound(bundleName, soundName);
    }
}
