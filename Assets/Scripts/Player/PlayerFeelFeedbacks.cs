using CrazyMarket.Player.V2;
using CrazyMarket.Player.V2.Unity;
using MoreMountains.Feedbacks;
using UnityEngine;

[RequireComponent(typeof(PlayerControllerV2))]
public sealed class PlayerFeelFeedbacks : MonoBehaviour
{
    [SerializeField] private Transform visual;
    [SerializeField] private MMF_Player jump;
    [SerializeField] private MMF_Player airJump;
    [SerializeField] private MMF_Player landing;
    private PlayerControllerV2 player;
    private long revision = -1;
    private bool wasGrounded;
    private float airborneTime;
    private Vector3 initialScale;
    private void Awake()
    {
        player = GetComponent<PlayerControllerV2>();
        initialScale = visual.localScale;
    }
    private void Update()
    {
        var snapshot = player.Snapshot;
        if (snapshot.Revision == revision) return;
        revision = snapshot.Revision;
        if ((snapshot.ActionFlags & PlayerActionFlags.Teleported) != 0)
        { StopAndRestore(); airborneTime = 0; wasGrounded = snapshot.StableGrounded; return; }
        if (!snapshot.StableGrounded) airborneTime += Time.deltaTime;
        MMF_Player feedback = null;
        if ((snapshot.ActionFlags & PlayerActionFlags.DoubleJumped) != 0) feedback = airJump;
        else if ((snapshot.ActionFlags & PlayerActionFlags.Jumped) != 0) feedback = jump;
        else if (snapshot.StableGrounded && !wasGrounded && airborneTime > .12f) feedback = landing;
        if (feedback != null) { StopAndRestore(); feedback.PlayFeedbacks(); }
        if (snapshot.StableGrounded) airborneTime = 0;
        wasGrounded = snapshot.StableGrounded;
    }
    private void StopAndRestore()
    {
        jump?.StopFeedbacks(); airJump?.StopFeedbacks(); landing?.StopFeedbacks();
        if (visual != null) visual.localScale = initialScale;
    }
    private void OnDisable() => StopAndRestore();
}
