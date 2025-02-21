using UnityEngine;

public class DeathTrigger : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            Debug.Log("[DeathTrigger] Player hit death zone! Respawning...");
            other.GetComponent<PlayerMovement>().Respawn();
        }
        else
        {
            Debug.Log("[DeathTrigger] Non-player object entered death zone: " + other.name);
        }
    }
}
