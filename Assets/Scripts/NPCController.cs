using UnityEngine;
using UnityEngine.AI;

public class NPCController : MonoBehaviour
{
    public float wanderRadius = 10f; // Radius to wander around
    public float wanderDelay = 3f;  // Delay between wander movements
    private NavMeshAgent agent;
    private float wanderTimer;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        wanderTimer = wanderDelay;
    }

    void Update()
    {
        wanderTimer -= Time.deltaTime;

        if (wanderTimer <= 0f)
        {
            Vector3 randomDestination = GetRandomNavMeshPoint(transform.position, wanderRadius);
            if (randomDestination != Vector3.zero)
            {
                agent.SetDestination(randomDestination);
            }
            wanderTimer = wanderDelay;
        }
    }

    Vector3 GetRandomNavMeshPoint(Vector3 origin, float radius)
    {
        Vector3 randomPoint = origin + Random.insideUnitSphere * radius;
        if (NavMesh.SamplePosition(randomPoint, out NavMeshHit hit, radius, NavMesh.AllAreas))
        {
            return hit.position;
        }
        return Vector3.zero; // Return zero if no valid position is found
    }
}