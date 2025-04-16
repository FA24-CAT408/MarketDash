using System.Collections;
using System.Collections.Generic;
using KinematicCharacterController;
using UnityEngine;
using UnityEngine.Splines;

public class MovingPlatform : MonoBehaviour, IMoverController
{
    private SplineAnimate _splineAnimator;
    private PhysicsMover _mover;
    private Transform _transform;
    
    // Start is called before the first frame update
    void Start()
    {
        _mover = GetComponent<PhysicsMover>();
        _splineAnimator = GetComponent<SplineAnimate>();
        
        _transform = transform;
        
        _mover.MoverController = this;
    }

    public void UpdateMovement(out Vector3 goalPosition, out Quaternion goalRotation, float deltaTime)
    {
        // Store current pose
        Vector3 posBefore = _transform.position;
        Quaternion rotBefore = _transform.rotation;

        // Animate along spline
        _splineAnimator.NormalizedTime = Mathf.PingPong(Time.time / _splineAnimator.Duration, 1f);

        // Set goal pose
        goalPosition = _transform.position;
        goalRotation = _transform.rotation;

        // Reset pose
        _transform.position = posBefore;
        _transform.rotation = rotBefore;
    }
}
