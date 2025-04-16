using KinematicCharacterController;
using UnityEngine;
using UnityEngine.Events;

public class RespawnComponent : MonoBehaviour
{
    [Header("Respawn Settings")]
    public Transform respawnPoint;

    [Header("Events")]
    public UnityEvent OnBeforeRespawn;
    public UnityEvent OnAfterRespawn;

    public void Respawn()
    {
        OnBeforeRespawn?.Invoke();

        if (respawnPoint != null)
        {
            var motor = GetComponent<KinematicCharacterMotor>();
            if (motor != null)
            {
                motor.SetPosition(respawnPoint.position);
            }
            else
            {
                transform.position = respawnPoint.position;
            }
        }

        OnAfterRespawn?.Invoke();
    }
}