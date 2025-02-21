using UnityEngine;

public class Enemy : MonoBehaviour
{
    public void Kill()
    {
        // Optional: Add death animation, particles, etc. before destroying
        Destroy(gameObject);
    }
}
