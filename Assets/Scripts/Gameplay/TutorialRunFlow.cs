using System.Collections;
using CrazyMarket.Player.V2.Unity;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>Owns the first-shift briefing and its route; production managers own the order and clock.</summary>
public sealed class TutorialRunFlow : MonoBehaviour
{
    [SerializeField] private PlayerControllerV2 player;
    [SerializeField] private Item targetItem;
    [SerializeField] private Transform doorway;
    [SerializeField] private Transform delivery;
    public bool Briefing { get; private set; } = true;
    public Item TargetItem => targetItem;
    public Transform Player => player.transform;
    public bool Returning => GameManager.Instance != null && GameManager.Instance.CurrentState == GameManager.GameState.EndGame;
    public Vector3 Destination => Returning ? delivery.position : targetItem.transform.position;
    public Vector3 NavigationTarget
    {
        get
        {
            bool inHub = player.transform.position.x > doorway.position.x;
            return Returning ? (inHub ? delivery.position : doorway.position)
                : (inHub ? doorway.position : targetItem.transform.position);
        }
    }
    public string NavigationLabel => NavigationTarget == doorway.position
        ? (Returning ? "THROUGH THE DOORS" : "ENTER THE MARKET")
        : Returning ? "DELIVERY BAY" : "EGGS • DISPLAY TABLE";

    private IEnumerator Start()
    {
        player.SetControlBlocked("TutorialBriefing", true);
        yield return null;
        if (GameManager.Instance.CurrentState == GameManager.GameState.LoadingIn)
            GameManager.Instance.ChangeState(GameManager.GameState.PreGame);
    }

    public void Acknowledge()
    {
        if (!Briefing || GameManager.Instance.CurrentState != GameManager.GameState.PreGame) return;
        Briefing = false;
        StartCoroutine(ReleaseBriefingControls());
    }

    private IEnumerator ReleaseBriefingControls()
    {
        // The gamepad confirm button is also jump. Consume its release before returning movement.
        while (Gamepad.current != null && Gamepad.current.buttonSouth.isPressed) yield return null;
        yield return new WaitForFixedUpdate();
        player.SetControlBlocked("TutorialBriefing", false);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!Briefing && other.GetComponentInParent<PlayerControllerV2>() == player &&
            GameManager.Instance.CurrentState == GameManager.GameState.PreGame)
            GameManager.Instance.GameStart();
    }

    private void OnDestroy()
    {
        if (player != null) player.SetControlBlocked("TutorialBriefing", false);
    }
}



