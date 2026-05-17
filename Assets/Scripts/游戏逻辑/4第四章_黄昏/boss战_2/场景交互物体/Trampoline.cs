using TangmenFramework;
using UnityEngine;

/// <summary>
/// 蹦床：玩家踩上去时，会给玩家施加一个向上的弹跳力。
/// 弹跳力度可在Inspector中调节。
/// </summary>
public class Trampoline : MonoBehaviour
{
    [Header("弹跳力度")]
    [SerializeField] private float bounceForce = 15f;

    [Header("是否覆盖玩家自身的跳跃力（勾选则使用蹦床力度，否则与玩家跳跃力叠加）")]
    [SerializeField] private bool overridePlayerJumpForce = true;

    private Animator animator;

    private void Awake()
    {
        animator = GetComponent<Animator>();
    }
    /// <summary>
    /// 玩家碰到蹦床时，检测是否从上方落下，若是则施加弹跳力
    /// </summary>
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (!collision.gameObject.CompareTag("Player"))
            return;

        // 检测碰撞法线方向：法线从对方(玩家)指向自己(蹦床)，
        // 玩家在上方时法线朝下(normal.y < 0)，只有从上方踩下才弹跳
        if (collision.contacts[0].normal.y > -0.5f)
            return;

        // 播放动画
        if (animator != null)
            animator.SetTrigger("trigger");

        MusicMgr.Instance.PlaySound(MyAssetBundleName.第四章音效包, "蹦床");

        // 通过BossLevelMgr获取玩家控制器
        BossPlayerControl playerControl = BossLevelMgr.Instance?.PlayerControl;
        if (playerControl == null)
            return;

        Rigidbody2D playerRb = playerControl.GetComponent<Rigidbody2D>();
        if (playerRb == null)
            return;

        // 先清除当前Y轴速度，确保弹跳力度完全由蹦床控制
        Vector2 velocity = playerRb.velocity;
        velocity.y = 0f;
        playerRb.velocity = velocity;

        // 施加向上的弹跳力（使用Impulse模式让力瞬间生效）
        float force = overridePlayerJumpForce ? bounceForce : bounceForce + playerControl.jumpForce;
        playerRb.AddForce(Vector2.up * force, ForceMode2D.Impulse);
    }
}
