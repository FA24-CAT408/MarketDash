using Unity.VisualScripting;
using UnityEngine;

public class MinimapIconConfiner : MonoBehaviour
{
    [Header("References")]
    public Transform target;          // The target game object the icon represents
    private PlayerStateMachine player;          // The player's transform (center of the minimap)
    private SpriteRenderer icon;       // The SpriteRenderer component of the icon

    [Header("Minimap Settings")]
    public float minimapRadius = 5f;  // Radius of the minimap in world units

    void Start()
    {
        player = FindObjectOfType<PlayerStateMachine>();
        icon = GetComponent<SpriteRenderer>();
    }

    void Update()
    {
        // Calculate the offset from the player to the target on the XZ plane
        Vector3 offset = target.position - player.transform.position;
        Vector2 offset2D = new Vector2(offset.x, offset.z);

        // Calculate the distance to the target
        float distance = offset2D.magnitude;

        if (distance <= minimapRadius)
        {
            // Target is within minimap range; place the icon at the target's position
            icon.transform.position = new Vector3(target.position.x, icon.transform.position.y, target.position.z);
        }
        else
        {
            // Target is out of range; place the icon at the edge of the minimap ring
            Vector2 clampedOffset = offset2D.normalized * minimapRadius;
            Vector3 iconPosition = new Vector3(clampedOffset.x, icon.transform.position.y, clampedOffset.y) + player.transform.position;
            icon.transform.position = iconPosition;
        }
    }
}
