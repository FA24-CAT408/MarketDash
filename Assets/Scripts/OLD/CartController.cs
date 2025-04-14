using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CartController : MonoBehaviour, IInteractable
{
    [Header("Cart Settings")]
    public float cartOffset = 5f;
    public float floorOffset = 0.5f;
    
    [Header("Joint Settings")]
    [Tooltip("Assign the dummy anchor on the player (a child GameObject with a kinematic Rigidbody)")]
    public Transform playerAnchor;
    public float jointSpring = 50f;
    public float jointDamper = 5f;
    public float jointBreakDistance = 7f; // If the cart goes farther than this, detach the joint.

    [Header("Method Options")]
    public bool useJointAttachment = true; // Use joint instead of parenting.
    [Tooltip("If true, this method pushes the cart with an impulse instead of attaching via joint.")]
    public bool usePushForce = false;
    public float pushForce = 5f;
    
    [Header("Debug")]
    public bool drawGizmos = true;
    public bool method1 = true;
    public bool method2 = false;
    
    private Rigidbody rb;
    private SpringJoint joint;
    
    // Start is called before the first frame update
    void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    // Update is called once per frame
    void Update()
    {
        if (PlayerStateMachine.Instance.IsJumpPressed)
        {
            rb.velocity = Vector3.zero;
            DetachCart();
        }
        
        if (joint != null && playerAnchor != null)
        {
            float distance = Vector3.Distance(transform.position, playerAnchor.position);
            if (distance > jointBreakDistance)
            {
                DetachCart();
                Debug.Log("Cart detached because it moved too far from the player.");
            }
        }
        
        
        // if (isAttatchedToPlayer)
        // {
        //     //Set the cart in front of the player ONCE using a coroutine
        //     if (!isSetUp)
        //     {
        //         StartCoroutine(SetUpCart());
        //     }
        //     
        //     
        //         
        //     
        //     // move quicker with the cart, slower without it
        //     // Speed is 100 when first starting out
        //     // Cart can get slower the more items are deposited in
        // }
        
        // Restrict Object from going into wall / platforms using Raycasts?
        
        
    }

    // IEnumerator SetUpCart()
    // {
    //     transform.position = PlayerStateMachine.Instance.transform.position + PlayerStateMachine.Instance.transform.forward * cartOffset + Vector3.up * floorOffset;
    //     transform.rotation = Quaternion.LookRotation(-PlayerStateMachine.Instance.transform.forward, Vector3.up);
    //     transform.parent = PlayerStateMachine.Instance.transform;
    //     isSetUp = true;
    //     yield return null;
    // }

    // public void Interact()
    // {
    //     if(method1)
    //     {
    //         isAttatchedToPlayer = true;
    //     } 
    //     else if (method2)
    //     {
    //         //Add a force to "push" this gameobject
    //         // cartOffset is how many meters in front of the player you want the cart to be
    //         transform.position = PlayerStateMachine.Instance.transform.position + PlayerStateMachine.Instance.transform.forward * cartOffset + Vector3.up * floorOffset;
    //
    //         // Orient the cart so that the cart’s BACK faces the player’s FRONT
    //         transform.rotation = Quaternion.LookRotation(-PlayerStateMachine.Instance.transform.forward, Vector3.up);
    //         
    //         rb.AddForce(-transform.forward * pushForce, ForceMode.Impulse);
    //     }
    //     
    // }
    
    public void Interact()
    {
        // Depending on which method you want, either attach via joint or apply a push.
        if (useJointAttachment && playerAnchor != null)
        {
            // Position the cart relative to the player before attaching.
            transform.position = PlayerStateMachine.Instance.transform.position 
                                  + PlayerStateMachine.Instance.transform.forward * cartOffset 
                                  + Vector3.up * floorOffset;
            transform.rotation = Quaternion.LookRotation(-PlayerStateMachine.Instance.transform.forward, Vector3.up);

            // Only add a joint if one isn’t already present.
            if (joint == null)
            {
                // Add and configure the SpringJoint.
                joint = gameObject.AddComponent<SpringJoint>();
                Rigidbody anchorRb = playerAnchor.GetComponent<Rigidbody>();
                if (anchorRb == null)
                {
                    Debug.LogError("Player Anchor must have a Rigidbody component (set to kinematic).");
                    return;
                }
                joint.connectedBody = anchorRb;
                joint.spring = jointSpring;
                joint.damper = jointDamper;
                joint.autoConfigureConnectedAnchor = false;
                joint.connectedAnchor = Vector3.zero; // Local point on the playerAnchor.
                joint.anchor = Vector3.zero;           // Local point on the cart.
            }
        }
        else if (usePushForce)
        {
            // Position the cart relative to the player.
            transform.position = PlayerStateMachine.Instance.transform.position 
                                  + PlayerStateMachine.Instance.transform.forward * cartOffset 
                                  + Vector3.up * floorOffset;
            transform.rotation = Quaternion.LookRotation(-PlayerStateMachine.Instance.transform.forward, Vector3.up);
            
            // Apply an impulse force to "push" the cart.
            rb.AddForce(-transform.forward * pushForce, ForceMode.Impulse);
        }
    }
    
    private void DetachCart()
    {
        if (joint != null)
        {
            Destroy(joint);
            joint = null;
        }
    }

    private void OnDrawGizmos()
    {
        if (drawGizmos)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawRay(transform.position, -transform.forward);
            
            Gizmos.color = Color.red;
            Gizmos.DrawRay(transform.position, -transform.up);
        }
    }
}
