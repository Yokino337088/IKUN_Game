using UnityEngine;
using UnityEngine.UI;
using TangmenFramework;

/// <summary>
/// 演出对话UI控制器 —— 处理对话文字的显示。
/// 
/// 【使用方式】
/// 挂载到UI画布的对话文字组件上，在Inspector中设置引用。
/// 自动订阅PerformanceDirector的OnDialogueDisplay事件。
/// </summary>
public class PerformanceDialogueUI : MonoBehaviour
{
    [Header("========== UI组件引用 ==========")]
    [Tooltip("说话人名称（Text）")]
    public Text speakerNameText;
    
    [Tooltip("对话内容（Text）")]
    public Text dialogueText;
    
    [Tooltip("对话框背景（可选）")]
    public RectTransform dialogueBox;

    [Header("========== 设置 ==========")]
    [Tooltip("文字显示动画时长（秒）")]
    public float displayDuration = 2f;
    
    [Tooltip("是否自动隐藏")]
    public bool autoHide = true;

    private void Start()
    {
        // 订阅PerformanceDirector的事件
        var director = FindObjectOfType<PerformanceDirector>();
        if (director != null)
        {
            director.OnDialogueDisplay += OnDialogueReceived;
            LogSystem.Info("PerformanceDialogueUI: 已订阅 OnDialogueDisplay 事件");
        }
        else
        {
            LogSystem.Debug("PerformanceDialogueUI: 未找到 PerformanceDirector");
        }
    }

    private void OnDestroy()
    {
        // 取消订阅
        var director = FindObjectOfType<PerformanceDirector>();
        if (director != null)
        {
            director.OnDialogueDisplay -= OnDialogueReceived;
        }
    }

    private void OnDialogueReceived(string speakerName, string content)
    {
        if (string.IsNullOrEmpty(content))
        {
            LogSystem.Debug("PerformanceDialogueUI: 收到空对话内容");
            return;
        }

        LogSystem.Info($"PerformanceDialogueUI: 显示对话 - [{speakerName}] {content}");
        
        // 显示UI
        if (dialogueBox != null)
            dialogueBox.gameObject.SetActive(true);
        
        // 设置说话人
        if (speakerNameText != null)
        {
            speakerNameText.text = speakerName ?? "";
            speakerNameText.gameObject.SetActive(!string.IsNullOrEmpty(speakerName));
        }
        
        // 设置对话内容
        if (dialogueText != null)
        {
            dialogueText.text = content;
        }
    }
}
