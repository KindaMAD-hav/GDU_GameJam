using UnityEngine;

public class Checkpoint : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            Debug.Log("[Checkpoint] Player reached checkpoint at: " + transform.position);
            other.GetComponent<PlayerMovement>().SetCheckpoint(transform.position);
        }
        else
        {
            Debug.Log("[Checkpoint] Non-player object entered checkpoint: " + other.name);
        }
    }
}
