using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShadowDecal : MonoBehaviour
{
    public GameObject raycastOrigin;
    public GameObject shadowDecal;
    public float shadowDistance;
    
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        LayerMask layerMask = LayerMask.GetMask("Ground");
        RaycastHit hit;
        
        if (Physics.Raycast(raycastOrigin.transform.position, transform.TransformDirection(Vector3.down), out hit, shadowDistance, layerMask))

        { 
            shadowDecal.transform.position = hit.point;
        }
    }
}
