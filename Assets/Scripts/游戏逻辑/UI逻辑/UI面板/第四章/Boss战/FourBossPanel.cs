using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using TangmenFramework;
using UnityEngine;
using UnityEngine.UI;

public class FourBossPanel : BasePanel
{
    [SerializeField]
    [Header("boss血条")]
    private Image bossHpBar;

    [SerializeField]
    [Header("玩家血量text")]
    private Text txtHp;

    public override void ShowMe()
    {
        base.ShowMe();
        this.DoPanelFadeInAnimation();
        EventCenter.Instance.AddEventListener<float>(MyEventTypeString.boss扣血事件, ChangeHpBar);
        EventCenter.Instance.AddEventListener<int>(MyEventTypeString.玩家受伤事件, ChangePlayerHp);

    }

    public override void HideMe()
    {
        base.HideMe();
        EventCenter.Instance.RemoveEventListener<float>(MyEventTypeString.boss扣血事件, ChangeHpBar);
        EventCenter.Instance.RemoveEventListener<int>(MyEventTypeString.玩家受伤事件, ChangePlayerHp);
    }

    /// <summary>
    /// 用DOTween平滑缩放血条的X轴，跟随rate（0~1）缓动变化
    /// </summary>
    /// <param name="rate">当前血量比例（0=空血，1=满血）</param>
    private void ChangeHpBar(float rate)
    {
        float targetScaleX = Mathf.Clamp01(rate);
        bossHpBar.rectTransform.DOScaleX(targetScaleX, 0.5f).SetEase(Ease.OutCubic);
    }

    private void ChangePlayerHp(int hp)
    {
        if (hp == 1)
            txtHp.text = "无敌";
        else
            txtHp.text =  hp.ToString() + "Hp";
    }
}
