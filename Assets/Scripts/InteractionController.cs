using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using System.Linq;

public class InteractionController : MonoBehaviour
{
    [Header("Keybinds")]
    public PlayerControls controls;
    private InputAction interact;

    [Header("Interaction")]
    public Transform interactCheck;
    public LayerMask interactLayer;
    public float interactRadius;
    public bool interactGizmos = false;
    public Collider[] interactColliders;

    private void Awake()
    {
        controls = new PlayerControls();
    }

    // private void OnEnable()
    // {
    //     interact = controls.Player.Interact;
    //     controls.Enable();
    //     interact.performed += ctx => Interact();
    // }

    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        interactColliders = Physics.OverlapSphere(interactCheck.position, interactRadius, interactLayer);
    }

    void Interact()
    {
        Collider closestCollider = interactColliders.OrderBy(x => Vector3.Distance(interactCheck.position, x.transform.position)).FirstOrDefault();

        if (closestCollider != null)
        {
            closestCollider.GetComponent<IInteractable>().Interact();
        }
    }

    private void OnDrawGizmos()
    {
        if (interactGizmos)
        {
            if (interactColliders.Length > 0)
                Gizmos.color = Color.green;
            else
                Gizmos.color = Color.red;

            Gizmos.DrawWireSphere(interactCheck.position, interactRadius);
        }
    }
}
