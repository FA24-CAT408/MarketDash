using UnityEngine;
using UnityEngine.Splines;
using Unity.Mathematics;
using DG.Tweening;
using KinematicCharacterController;

public class NPCSplineWalker : MonoBehaviour, IMoverController
{
    [Header("References")]
    [SerializeField] private SplineContainer spline;
    [SerializeField] private Transform model;
    
    [Header("Movement Settings")]
    [SerializeField] private float moveDuration = 3f;
    [SerializeField] private bool pingPong = true;
    
    private PhysicsMover _mover;
    private Vector3 _targetPosition;
    private float _splinePosition = 0f;
    private bool _movingForward = true;
    private Tween _currentTween;
    
    void Start()
    {
        // Get references
        _mover = GetComponent<PhysicsMover>();
        _mover.MoverController = this;
        
        // Initialize position
        UpdateSplinePosition(_splinePosition);
        
        // Start movement
        MoveAlongSpline();
    }
    
    void MoveAlongSpline()
    {
        float endPosition = _movingForward ? 1f : 0f;
        
        _currentTween = DOTween.To(() => _splinePosition, 
            x => {
                _splinePosition = x;
                UpdateSplinePosition(x);
            }, 
            endPosition, moveDuration)
            .SetEase(Ease.Linear)
            .OnComplete(() => {
                if (pingPong) {
                    _movingForward = !_movingForward;
                } else {
                    _splinePosition = 0f;
                }
                MoveAlongSpline();
            });
    }
    
    void UpdateSplinePosition(float t)
    {
        if (spline == null) return;
        
        // Get position on spline - using correct float3 types
        float3 position;
        float3 tangent;
        float3 upVector;
        spline.Spline.Evaluate(t, out position, out tangent, out upVector);
        
        // Convert to world space
        _targetPosition = spline.transform.TransformPoint(new Vector3(position.x, position.y, position.z));
        
        // Update model rotation if needed
        if (model != null && math.length(tangent) > 0.01f)
        {
            Vector3 direction = spline.transform.TransformDirection(new Vector3(tangent.x, tangent.y, tangent.z));
            if (!_movingForward) direction = -direction;
            
            Quaternion lookRotation = Quaternion.LookRotation(direction, Vector3.up);
            model.rotation = Quaternion.Euler(0, lookRotation.eulerAngles.y, 0);
        }
    }
    
    public void UpdateMovement(out Vector3 goalPosition, out Quaternion goalRotation, float deltaTime)
    {
        // Set target position for the PhysicsMover
        goalPosition = _targetPosition;
        
        // Keep current rotation
        goalRotation = transform.rotation;
        
        // Update model position to follow root transform
        if (model != null && model != transform)
        {
            model.position = transform.position;
        }
    }
    
    void OnDestroy()
    {
        if (_currentTween != null && _currentTween.IsActive())
        {
            _currentTween.Kill();
        }
    }
}
