using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MovingPlatform : MonoBehaviour
{
    [SerializeField] private PlayerStateMachine player;
    [SerializeField] private GameObject playerOriginalParent;

    // Start is called before the first frame update
    void Start()
    {
        player = FindObjectOfType<PlayerStateMachine>();
        playerOriginalParent = player.transform.parent.gameObject;
    }

    // Update is called once per frame
    void Update()
    {

    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("Player"))
        {
            player.transform.SetParent(transform, true);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.gameObject.CompareTag("Player"))
        {
            player.transform.SetParent(playerOriginalParent.transform, false);
        }
    }

    // private void OnTriggerEnter(Collision other)
    // {
    //     if (other.gameObject.CompareTag("Player"))
    //     {
    //         player.transform.SetParent(transform);
    //     }
    // }

    // private void OnCollisionExit(Collision other)
    // {
    //     if (other.gameObject.CompareTag("Player"))
    //     {
    //         player.transform.SetParent(null);
    //     }
    // }
}
