using UnityEngine;

// Menu choreography only: collecting this visual never raises gameplay item events.
[DisallowMultipleComponent, RequireComponent(typeof(Animator))]
public sealed class MainMenuBlobbyPickup : MonoBehaviour
{
    [SerializeField] private Transform item;
    [SerializeField] private Transform glow;
    [SerializeField] private ParticleSystem collectionBurst;

    private Animator animator;
    private Transform hand;
    private Transform upperArm;
    private Transform forearm;
    private Quaternion animatedUpperArm;
    private Quaternion animatedForearm;
    private bool armPoseApplied;
    private Vector3 itemPosition;
    private Quaternion itemRotation;
    private Vector3 itemScale;
    private Vector3 glowScale;
    private float progress = -1f;
    private bool initialized;
    private bool collectionPlayed;

    private void Awake()
    {
        Initialize();
    }

    private void Initialize()
    {
        if (initialized || item == null)
            return;

        animator = GetComponent<Animator>();
        hand = animator.isHuman ? animator.GetBoneTransform(HumanBodyBones.RightHand) : null;
        upperArm = animator.isHuman ? animator.GetBoneTransform(HumanBodyBones.RightUpperArm) : null;
        forearm = animator.isHuman ? animator.GetBoneTransform(HumanBodyBones.RightLowerArm) : null;
        itemPosition = item.position;
        itemRotation = item.rotation;
        itemScale = item.localScale;
        if (glow != null)
            glowScale = glow.localScale;
        initialized = true;
    }

    public void SetProgress(float value)
    {
        progress = Mathf.Clamp01(value);
    }

    public void ResetPickup()
    {
        Initialize();
        RestoreArmPose();
        progress = -1f;
        collectionPlayed = false;
        if (collectionBurst != null)
            collectionBurst.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        if (!initialized || item == null)
            return;

        item.SetPositionAndRotation(itemPosition, itemRotation);
        item.localScale = itemScale;
        item.gameObject.SetActive(true);
        if (glow != null)
        {
            glow.localScale = glowScale;
            glow.gameObject.SetActive(true);
        }
    }

    private void Update()
    {
        RestoreArmPose();
    }

    private void ApplyReach()
    {
        if (hand == null || upperArm == null || forearm == null || item == null)
            return;

        float reach = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.15f, 0.42f, progress));
        float release = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.7f, 0.95f, progress));
        float weight = reach * (1f - release);
        if (weight <= 0f)
            return;

        animatedUpperArm = upperArm.localRotation;
        animatedForearm = forearm.localRotation;
        armPoseApplied = true;
        Vector3 pocket = transform.position + transform.right * 0.65f + transform.forward * 0.6f + Vector3.up * 1.6f;
        float retract = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.46f, 0.72f, progress));
        Vector3 target = Vector3.Lerp(itemPosition + Vector3.up * 0.25f, pocket, retract);

        // Solve just this arm after animation. The imported rig's humanoid IK
        // does not reach the visible hand accurately at its authored scale.
        Vector3 shoulder = upperArm.position;
        float upperLength = Vector3.Distance(shoulder, forearm.position);
        float lowerLength = Vector3.Distance(forearm.position, hand.position);
        Vector3 direction = target - shoulder;
        if (direction.sqrMagnitude < 0.0001f || upperLength < 0.001f || lowerLength < 0.001f)
            return;
        float distance = Mathf.Clamp(direction.magnitude,
            Mathf.Abs(upperLength - lowerLength) + 0.001f, upperLength + lowerLength - 0.001f);
        direction.Normalize();
        Vector3 bend = Vector3.ProjectOnPlane(Vector3.down - transform.forward, direction).normalized;
        if (bend.sqrMagnitude < 0.001f)
            bend = Vector3.ProjectOnPlane(transform.right, direction).normalized;
        float along = (upperLength * upperLength - lowerLength * lowerLength + distance * distance) / (2f * distance);
        float outwards = Mathf.Sqrt(Mathf.Max(0f, upperLength * upperLength - along * along));
        Vector3 elbow = shoulder + direction * along + bend * outwards;
        target = shoulder + direction * distance;
        upperArm.rotation = Quaternion.FromToRotation(forearm.position - shoulder, elbow - shoulder) * upperArm.rotation;
        forearm.rotation = Quaternion.FromToRotation(hand.position - forearm.position, target - forearm.position) * forearm.rotation;
        // Blend the elbow pose too, so starting the reach cannot abruptly change
        // its bend direction while the hand is still close to the idle pose.
        upperArm.localRotation = Quaternion.Slerp(animatedUpperArm, upperArm.localRotation, weight);
        forearm.localRotation = Quaternion.Slerp(animatedForearm, forearm.localRotation, weight);
    }

    private void RestoreArmPose()
    {
        if (!armPoseApplied)
            return;
        if (upperArm != null)
            upperArm.localRotation = animatedUpperArm;
        if (forearm != null)
            forearm.localRotation = animatedForearm;
        armPoseApplied = false;
    }

    private void LateUpdate()
    {
        if (!initialized || item == null)
            return;

        ApplyReach();
        bool grabbed = progress >= 0.45f;
        item.gameObject.SetActive(progress < 0.9f);
        if (grabbed && hand != null)
        {
            item.position = hand.position - Vector3.up * 0.25f;
            item.localScale = itemScale * (1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.75f, 0.9f, progress)));
        }
        else
        {
            // A small, slow float belongs to the collectible, never to the player.
            item.position = itemPosition + Vector3.up * (Mathf.Sin(Time.time * 2f) * 0.04f);
            item.rotation = itemRotation * Quaternion.Euler(0f, Time.time * 18f, 0f);
        }
        if (grabbed && !collectionPlayed)
        {
            collectionPlayed = true;
            if (collectionBurst != null)
            {
                collectionBurst.transform.position = item.position + Vector3.up * 0.25f
                    + collectionBurst.transform.forward * 0.5f;
                collectionBurst.Play();
            }
        }
        if (glow != null)
        {
            glow.gameObject.SetActive(!grabbed);
            float gather = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.32f, 0.45f, progress));
            float pulse = 1f + Mathf.Sin(Time.time * 2f) * 0.04f;
            glow.localScale = glowScale * pulse * Mathf.Lerp(1f, 0.15f, gather);
        }
    }

    private void OnDisable()
    {
        ResetPickup();
    }
}
