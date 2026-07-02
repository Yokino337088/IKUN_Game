#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using Codely.Newtonsoft.Json;
using TJGenerators.AssetSearch;
using TJGenerators.Config;
using TJGenerators.Pipeline;
using TJGenerators.UI;
using TJGenerators.Utils;
using Unity.UniAsset.Manager.Editor.InternalBridge;

namespace TJGenerators
{
    /// <summary>
    /// 图片切割窗口：选择一张大图，使用传统 CV 方法自动检测并切割出其中的独立元素。
    /// </summary>
    public class TJGeneratorsImageSliceWindow : EditorWindow
    {
        // ========== 状态 ==========
        private Texture2D _sourceTexture;
        private string _sourceAssetPath;
        private Texture2D _previewTexture;
        private List<ImageSliceService.SliceRegion> _detectedRegions = new List<ImageSliceService.SliceRegion>();
        private Vector2 _scrollPosition;
        private bool _isProcessing;

        // ========== 参数 ==========
        private int _bgModeIndex = 0;
        private float _alphaThreshold = 0.1f;
        private float _colorTolerance = 15f;
        private int _minRegionPixels = 100;
        private int _padding = 2;
        private bool _setAsSprite = true;

        private const float MaxSourcePreviewHeight = 200f;
        private const string GuideUrl = "https://codely-docs.tuanjie.cn/ai-generation-tools/play-guide/#%E7%94%9F%E6%88%90-ui";

        private static readonly string[] BgModeNames = { "自动检测", "透明背景", "纯色背景" };
        private static readonly ImageSliceService.BackgroundMode[] BgModeValues =
        {
            ImageSliceService.BackgroundMode.Auto,
            ImageSliceService.BackgroundMode.Transparent,
            ImageSliceService.BackgroundMode.SolidColor
        };

        private static GUIStyle s_ActionButtonStyle;
        private static GUIContent s_HelpButtonContent;

        private static readonly Dictionary<string, TJGeneratorsImageSliceWindow> s_OpenWindows =
            new Dictionary<string, TJGeneratorsImageSliceWindow>();

        private static GUIStyle ActionButtonStyle
        {
            get
            {
                if (s_ActionButtonStyle == null)
                {
                    s_ActionButtonStyle = new GUIStyle(CommonStyles.GenerateButtonSolidStyle)
                    {
                        imagePosition = ImagePosition.ImageLeft,
                        fontSize = 14,
                        fontStyle = FontStyle.Bold,
                        alignment = TextAnchor.MiddleCenter
                    };
                    s_ActionButtonStyle.normal.textColor = Color.white;
                    s_ActionButtonStyle.hover.textColor = Color.white;
                    s_ActionButtonStyle.active.textColor = Color.white;
                }
                return s_ActionButtonStyle;
            }
        }

        // ========== 入口 ==========

        [MenuItem("AI/工具/图片切割", false, 3050)]
        public static void ShowWindow()
        {
            var window = GetWindow<TJGeneratorsImageSliceWindow>("图片切割");
            window.minSize = new Vector2(420, 600);
            window.Show();
        }

        public static void OpenForAsset(string assetPath)
        {
            if (string.IsNullOrEmpty(assetPath))
            {
                ShowWindow();
                return;
            }

            string guid = AssetDatabase.AssetPathToGUID(assetPath);
            if (!string.IsNullOrEmpty(guid) && s_OpenWindows.TryGetValue(guid, out var existing) && existing != null)
            {
                existing.Focus();
                return;
            }

            var window = CreateInstance<TJGeneratorsImageSliceWindow>();
            window.titleContent = new GUIContent($"图片切割 - {Path.GetFileNameWithoutExtension(assetPath)}");
            window.minSize = new Vector2(420, 600);
            window.SetSourceAsset(assetPath);
            window.Show();

            if (!string.IsNullOrEmpty(guid))
                s_OpenWindows[guid] = window;
        }

        // ========== 生命周期 ==========

        private void OnEnable()
        {
            _bgModeIndex = EditorPrefs.GetInt("TJGen_ImageSlice_BgMode", 0);
            _alphaThreshold = EditorPrefs.GetFloat("TJGen_ImageSlice_AlphaThreshold", 0.1f);
            _colorTolerance = EditorPrefs.GetFloat("TJGen_ImageSlice_ColorTolerance", 15f);
            _minRegionPixels = EditorPrefs.GetInt("TJGen_ImageSlice_MinRegion", 100);
            _padding = EditorPrefs.GetInt("TJGen_ImageSlice_Padding", 2);
            _setAsSprite = EditorPrefs.GetBool("TJGen_ImageSlice_SetAsSprite", true);
        }

        private void OnDestroy()
        {
            ClearPreview();
            foreach (var kvp in s_OpenWindows)
            {
                if (kvp.Value == this)
                {
                    s_OpenWindows.Remove(kvp.Key);
                    break;
                }
            }
        }

        /// <summary>
        /// 标题栏右上角「?」帮助图标，点击打开使用文档（与其他生成窗口一致）。
        /// </summary>
        private void ShowButton(Rect rect)
        {
            if (s_HelpButtonContent == null)
            {
                s_HelpButtonContent = EditorGUIUtility.IconContent("_Help");
                s_HelpButtonContent.tooltip = "怎么用？查看使用文档";
            }

            if (GUI.Button(rect, s_HelpButtonContent, GUIStyle.none))
                Application.OpenURL(GuideUrl);
        }

        // ========== GUI ==========

        private void OnGUI()
        {
            EditorGUI.DrawRect(new Rect(0, 0, position.width, position.height), CommonStyles.WindowBackgroundColor);

            float contentWidth = position.width - CommonStyles.LeftContentPadding * 2f;

            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);
            GUILayout.BeginHorizontal();
            GUILayout.Space(CommonStyles.LeftContentPadding);
            GUILayout.BeginVertical();

            GUILayout.Space(CommonStyles.Space2);
            DrawSourceImageSection(contentWidth);
            GUILayout.Space(CommonStyles.Space2);
            DrawGuideLink();
            GUILayout.Space(CommonStyles.Space2);
            DrawParameterSection(contentWidth);
            GUILayout.Space(CommonStyles.Space2);
            DrawPreviewSection(contentWidth);
            GUILayout.Space(CommonStyles.Space2);
            DrawActionSection(contentWidth);
            GUILayout.Space(CommonStyles.Space2);

            GUILayout.EndVertical();
            GUILayout.Space(CommonStyles.LeftContentPadding);
            GUILayout.EndHorizontal();
            EditorGUILayout.EndScrollView();
        }

        // ---------- 图片选择区 ----------

        private void DrawSourceImageSection(float contentWidth)
        {
            UIComponents.DrawSectionTitle("选择图片", uppercase: false);
            GUILayout.Space(CommonStyles.Space2);

            if (_sourceTexture != null)
            {
                // 已选图片：等比例显示，上限 MaxSourcePreviewHeight
                float aspect = (float)_sourceTexture.width / Mathf.Max(1, _sourceTexture.height);
                float displayW = Mathf.Min(contentWidth, _sourceTexture.width);
                float displayH = displayW / Mathf.Max(0.01f, aspect);
                if (displayH > MaxSourcePreviewHeight)
                {
                    displayH = MaxSourcePreviewHeight;
                    displayW = displayH * aspect;
                }

                Rect imgRect = GUILayoutUtility.GetRect(displayW, displayH, GUILayout.ExpandWidth(false));
                EditorGUI.DrawPreviewTexture(imgRect, _sourceTexture);

                // 点击选择新图片
                int clickId = GUIUtility.GetControlID(FocusType.Passive);
                if (Event.current.type == EventType.MouseDown && imgRect.Contains(Event.current.mousePosition))
                {
                    string path = EditorUtility.OpenFilePanelWithFilters("选择图片", "Assets", new[] { "Image files", "png,jpg,jpeg,tga,bmp", "All files", "*" });
                    if (!string.IsNullOrEmpty(path))
                    {
                        path = PathUtils.AbsolutePathToAssetsRelative(path);
                        SetSourceAsset(path);
                    }
                    Event.current.Use();
                }
                EditorGUIUtility.AddCursorRect(imgRect, MouseCursor.Link);

                // 替换按钮
                GUILayout.Space(4f);
                GUILayout.BeginHorizontal();
                {
                    if (GUILayout.Button("更换图片", CommonStyles.ButtonStyle, GUILayout.Width(80f)))
                    {
                        string path = EditorUtility.OpenFilePanelWithFilters("选择图片", "Assets", new[] { "Image files", "png,jpg,jpeg,tga,bmp", "All files", "*" });
                        if (!string.IsNullOrEmpty(path))
                        {
                            path = PathUtils.AbsolutePathToAssetsRelative(path);
                            SetSourceAsset(path);
                        }
                    }
                    if (GUILayout.Button("清除", CommonStyles.ButtonStyle, GUILayout.Width(60f)))
                    {
                        ClearSource();
                    }
                }
                GUILayout.EndHorizontal();
            }
            else
            {
                // 未选图片：空区域提示
                float emptyH = 120f;
                Rect emptyRect = GUILayoutUtility.GetRect(contentWidth, emptyH);
                var bgStyle = new GUIStyle { normal = { background = CommonStyles.CreateSolidColorTexture(new Color(1f, 1f, 1f, 0.03f)) } };
                GUI.Box(emptyRect, GUIContent.none, bgStyle);
                var hintStyle = new GUIStyle(CommonStyles.HintLabelStyle) { alignment = TextAnchor.MiddleCenter };
                GUI.Label(emptyRect, "点击选择图片或将图片拖入此区域", hintStyle);

                if (Event.current.type == EventType.MouseDown && emptyRect.Contains(Event.current.mousePosition))
                {
                    string path = EditorUtility.OpenFilePanelWithFilters("选择图片", "Assets", new[] { "Image files", "png,jpg,jpeg,tga,bmp", "All files", "*" });
                    if (!string.IsNullOrEmpty(path))
                    {
                        path = PathUtils.AbsolutePathToAssetsRelative(path);
                        SetSourceAsset(path);
                    }
                    Event.current.Use();
                }
                EditorGUIUtility.AddCursorRect(emptyRect, MouseCursor.Link);
            }

            if (!string.IsNullOrEmpty(_sourceAssetPath))
            {
                GUILayout.Space(2f);
                GUILayout.Label(_sourceAssetPath, CommonStyles.SmallGreyLeftLabelStyle);
            }
        }

        // ---------- 使用指南链接 ----------

        private void DrawGuideLink()
        {
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("✦ UI切图使用指南", CommonStyles.LinkStyle))
            {
                Application.OpenURL(GuideUrl);
            }
            UIComponents.AddLinkCursorToLastRect();
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
        }

        // ---------- 参数区 ----------

        private void DrawParameterSection(float contentWidth)
        {
            UIComponents.DrawSectionTitle("参数设置", uppercase: false);
            GUILayout.Space(CommonStyles.Space2);

            // 背景模式
            int newBgMode = EditorGUILayout.Popup("背景模式", _bgModeIndex, BgModeNames);
            if (newBgMode != _bgModeIndex)
            {
                _bgModeIndex = newBgMode;
                EditorPrefs.SetInt("TJGen_ImageSlice_BgMode", _bgModeIndex);
                InvalidateAnalysis();
            }

            // Alpha 阈值（透明背景模式）
            if (_bgModeIndex == 0 || _bgModeIndex == 1)
            {
                float newAlpha = EditorGUILayout.Slider("Alpha 阈值", _alphaThreshold, 0f, 1f);
                if (Mathf.Abs(newAlpha - _alphaThreshold) > 0.001f)
                {
                    _alphaThreshold = newAlpha;
                    EditorPrefs.SetFloat("TJGen_ImageSlice_AlphaThreshold", _alphaThreshold);
                    InvalidateAnalysis();
                }
            }

            // 颜色容差（纯色背景模式）
            if (_bgModeIndex == 0 || _bgModeIndex == 2)
            {
                float newTol = EditorGUILayout.Slider("颜色容差", _colorTolerance, 0f, 100f);
                if (Mathf.Abs(newTol - _colorTolerance) > 0.01f)
                {
                    _colorTolerance = newTol;
                    EditorPrefs.SetFloat("TJGen_ImageSlice_ColorTolerance", _colorTolerance);
                    InvalidateAnalysis();
                }
            }

            // 最小区域像素数
            int newMin = EditorGUILayout.IntField("最小区域(像素)", _minRegionPixels);
            if (newMin != _minRegionPixels)
            {
                _minRegionPixels = Mathf.Max(1, newMin);
                EditorPrefs.SetInt("TJGen_ImageSlice_MinRegion", _minRegionPixels);
                InvalidateAnalysis();
            }

            // 留白
            int newPad = EditorGUILayout.IntField("留白(像素)", _padding);
            if (newPad != _padding)
            {
                _padding = Mathf.Max(0, newPad);
                EditorPrefs.SetInt("TJGen_ImageSlice_Padding", _padding);
                InvalidateAnalysis();
            }

            // 设为 Sprite
            bool newSprite = EditorGUILayout.Toggle("自动设为 Sprite", _setAsSprite);
            if (newSprite != _setAsSprite)
            {
                _setAsSprite = newSprite;
                EditorPrefs.SetBool("TJGen_ImageSlice_SetAsSprite", _setAsSprite);
            }
        }

        // ---------- 预览区 ----------

        private void DrawPreviewSection(float contentWidth)
        {
            if (_sourceTexture == null)
                return;

            UIComponents.DrawSectionTitle("预览", uppercase: false);
            GUILayout.Space(CommonStyles.Space2);

            // 分析按钮
            EditorGUI.BeginDisabledGroup(_isProcessing);
            {
                if (GUILayout.Button("检测区域", ActionButtonStyle, GUILayout.Height(32f)))
                {
                    RunAnalysis();
                }
            }
            EditorGUI.EndDisabledGroup();

            // 区域数量
            if (_detectedRegions.Count > 0)
            {
                GUILayout.Space(CommonStyles.Space2);
                GUILayout.Label($"检测到 {_detectedRegions.Count} 个区域", CommonStyles.SectionTitleStyle);
            }

            // 预览图
            if (_previewTexture != null)
            {
                GUILayout.Space(CommonStyles.Space2);
                float maxPreviewHeight = 400f;
                float aspect = (float)_previewTexture.width / Mathf.Max(1, _previewTexture.height);
                float previewW = Mathf.Min(contentWidth, _previewTexture.width);
                float previewH = previewW / Mathf.Max(0.01f, aspect);
                if (previewH > maxPreviewHeight)
                {
                    previewH = maxPreviewHeight;
                    previewW = previewH * aspect;
                }

                Rect previewRect = GUILayoutUtility.GetRect(previewW, previewH, GUILayout.ExpandWidth(false));
                EditorGUI.DrawPreviewTexture(previewRect, _previewTexture);
            }
            else if (_isProcessing)
            {
                GUILayout.Space(CommonStyles.Space2);
                GUILayout.Label("正在分析...", CommonStyles.HintLabelStyle);
            }
        }

        // ---------- 操作区 ----------

        private void DrawActionSection(float contentWidth)
        {
            if (_detectedRegions.Count == 0)
                return;

            UIComponents.DrawSeparator();
            GUILayout.Space(CommonStyles.Space2);

            EditorGUI.BeginDisabledGroup(_isProcessing);
            {
                string btnText = $"切割并导出 ({_detectedRegions.Count} 张)";
                if (GUILayout.Button(btnText, ActionButtonStyle, GUILayout.Height(40f)))
                {
                    RunExport();
                }
            }
            EditorGUI.EndDisabledGroup();
        }

        // ========== 逻辑 ==========

        private void SetSourceAsset(string assetPath)
        {
            ClearPreview();
            _sourceAssetPath = assetPath;

            if (!string.IsNullOrEmpty(assetPath))
            {
                _sourceTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
            }
            else
            {
                _sourceTexture = null;
            }
            Repaint();
        }

        private void ClearSource()
        {
            ClearPreview();
            _sourceTexture = null;
            _sourceAssetPath = null;
        }

        private void ClearPreview()
        {
            if (_previewTexture != null)
            {
                if (string.IsNullOrEmpty(AssetDatabase.GetAssetPath(_previewTexture)))
                    DestroyImmediate(_previewTexture);
                _previewTexture = null;
            }
            _detectedRegions.Clear();
        }

        private void InvalidateAnalysis()
        {
            if (_detectedRegions.Count > 0 || _previewTexture != null)
            {
                ClearPreview();
                Repaint();
            }
        }

        private void RunAnalysis()
        {
            if (_sourceTexture == null)
                return;

            _isProcessing = true;
            Repaint();

            try
            {
                var readableTex = SpriteSequencePostProcessService.LoadReadableTextureFromAssetPath(_sourceAssetPath);
                if (readableTex == null)
                {
                    EditorUtility.DisplayDialog("图片切割", "无法读取图片像素，请确保图片不是压缩格式。", "确定");
                    _isProcessing = false;
                    return;
                }

                var result = ImageSliceService.Analyze(
                    readableTex,
                    BgModeValues[_bgModeIndex],
                    _alphaThreshold,
                    _colorTolerance,
                    _minRegionPixels,
                    _padding);

                ClearPreview();
                _detectedRegions = result.regions;
                _previewTexture = result.previewTexture;

                if (string.IsNullOrEmpty(AssetDatabase.GetAssetPath(readableTex)))
                    DestroyImmediate(readableTex);
            }
            catch (Exception e)
            {
                EditorUtility.DisplayDialog("图片切割", $"分析失败：{e.Message}", "确定");
            }
            finally
            {
                _isProcessing = false;
                Repaint();
            }
        }

        private void RunExport()
        {
            if (_sourceTexture == null)
                return;

            _isProcessing = true;
            Repaint();

            try
            {
                var readableTex = SpriteSequencePostProcessService.LoadReadableTextureFromAssetPath(_sourceAssetPath);
                if (readableTex == null)
                {
                    EditorUtility.DisplayDialog("图片切割", "无法读取图片像素。", "确定");
                    _isProcessing = false;
                    return;
                }

                var result = ImageSliceService.Export(
                    readableTex,
                    _sourceAssetPath,
                    BgModeValues[_bgModeIndex],
                    _alphaThreshold,
                    _colorTolerance,
                    _minRegionPixels,
                    _padding,
                    _setAsSprite);

                if (string.IsNullOrEmpty(AssetDatabase.GetAssetPath(readableTex)))
                    DestroyImmediate(readableTex);

                if (result.ExportedCount > 0)
                {
                    ReportImageSliceUsage(_sourceAssetPath, result.ExportedCount, BgModeNames[_bgModeIndex]);

                    var folderObj = AssetDatabase.LoadAssetAtPath<DefaultAsset>(result.OutputDirectory);
                    if (folderObj != null)
                    {
                        Selection.activeObject = folderObj;
                        EditorGUIUtility.PingObject(folderObj);
                    }

                    EditorUtility.DisplayDialog("图片切割",
                        $"成功切割并导出 {result.ExportedCount} 张图片\n\n输出目录：{result.OutputDirectory}",
                        "确定");
                }
                else
                {
                    EditorUtility.DisplayDialog("图片切割", "未检测到可切割的区域，请调整参数后重试。", "确定");
                }
            }
            catch (Exception e)
            {
                EditorUtility.DisplayDialog("图片切割", $"导出失败：{e.Message}", "确定");
            }
            finally
            {
                _isProcessing = false;
                Repaint();
            }
        }

        /// <summary>
        /// 向后端上报图片切割使用记录，用于任务统计。fire-and-forget，失败静默。
        /// </summary>
        private static void ReportImageSliceUsage(string sourceAssetPath, int sliceCount, string backgroundMode)
        {
            // 在主线程先取 token，避免后台线程访问 UnityConnectSession
            string token = UnityConnectSession.instance.GetAccessToken();
            if (string.IsNullOrEmpty(token))
            {
                try { token = CodelyTokenProvider.GetToken(); } catch { token = ""; }
            }
            if (string.IsNullOrEmpty(token))
                return;

            string url = ConfigManager.GetApiBaseUrl() + "task/image-slice";
            var body = new Dictionary<string, object>
            {
                ["sourceAssetPath"] = sourceAssetPath ?? "",
                ["sliceCount"] = sliceCount,
                ["backgroundMode"] = backgroundMode ?? ""
            };
            string jsonBody = JsonConvert.SerializeObject(body);

            Task.Run(() =>
            {
                try
                {
                    CodelyHttpClient.PostJsonSync(url, jsonBody, token, timeoutSeconds: 10);
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"[ImageSlice] 上报使用记录失败: {e.Message}");
                }
            });
        }
    }
}
#endif
