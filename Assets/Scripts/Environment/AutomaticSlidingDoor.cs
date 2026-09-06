using CrazyMarket.Player.V2.Unity;
using MoreMountains.Feedbacks;
using UnityEngine;

/// <summary>Approach from either side opens the leaves; the occupied doorway never closes.</summary>
[RequireComponent(typeof(BoxCollider))]
public sealed class AutomaticSlidingDoor : MonoBehaviour
{
    [SerializeField] private Transform leftLeaf;
    [SerializeField] private Transform rightLeaf;
    [SerializeField] private float travel = 4.5f;
    [SerializeField] private float slideTime = .4f;
    [SerializeField] private float closeDelay = 1.2f;
    [SerializeField] private MMF_Player openingFeedback;
    [SerializeField] private MMF_Player closingFeedback;
    private BoxCollider sensor;
    private Vector3 leftClosed, rightClosed;
    private readonly Collider[] overlaps = new Collider[32];
    private float amount, velocity, lastOccupied;
    public bool IsOpen { get; private set; }
    public float OpenAmount => amount;

    private void Awake()
    {
        sensor = GetComponent<BoxCollider>();
        leftClosed = leftLeaf.localPosition;
        rightClosed = rightLeaf.localPosition;
        lastOccupied = float.NegativeInfinity;
    }

    private void Update()
    {
        if (Time.timeScale == 0) return;
        Vector3 size = Vector3.Scale(sensor.size, transform.lossyScale) * .5f;
        int count = Physics.OverlapBoxNonAlloc(transform.TransformPoint(sensor.center), size, overlaps,
            transform.rotation, ~0, QueryTriggerInteraction.Ignore);
        bool occupied = count == overlaps.Length; // Fail open if an unexpectedly crowded sensor saturates.
        for (int i = 0; i < count; i++)
            if (overlaps[i].GetComponentInParent<PlayerControllerV2>() != null) occupied = true;
        if (occupied) lastOccupied = Time.time;
        bool open = occupied || Time.time - lastOccupied < closeDelay;
        if (open != IsOpen)
        {
            IsOpen = open;
            (open ? openingFeedback : closingFeedback)?.PlayFeedbacks();
        }
        amount = Mathf.SmoothDamp(amount, open ? 1 : 0, ref velocity, slideTime);
        leftLeaf.localPosition = leftClosed + Vector3.left * (travel * amount);
        rightLeaf.localPosition = rightClosed + Vector3.right * (travel * amount);
    }

    private void OnDisable()
    {
        openingFeedback?.StopFeedbacks();
        closingFeedback?.StopFeedbacks();
        if (leftLeaf != null) leftLeaf.localPosition = leftClosed;
        if (rightLeaf != null) rightLeaf.localPosition = rightClosed;
        amount = velocity = 0;
        IsOpen = false;
        lastOccupied = float.NegativeInfinity;
    }
}
