using UnityEngine;

[CreateAssetMenu(fileName = "玩家动画配置SO", menuName = "玩家动画SO配置", order = 2)]
public class BossPlayerAnimationSO : ScriptableObject
{
    public AnimationClip idle;
    public AnimationClip walk;
    public AnimationClip jump;
    public AnimationClip fall;
    public AnimationClip attack;
    public AnimationClip skill;
    public AnimationClip ultimate;
}
