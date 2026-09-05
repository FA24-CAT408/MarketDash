using UnityEngine;
using UnityEngine.Splines;

[DisallowMultipleComponent]
public sealed class MainMenuBlobbyRunner : MonoBehaviour
{
    [SerializeField] private SplineContainer path;
    [SerializeField] private string runningParameter = "isRunning";
    [SerializeField, Min(0.1f)] private float moveSpeed = 7f;
    [SerializeField, Min(1f)] private float turnSpeed = 240f;
    [SerializeField, Min(0.1f)] private float acceleration = 10f;
    [SerializeField, Min(0.1f)] private float deceleration = 12f;
    [SerializeField, Min(0.1f)] private float turnLookAhead = 3f;
    [SerializeField, Min(0.1f)] private float animationMoveSpeed = 7f;
    [SerializeField, Range(0f, 1f)] private float pauseAt = 0.25f;
    [SerializeField, Min(0f)] private float pauseDuration = 2.6f;
    [SerializeField] private Transform lookTarget;
    [SerializeField, Range(0f, 1f)] private float registerPauseAt = 0.95f;
    [SerializeField, Min(0f)] private float registerPauseDuration = 1.8f;
    [SerializeField] private Transform registerLookTarget;
    [SerializeField] private MainMenuBlobbyPickup pickup;
    [SerializeField] private Transform[] headBones = new Transform[0];
    [SerializeField, Range(0f, 70f)] private float lookAngle = 50f;

    private Vector3 originPosition;
    private Quaternion originRotation;
    private Animator[] animators;
    private float[] originalAnimatorSpeeds;
    private Quaternion[] animatedHeadRotations;
    private bool headLookApplied;
    private int runningParameterHash;
    private float distanceTravelled;
    private float pathLength;
    private float currentSpeed;
    private float pauseElapsed;
    private bool isPausing;
    private bool pausedThisLap;
    private bool pausedAtRegisterThisLap;
    private bool pausingAtRegister;
    private bool isSlowing;
    private static readonly int CadenceHash = Animator.StringToHash("Cadence");
    private float ActivePauseDuration => pausingAtRegister ? registerPauseDuration : pauseDuration;

    private void Awake()
    {
        originPosition = transform.position;
        originRotation = transform.rotation;
        runningParameterHash = Animator.StringToHash(runningParameter);
        animators = System.Array.FindAll(
            GetComponentsInChildren<Animator>(true),
            animator => animator.runtimeAnimatorController != null && HasBoolParameter(animator, runningParameterHash));
        originalAnimatorSpeeds = new float[animators.Length];
        for (int i = 0; i < animators.Length; i++)
            originalAnimatorSpeeds[i] = animators[i].speed;
        animatedHeadRotations = new Quaternion[headBones.Length];
    }

    private void OnEnable()
    {
        if (!Application.isPlaying)
            return;

        pathLength = path != null && path.Spline != null ? path.CalculateLength() : 0f;
        distanceTravelled = 0f;
        currentSpeed = 0f;
        pauseElapsed = 0f;
        isPausing = false;
        pausedThisLap = false;
        pausedAtRegisterThisLap = false;
        pausingAtRegister = false;
        isSlowing = false;
        if (pickup != null)
            pickup.ResetPickup();
        if (HasUsablePath())
            PlaceOnPath(true);
        UpdateAnimation(false);
    }

    private void Update()
    {
        RestoreHeadPose();
        if (!HasUsablePath())
        {
            currentSpeed = 0f;
            isPausing = false;
            if (pickup != null)
                pickup.ResetPickup();
            UpdateAnimation(false);
            return;
        }

        float deltaTime = Time.deltaTime;
        if (isPausing)
        {
            pauseElapsed += deltaTime;
            if (!pausingAtRegister && pickup != null)
                pickup.SetProgress(pauseElapsed / Mathf.Max(0.01f, ActivePauseDuration));
            if (pauseElapsed < ActivePauseDuration)
            {
                UpdateAnimation(false);
                return;
            }

            isPausing = false;
        }

        float normalizedDistance = distanceTravelled / pathLength;
        Vector3 tangent = (Vector3)path.EvaluateTangent(normalizedDistance);
        Vector3 upcomingTangent = (Vector3)path.EvaluateTangent(
            Mathf.Repeat(distanceTravelled + turnLookAhead, pathLength) / pathLength);
        float corner = Mathf.InverseLerp(10f, 85f, Vector3.Angle(tangent, upcomingTangent));
        float targetSpeed = moveSpeed * Mathf.Lerp(1f, 0.4f, corner);
        float distanceToPause = float.PositiveInfinity;
        bool nextPauseIsRegister = false;
        if (!pausedThisLap && pauseDuration > 0f)
        {
            distanceToPause = Mathf.Max(0f, Mathf.Clamp01(pauseAt) * pathLength - distanceTravelled);
        }
        if (!pausedAtRegisterThisLap && registerPauseDuration > 0f)
        {
            float distanceToRegister = Mathf.Max(0f, Mathf.Clamp01(registerPauseAt) * pathLength - distanceTravelled);
            if (distanceToRegister < distanceToPause)
            {
                distanceToPause = distanceToRegister;
                nextPauseIsRegister = true;
            }
        }
        targetSpeed = Mathf.Min(targetSpeed, Mathf.Sqrt(2f * deceleration * distanceToPause));

        isSlowing = targetSpeed < currentSpeed;
        currentSpeed = Mathf.MoveTowards(currentSpeed, targetSpeed,
            (isSlowing ? deceleration : acceleration) * deltaTime);
        float step = currentSpeed * deltaTime;
        if (distanceToPause <= step + 0.001f)
        {
            distanceTravelled += distanceToPause;
            currentSpeed = 0f;
            pauseElapsed = 0f;
            isPausing = true;
            pausingAtRegister = nextPauseIsRegister;
            if (pausingAtRegister)
                pausedAtRegisterThisLap = true;
            else
            {
                pausedThisLap = true;
                if (pickup != null)
                    pickup.SetProgress(0f);
            }
        }
        else
        {
            distanceTravelled += step;
        }

        if (!isPausing && distanceTravelled >= pathLength)
        {
            distanceTravelled = Mathf.Repeat(distanceTravelled, pathLength);
            pausedThisLap = false;
            pausedAtRegisterThisLap = false;
            if (pickup != null)
                pickup.ResetPickup();
        }

        PlaceOnPath(false);
        // Blend the last step into idle just before translation reaches zero.
        UpdateAnimation(!isPausing && currentSpeed > (isSlowing ? 1.1f : 0.15f));
    }

    private void LateUpdate()
    {
        Transform target = pausingAtRegister ? registerLookTarget : lookTarget;
        float duration = ActivePauseDuration;
        if (!isPausing || target == null || duration <= 0f)
            return;

        // Apply after the Animator, and remove before its next evaluation. This also
        // prevents the offset accumulating when an Animator is culled or disabled.
        float settleDuration = Mathf.Min(0.22f, duration * 0.2f);
        float fadeDuration = Mathf.Min(0.65f, (duration - settleDuration) * 0.5f);
        float weight = Mathf.SmoothStep(0f, 1f,
            Mathf.Clamp01(Mathf.Min(pauseElapsed - settleDuration, duration - pauseElapsed) / fadeDuration));
        Vector3 direction = target.position - transform.position;
        direction.y = 0f;
        if (direction.sqrMagnitude < 0.001f)
            return;

        float yaw = Mathf.Clamp(Vector3.SignedAngle(transform.forward, direction, Vector3.up),
            -lookAngle, lookAngle) * weight;
        for (int i = 0; i < headBones.Length; i++)
        {
            if (headBones[i] == null)
                continue;
            animatedHeadRotations[i] = headBones[i].localRotation;
            headBones[i].rotation = Quaternion.AngleAxis(yaw, Vector3.up) * headBones[i].rotation;
        }
        headLookApplied = true;
    }

    private void OnDisable()
    {
        if (!Application.isPlaying)
            return;

        RestoreHeadPose();
        if (pickup != null)
            pickup.ResetPickup();
        UpdateAnimation(false);
        for (int i = 0; i < animators.Length; i++)
        {
            if (animators[i] != null)
                animators[i].speed = originalAnimatorSpeeds[i];
        }
        transform.SetPositionAndRotation(originPosition, originRotation);
    }

    private void RestoreHeadPose()
    {
        if (!headLookApplied)
            return;
        for (int i = 0; i < headBones.Length; i++)
        {
            if (headBones[i] != null)
                headBones[i].localRotation = animatedHeadRotations[i];
        }
        headLookApplied = false;
    }

    private void UpdateAnimation(bool running)
    {
        for (int i = 0; i < animators.Length; i++)
        {
            Animator animator = animators[i];
            if (animator == null || !animator.isActiveAndEnabled)
                continue;
            animator.SetBool(runningParameterHash, running);
            // Only the moving state changes cadence. Idle and the stop/start blends
            // keep their own timing, so slowing down cannot freeze the transition.
            float speedRatio = currentSpeed / Mathf.Max(0.1f, animationMoveSpeed);
            animator.SetFloat(CadenceHash, Mathf.Max(0.55f, speedRatio * 1.5f));
            animator.speed = 1f;
        }
    }

    private bool HasUsablePath()
    {
        return path != null && path.Spline != null && path.Spline.Count > 1
            && path.Spline.Closed && pathLength > 0.01f;
    }

    private void PlaceOnPath(bool snapRotation)
    {
        if (!path.Evaluate(distanceTravelled / pathLength, out var position, out var tangent, out _))
            return;

        transform.position = (Vector3)position;
        Vector3 ahead = (Vector3)path.EvaluatePosition(
            Mathf.Repeat(distanceTravelled + turnLookAhead * 0.35f, pathLength) / pathLength);
        Vector3 direction = snapRotation ? (Vector3)tangent : ahead - transform.position;
        direction.y = 0f;
        if (direction.sqrMagnitude < 0.0001f)
            return;

        Quaternion targetRotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
        transform.rotation = snapRotation ? targetRotation : Quaternion.RotateTowards(
            transform.rotation,
            Quaternion.Slerp(transform.rotation, targetRotation, 1f - Mathf.Exp(-12f * Time.deltaTime)),
            turnSpeed * Time.deltaTime);
    }

    private static bool HasBoolParameter(Animator targetAnimator, int parameterHash)
    {
        foreach (AnimatorControllerParameter parameter in targetAnimator.parameters)
        {
            if (parameter.nameHash == parameterHash && parameter.type == AnimatorControllerParameterType.Bool)
                return true;
        }
        return false;
    }

    private void OnDrawGizmosSelected()
    {
        if (path == null || path.Spline == null || path.Spline.Count < 2)
            return;

        Gizmos.color = new Color(1f, 0.75f, 0.2f, 0.85f);
        Vector3 previous = path.EvaluatePosition(0f);
        const int samples = 48;
        for (int i = 1; i <= samples; i++)
        {
            Vector3 current = path.EvaluatePosition(i / (float)samples);
            Gizmos.DrawLine(previous, current);
            previous = current;
        }
        Gizmos.DrawWireSphere(path.EvaluatePosition(pauseAt), 0.5f);
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(path.EvaluatePosition(registerPauseAt), 0.5f);
    }
}
