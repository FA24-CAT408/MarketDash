using System.Linq;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public class PlayerCollisionManager : MonoBehaviour
{
    private Transform currentSpawnPoint;
    private Vector3 _startingPosition;
    public float spawnPointRadius = 10f;
    void Start()
    {
        _startingPosition = transform.position;
        
        currentSpawnPoint = null;
    }

    void Update()
    {
        // UpdateClosestSpawnPoint();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("Item"))
        {
            other.GetComponent<Item>().Interact();
            
        } else if (other.gameObject.CompareTag("Death"))
        {
            GameManager.Instance.RespawnPlayer(currentSpawnPoint);
        }
    }
    
    private void UpdateClosestSpawnPoint()
    {
        // Find all spawn points within the defined radius
        Collider[] nearbySpawnPoints = Physics.OverlapSphere(transform.position, spawnPointRadius)
            .Where(c => c.CompareTag("SpawnPoint"))
            .ToArray();
        
        Debug.Log($"Found {nearbySpawnPoints.Length} spawn points within radius.");

        if (nearbySpawnPoints.Length > 0)
        {
            // Find the closest spawn point
            currentSpawnPoint = nearbySpawnPoints
                .Select(c => c.transform)
                .OrderBy(t => Vector3.Distance(transform.position, t.position))
                .First();

            Debug.Log($"Closest spawn point updated to: {currentSpawnPoint.position}");
        }else if (currentSpawnPoint == null)
        {
            // Default to starting position if no spawn point was previously found
            Debug.LogWarning("No spawn points found in radius. Defaulting to starting position.");
            currentSpawnPoint = new GameObject("StartingPosition").transform;
            currentSpawnPoint.position = _startingPosition;
        }
        else
        {
            // Keep the last spawn point if no new one is found
            Debug.Log($"No new spawn points found. Keeping the last spawn point at: {currentSpawnPoint.position}");
        }
    }

    private void OnDrawGizmosSelected()
    {
        // Visualize the spawn point radius in the editor
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, spawnPointRadius);
    }
}
