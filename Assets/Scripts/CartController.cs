using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CartController : MonoBehaviour, IInteractable
{
    public float cartOffset = 5f;
    public float floorOffset = 0.5f;
    
    [Header("Method 1")]
    public bool isAttatchedToPlayer = false;
    
    [Header("Method 2")]
    public float pushForce = 5f;
    
    [Space(10)]
    public bool drawGizmos = true;
    public bool method1 = true;
    public bool method2 = false;
    
    private Rigidbody rb;
    
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
            isAttatchedToPlayer = false;
        }
        
        
        if (isAttatchedToPlayer)
        {
            // Orient self to be in front of player
            
            // cartOffset is how many meters in front of the player you want the cart to be
            transform.position = PlayerStateMachine.Instance.transform.position + PlayerStateMachine.Instance.transform.forward * cartOffset + Vector3.up * floorOffset;

            // Orient the cart so that the cart’s BACK faces the player’s FRONT
            transform.rotation = Quaternion.LookRotation(-PlayerStateMachine.Instance.transform.forward, Vector3.up);
                
            
            // Move With Player when on ground
            
            // move quicker with the cart, slower without it
            
            // Speed is 100 when first starting out
            
            // Cart can get slower the more items are deposited in
        }
    }

    public void Interact()
    {
        if(method1)
        {
            isAttatchedToPlayer = true;
        } 
        else if (method2)
        {
            //Add a force to "push" this gameobject
            // cartOffset is how many meters in front of the player you want the cart to be
            transform.position = PlayerStateMachine.Instance.transform.position + PlayerStateMachine.Instance.transform.forward * cartOffset + Vector3.up * floorOffset;

            // Orient the cart so that the cart’s BACK faces the player’s FRONT
            transform.rotation = Quaternion.LookRotation(-PlayerStateMachine.Instance.transform.forward, Vector3.up);
            
            rb.AddForce(-transform.forward * pushForce, ForceMode.Impulse);
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
