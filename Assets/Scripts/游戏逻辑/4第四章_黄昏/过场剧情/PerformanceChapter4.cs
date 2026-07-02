using System.Collections.Generic;
using TangmenFramework;
using UnityEngine;

public class PerformanceChapter4 : MonoBehaviour
{
    [Header("========== 引用 ==========")]
    [SerializeField] private PerformanceDirector director;
    [SerializeField] private PerformanceSceneData sceneData;

    [Header("========== 角色预制体 ==========")]
    [Tooltip("角色根节点")]
    [SerializeField] private Transform characterRoot;
    
    [Tooltip("爱坤各姿态预制体")]
    [SerializeField] private GameObject ikun_Float;
    [SerializeField] private GameObject ikun_Fall;
    [SerializeField] private GameObject ikun_Lie;
    [SerializeField] private GameObject ikun_Sit;
    [SerializeField] private GameObject ikun_Stand;

    [Header("========== 背景预制体(带Shader) ==========")]
    [Tooltip("背景根节点")]
    [SerializeField] private Transform backgroundRoot;
    
    [Tooltip("深海背景预制体(带水波扭曲Shader)")]
    [SerializeField] private GameObject bg_DeepSea;
    [Tooltip("暴风背景预制体(带闪电Shader)")]
    [SerializeField] private GameObject bg_Storm;
    [Tooltip("黎明背景预制体(带光晕Shader)")]
    [SerializeField] private GameObject bg_Dawn;

    private static readonly Color DeepSeaDark   = new Color(0.024f, 0.059f, 0.118f, 0.4f);
    private static readonly Color DeepSeaMid    = new Color(0.039f, 0.086f, 0.176f, 0.35f);
    private static readonly Color StormDark     = new Color(0.102f, 0.082f, 0.125f, 0.45f);
    private static readonly Color BurstRed      = new Color(0.878f, 0.333f, 0.416f, 0.25f);
    private static readonly Color DawnGold      = new Color(0.831f, 0.659f, 0.392f, 0.2f);
    private static readonly Color DawnWarm      = new Color(0.941f, 0.847f, 0.565f, 0.15f);
    private static readonly Color ClearTrans    = new Color(0, 0, 0, 0);

    void Start()
    {
        if (director == null)
            director = GetComponent<PerformanceDirector>();
        if (director == null)
        {
            LogSystem.Error("PerformanceChapter4: PerformanceDirector 未找到！");
            return;
        }

        RegisterCharacters();
        RegisterBackgrounds();

        var data = ConfigureSceneData();
        director.PlayScene(data);
    }

    private void RegisterCharacters()
    {
        if (characterRoot == null) return;

        GameObject[] prefabs = { ikun_Float, ikun_Fall, ikun_Lie, ikun_Sit, ikun_Stand };
        string[] names = { "爱坤_水下漂浮", "爱坤_自由坠落", "爱坤_躺姿", "爱坤_坐姿", "爱坤_站姿" };

        for (int i = 0; i < prefabs.Length; i++)
        {
            if (prefabs[i] != null)
            {
                GameObject instance = Instantiate(prefabs[i], characterRoot);
                instance.name = names[i];
                instance.SetActive(false);
                
                var sr = instance.GetComponent<SpriteRenderer>();
                if (sr != null)
                {
                    sr.sortingOrder = 10;
                }
            }
        }
    }

    private void RegisterBackgrounds()
    {
        if (backgroundRoot == null) return;

        GameObject[] prefabs = { bg_DeepSea, bg_Storm, bg_Dawn };
        string[] names = { "背景_深海", "背景_暴风", "背景_黎明" };

        for (int i = 0; i < prefabs.Length; i++)
        {
            if (prefabs[i] != null)
            {
                GameObject instance = Instantiate(prefabs[i], backgroundRoot);
                instance.name = names[i];
                instance.SetActive(i == 0); // 只有深海背景默认显示
                
                var sr = instance.GetComponent<SpriteRenderer>();
                if (sr != null)
                {
                    sr.sortingOrder = 0;
                }
            }
        }
    }

    private PerformanceSceneData ConfigureSceneData()
    {
        var data = ScriptableObject.CreateInstance<PerformanceSceneData>();
        data.sceneName = "第四章_爱坤大冒险_WashedAway";
        data.chapterId = 4;
        data.moduleId = 3;
        data.syncMode = PerformanceSyncMode.BGMDriven;
        data.performanceBGMResName = "Washed Away";
        data.performanceBGMABName = "music_four";
        data.performanceBGMVolume = 0.85f;
        data.performanceDuration = 198f;

        data.commands = BuildAllCommands();
        return data;
    }

    private List<PerformanceCommand> BuildAllCommands()
    {
        List<PerformanceCommand> cmds = new List<PerformanceCommand>();

        // ACT I · 沉溺与坠落 (0:00 — 1:10)
        cmds.Add(Cmd(EPerformanceCommandType.ScreenFade,   t:0,   screenFadeAlpha:0f, duration:2f));
        cmds.Add(Cmd(EPerformanceCommandType.CharacterVisibility, t:0, characterName:"爱坤_水下漂浮", characterVisible:true));
        cmds.Add(Cmd(EPerformanceCommandType.ScreenColorFilter, t:1f, screenFilterColor:DeepSeaDark, duration:1f));
        cmds.Add(Cmd(EPerformanceCommandType.ScreenBlur,  t:1f,   blurStrength:1.2f, duration:1f));
        cmds.Add(Cmd(EPerformanceCommandType.Dialogue,     t:6f,  speakerName:"", textContent:"Sinking... I'm calling out your name", duration:5f));
        cmds.Add(Cmd(EPerformanceCommandType.CameraPan,    t:8f,   cameraPanOffset:new Vector3(0, -0.5f, 0), duration:4f));

        // Shot 02: 梦的碎片·闪回 (0:12 — 0:25)
        cmds.Add(Cmd(EPerformanceCommandType.ScreenColorFilter, t:12f, screenFilterColor:DeepSeaMid, duration:1f));
        cmds.Add(Cmd(EPerformanceCommandType.CameraShake,   t:23.5f, cameraShakeStrength:0.3f, cameraShakeVibrato:25, duration:0.8f));
        cmds.Add(Cmd(EPerformanceCommandType.Flash,         t:23.5f, flashColor:Color.white, duration:0.4f));

        // Shot 03: 假面·迎合的代价 (0:25 — 0:48)
        cmds.Add(Cmd(EPerformanceCommandType.ScreenColorFilter, t:25f, screenFilterColor:DeepSeaDark, duration:2f));
        cmds.Add(Cmd(EPerformanceCommandType.Dialogue, t:26f, speakerName:"", textContent:"If I show you what you wanna see", duration:5f));
        cmds.Add(Cmd(EPerformanceCommandType.CharacterShake, t:30f, characterName:"爱坤_水下漂浮", shakeIntensity:2f, duration:0.3f));
        cmds.Add(Cmd(EPerformanceCommandType.CameraZoom,    t:35f,  cameraZoomTarget:3.5f, duration:3f));
        cmds.Add(Cmd(EPerformanceCommandType.CharacterShake, t:38f, characterName:"爱坤_水下漂浮", shakeIntensity:3f, duration:0.3f));
        cmds.Add(Cmd(EPerformanceCommandType.Dialogue, t:40f, speakerName:"", textContent:"Will you tell me I fit in with no melody", duration:6f));
        cmds.Add(Cmd(EPerformanceCommandType.CameraShake, t:46f, cameraShakeStrength:0.5f, cameraShakeVibrato:30, duration:0.8f));
        cmds.Add(Cmd(EPerformanceCommandType.Flash,       t:46f, flashColor:Color.white, duration:0.3f));

        // Shot 04: 放弃挣扎·第一次溺水宣言 (0:48 — 1:10)
        cmds.Add(Cmd(EPerformanceCommandType.ScreenColorFilter, t:48f, screenFilterColor:new Color(0.01f, 0.03f, 0.06f, 0.6f), duration:3f));
        cmds.Add(Cmd(EPerformanceCommandType.ScreenFade,   t:50f, screenFadeAlpha:0.3f, duration:2f));
        cmds.Add(Cmd(EPerformanceCommandType.Dialogue,     t:52f, speakerName:"", textContent:"I'm drowning...", duration:4f));
        cmds.Add(Cmd(EPerformanceCommandType.Dialogue,     t:58f, speakerName:"", textContent:"Oh, I'm drowning...", duration:4f));
        cmds.Add(Cmd(EPerformanceCommandType.CameraZoom,   t:60f, cameraZoomTarget:8f, duration:10f));
        cmds.Add(Cmd(EPerformanceCommandType.ScreenFade,   t:62f, screenFadeAlpha:0.7f, duration:5f));
        cmds.Add(Cmd(EPerformanceCommandType.Dialogue,     t:64f, speakerName:"", textContent:"Wash me out to sea...", duration:5f));
        cmds.Add(Cmd(EPerformanceCommandType.ScreenFade,   t:68f, screenFadeAlpha:1f, duration:2f));

        // ⚡ 关键转折: 鼓点劈开海面 (1:10)
        cmds.Add(Cmd(EPerformanceCommandType.Flash,         t:70f, flashColor:Color.white, duration:0.05f));
        cmds.Add(Cmd(EPerformanceCommandType.CameraShake,   t:70f, cameraShakeStrength:1f, cameraShakeVibrato:40, duration:0.4f));
        cmds.Add(Cmd(EPerformanceCommandType.BackgroundChange, t:70.2f, backgroundSpriteName:"背景_暴风", duration:0.5f));
        cmds.Add(Cmd(EPerformanceCommandType.ScreenColorFilter, t:70.3f, screenFilterColor:StormDark, duration:0.5f));

        // ACT II · 坠落与爆发 (1:10 — 2:20)
        // Shot 05: 撕裂天空·自由坠落 (1:10 — 1:25)
        cmds.Add(Cmd(EPerformanceCommandType.CharacterVisibility, t:70.5f, characterName:"爱坤_水下漂浮", characterVisible:false));
        cmds.Add(Cmd(EPerformanceCommandType.CharacterVisibility, t:71f, characterName:"爱坤_自由坠落", characterVisible:true));
        cmds.Add(Cmd(EPerformanceCommandType.CameraShake,   t:71f,  cameraShakeStrength:0.5f, cameraShakeVibrato:15, duration:14f));
        cmds.Add(Cmd(EPerformanceCommandType.Dialogue,      t:72f,  speakerName:"", textContent:"Falling...", duration:3f));
        cmds.Add(Cmd(EPerformanceCommandType.CameraZoom,    t:73f,  cameraZoomTarget:3.8f, duration:1f));
        cmds.Add(Cmd(EPerformanceCommandType.Flash,         t:74f,  flashColor:new Color(0.8f, 0.85f, 1f, 1f), duration:0.15f));
        cmds.Add(Cmd(EPerformanceCommandType.Dialogue,      t:76f,  speakerName:"", textContent:"Free falling through the sky", duration:4f));
        cmds.Add(Cmd(EPerformanceCommandType.CameraRotate,  t:78f,  cameraRotateAngle:15f, duration:3f));
        cmds.Add(Cmd(EPerformanceCommandType.Flash,         t:80f,  flashColor:new Color(0.8f, 0.85f, 1f, 1f), duration:0.15f));
        cmds.Add(Cmd(EPerformanceCommandType.Flash,         t:83f,  flashColor:new Color(0.8f, 0.85f, 1f, 1f), duration:0.15f));

        // Shot 06: 声音之海·吞噬 (1:25 — 1:48)
        cmds.Add(Cmd(EPerformanceCommandType.CameraReset,   t:85f));
        cmds.Add(Cmd(EPerformanceCommandType.Dialogue,      t:86f,  speakerName:"", textContent:"Your voice consumes me", duration:5f));
        cmds.Add(Cmd(EPerformanceCommandType.CameraRotate,  t:88f,  cameraRotateAngle:30f, duration:8f));
        cmds.Add(Cmd(EPerformanceCommandType.CharacterShake, t:96f, characterName:"爱坤_自由坠落", shakeIntensity:4f, duration:1f));
        cmds.Add(Cmd(EPerformanceCommandType.Dialogue,      t:98f,  speakerName:"", textContent:"There's nowhere left to hide", duration:5f));
        cmds.Add(Cmd(EPerformanceCommandType.ScreenFade,    t:102f, screenFadeAlpha:0.4f, duration:2f));
        cmds.Add(Cmd(EPerformanceCommandType.CameraZoom,    t:105f, cameraZoomTarget:8f, duration:3f));
        cmds.Add(Cmd(EPerformanceCommandType.Flash,         t:106f, flashColor:Color.white, duration:0.2f));

        // Shot 07: 破晓·第二次溺水宣言(爆发版) (1:48 — 2:20)
        cmds.Add(Cmd(EPerformanceCommandType.CameraReset,   t:108f));
        cmds.Add(Cmd(EPerformanceCommandType.CameraZoom,    t:108.5f, cameraZoomTarget:3f, duration:0.3f));
        cmds.Add(Cmd(EPerformanceCommandType.ScreenColorFilter, t:109f, screenFilterColor:BurstRed, duration:1f));
        cmds.Add(Cmd(EPerformanceCommandType.Flash,         t:109f, flashColor:new Color(1f, 0.85f, 0.4f, 1f), duration:0.3f));
        cmds.Add(Cmd(EPerformanceCommandType.Dialogue,      t:110f, speakerName:"", textContent:"I'm drowning!!", duration:4f));
        cmds.Add(Cmd(EPerformanceCommandType.ScreenColorFilter, t:112f, screenFilterColor:DawnGold, duration:3f));
        cmds.Add(Cmd(EPerformanceCommandType.CameraZoom,    t:115f, cameraZoomTarget:10f, duration:8f));
        cmds.Add(Cmd(EPerformanceCommandType.Dialogue,      t:118f, speakerName:"", textContent:"Oh, I'm drowning...", duration:5f));
        cmds.Add(Cmd(EPerformanceCommandType.Flash,         t:120f, flashColor:new Color(1f, 0.9f, 0.5f, 1f), duration:0.2f));
        cmds.Add(Cmd(EPerformanceCommandType.Dialogue,      t:124f, speakerName:"", textContent:"Wash me out to sea!!", duration:6f));
        cmds.Add(Cmd(EPerformanceCommandType.ScreenColorFilter, t:130f, screenFilterColor:DawnWarm, duration:5f));
        cmds.Add(Cmd(EPerformanceCommandType.Flash,         t:139f, flashColor:new Color(1f, 0.95f, 0.6f, 1f), duration:0.5f));

        // ACT III · 留白与重生 (2:20 — 3:18)
        // Shot 08: 旷野黎明·无词吟唱 (2:20 — 2:50)
        cmds.Add(Cmd(EPerformanceCommandType.CameraReset,   t:140f));
        cmds.Add(Cmd(EPerformanceCommandType.ScreenFade,    t:140f, screenFadeAlpha:1f, duration:0.1f));
        cmds.Add(Cmd(EPerformanceCommandType.BackgroundChange, t:140.2f, backgroundSpriteName:"背景_黎明", duration:2f));
        cmds.Add(Cmd(EPerformanceCommandType.ScreenFade,    t:140.5f, screenFadeAlpha:0f, duration:2f));
        cmds.Add(Cmd(EPerformanceCommandType.ScreenColorFilter, t:141f, screenFilterColor:DawnWarm, duration:3f));
        cmds.Add(Cmd(EPerformanceCommandType.CharacterVisibility, t:141.5f, characterName:"爱坤_自由坠落", characterVisible:false));
        cmds.Add(Cmd(EPerformanceCommandType.CharacterVisibility, t:142f, characterName:"爱坤_躺姿", characterVisible:true));
        cmds.Add(Cmd(EPerformanceCommandType.CameraZoom,    t:148f, cameraZoomTarget:9f, duration:20f));
        cmds.Add(Cmd(EPerformanceCommandType.Dialogue,      t:145f, speakerName:"", textContent:"Ohh...", duration:5f));
        cmds.Add(Cmd(EPerformanceCommandType.Dialogue,      t:152f, speakerName:"", textContent:"Oooh...", duration:8f));

        // Shot 09: 站起·第三次溺水宣言(呢喃版) (2:50 — 3:10)
        cmds.Add(Cmd(EPerformanceCommandType.CameraReset,   t:170f));
        cmds.Add(Cmd(EPerformanceCommandType.CharacterVisibility, t:170.2f, characterName:"爱坤_躺姿", characterVisible:false));
        cmds.Add(Cmd(EPerformanceCommandType.CharacterVisibility, t:170.5f, characterName:"爱坤_坐姿", characterVisible:true));
        cmds.Add(Cmd(EPerformanceCommandType.Dialogue,      t:171f, speakerName:"", textContent:"I'm drowning...", duration:4f));
        cmds.Add(Cmd(EPerformanceCommandType.CharacterVisibility, t:174.5f, characterName:"爱坤_坐姿", characterVisible:false));
        cmds.Add(Cmd(EPerformanceCommandType.CharacterVisibility, t:175f, characterName:"爱坤_站姿", characterVisible:true));
        cmds.Add(Cmd(EPerformanceCommandType.Dialogue,      t:177f, speakerName:"", textContent:"I'm drowning...", duration:4f));
        cmds.Add(Cmd(EPerformanceCommandType.CameraPan,     t:182f, cameraPanOffset:new Vector3(0, 0.3f, 0), duration:3f));
        cmds.Add(Cmd(EPerformanceCommandType.Dialogue,      t:185f, speakerName:"", textContent:"I'm drowning...", duration:4f));
        cmds.Add(Cmd(EPerformanceCommandType.Dialogue,      t:189f, speakerName:"", textContent:"Wash me out to sea", duration:4f));

        // Shot 10: 远方·终幕 (3:10 — 3:18 + 留白)
        cmds.Add(Cmd(EPerformanceCommandType.CameraZoom,    t:192f, cameraZoomTarget:11f, duration:8f));
        cmds.Add(Cmd(EPerformanceCommandType.ScreenColorFilter, t:193f, screenFilterColor:DawnGold, duration:3f));
        cmds.Add(Cmd(EPerformanceCommandType.Dialogue,      t:195f, speakerName:"", textContent:"— 爱坤大冒险 —", duration:8f));
        cmds.Add(Cmd(EPerformanceCommandType.ScreenColorFilter, t:198f, screenFilterColor:ClearTrans, duration:3f));
        cmds.Add(Cmd(EPerformanceCommandType.ScreenFade,    t:200f, screenFadeAlpha:1f, duration:3f));

        LogSystem.Info($"PerformanceChapter4: 命令构建完成，共 {cmds.Count} 条命令，总时长 198s");
        return cmds;
    }

    private static PerformanceCommand Cmd(EPerformanceCommandType type, float t = 0f,
        string characterName = null, bool characterVisible = true,
        Vector2 characterTargetPos = default,
        string backgroundSpriteName = null,
        string speakerName = null, string textContent = null,
        float cameraShakeStrength = 0f, int cameraShakeVibrato = 0,
        float cameraZoomTarget = 0f, Vector3 cameraPanOffset = default,
        float cameraRotateAngle = 0f,
        Color screenFilterColor = default, float screenFadeAlpha = -1f,
        float blurStrength = 0f,
        Color flashColor = default,
        float shakeIntensity = 0f,
        float duration = 0f, float delay = 0f)
    {
        return new PerformanceCommand
        {
            type = type,
            timestamp = t,
            duration = duration,
            delay = delay,
            characterName = characterName ?? "",
            characterVisible = characterVisible,
            characterTargetPos = characterTargetPos,
            backgroundSpriteName = backgroundSpriteName ?? "",
            speakerName = speakerName ?? "",
            textContent = textContent ?? "",
            cameraShakeStrength = cameraShakeStrength,
            cameraShakeVibrato = cameraShakeVibrato,
            cameraZoomTarget = cameraZoomTarget,
            cameraPanOffset = cameraPanOffset,
            cameraRotateAngle = cameraRotateAngle,
            screenFilterColor = screenFilterColor,
            screenFadeAlpha = screenFadeAlpha,
            blurStrength = blurStrength,
            flashColor = flashColor,
            shakeIntensity = shakeIntensity,
        };
    }
}
