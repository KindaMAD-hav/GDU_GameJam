using UnityEngine;

public class Enemy : MonoBehaviour
{
    public void Kill()
    {
        Debug.Log($"[Enemy] Kill() called on {gameObject.name}");
        // Destroy or handle death logic
        Destroy(gameObject);
    }
}
