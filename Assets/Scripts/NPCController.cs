using UnityEngine;
using UnityEngine.Splines;
using DG.Tweening;

public class NPCSplineWalker : MonoBehaviour
{
    [SerializeField] private SplineContainer spline;
    [SerializeField] private Transform model;
    [SerializeField] private float moveDuration = 3f;
    [SerializeField] private float rotateDuration = 0.5f;
    [SerializeField] private Ease moveEase = Ease.Linear;
    [SerializeField] private Ease rotateEase = Ease.InOutSine;

    private bool movingForward = true;
    private float t = 0f;
    private bool isMoving = false;
    private bool isRotating = false;

    void Start()
    {
        if (spline == null || model == null) return;
        MoveAlongSpline();
    }

    void MoveAlongSpline()
    {
        isMoving = true;
        float start = movingForward ? 0f : 1f;
        float end = movingForward ? 1f : 0f;

        DOTween.To(() => t, x => {
            t = x;
            SetModelOnSpline(t, movingForward);
        }, end, moveDuration)
        .SetEase(moveEase)
        .OnComplete(() => {
            isMoving = false;
            RotateAtEnd();
        });
    }

    void RotateAtEnd()
    {
        isRotating = true;

        float endT = movingForward ? 1f : 0f;
        spline.Spline.Evaluate(endT, out var pos, out var tangent, out _);

        Vector3 worldTangent = spline.transform.TransformDirection(tangent);
        Vector3 lookDir = -worldTangent;

        if (lookDir.sqrMagnitude < 0.0001f)
            lookDir = -model.forward; // fallback to current backward

        Quaternion targetRot = Quaternion.identity;
        if (lookDir.sqrMagnitude > 0.0001f)
            targetRot = Quaternion.LookRotation(lookDir, Vector3.up);
        else
            targetRot = model.rotation; // fallback to current rotation

        model.DORotateQuaternion(targetRot, rotateDuration)
            .SetEase(rotateEase)
            .OnComplete(() => {
                movingForward = !movingForward;
                isRotating = false;
                MoveAlongSpline();
            });
    }

    void SetModelOnSpline(float t, bool forward)
    {
        spline.Spline.Evaluate(t, out var pos, out var tangent, out _);
        model.position = spline.transform.TransformPoint(pos);

        if (isRotating) return; // Don't update rotation during turn

        Vector3 worldTangent = spline.transform.TransformDirection(tangent);
        if (!forward) worldTangent = -worldTangent;

        if (worldTangent.sqrMagnitude > 0.0001f)
        {
            Quaternion lookRot = Quaternion.LookRotation(worldTangent, Vector3.up);
            model.rotation = Quaternion.Euler(0, lookRot.eulerAngles.y, 0);
        }
        // else: Do not set rotation if tangent is too small
    }


#if UNITY_EDITOR
    void OnValidate()
    {
        if (spline != null && model != null)
        {
            spline.Spline.Evaluate(t, out var pos, out var tangent, out _);
            model.position = spline.transform.TransformPoint(pos);

            Vector3 worldTangent = spline.transform.TransformDirection(tangent);

            if (worldTangent.sqrMagnitude > 0.0001f)
            {
                Quaternion lookRot = Quaternion.LookRotation(worldTangent, Vector3.up);
                model.rotation = Quaternion.Euler(0, lookRot.eulerAngles.y, 0);
            }
            // else: Do not set rotation if tangent is too small
        }
    }
#endif
}
