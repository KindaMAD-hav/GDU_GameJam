using UnityEngine;
using System.Collections;

public class PlayerAttack : MonoBehaviour
{
    [Header("Attack Settings")]
    public float attackRange = 2f;       // Melee range
    public float attackDelay = 0.5f;     // Delay before the "hit" registers
    public LayerMask enemyLayer;         // LayerMask for enemies

    private void Update()
    {
        // 1) Check for left-click
        if (Input.GetMouseButtonDown(0))
        {
            Debug.Log("[PlayerAttack] Left mouse button clicked. Starting attack.");
            StartCoroutine(PerformAttack());
        }
    }

    private IEnumerator PerformAttack()
    {
        Debug.Log($"[PlayerAttack] Attack coroutine started. Waiting {attackDelay} seconds to simulate sword swing...");

        // 2) Wait for the sword-swing delay
        yield return new WaitForSeconds(attackDelay);

        Debug.Log("[PlayerAttack] Performing attack now...");

        // 3) Detect enemies in a sphere around the player's transform
        Collider[] hits = Physics.OverlapSphere(transform.position, attackRange, enemyLayer);

        Debug.Log($"[PlayerAttack] OverlapSphere found {hits.Length} colliders in range {attackRange} on layer {enemyLayer.value}.");

        if (hits.Length == 0)
        {
            Debug.LogWarning("[PlayerAttack] No enemies found within attack range!");
        }

        // 4) Kill each enemy found
        foreach (Collider hit in hits)
        {
            Enemy enemy = hit.GetComponent<Enemy>();
            if (enemy != null)
            {
                Debug.Log($"[PlayerAttack] Enemy {enemy.gameObject.name} found. Calling Kill().");
                enemy.Kill();
            }
            else
            {
                Debug.LogWarning($"[PlayerAttack] Found collider '{hit.name}' but it does NOT have an Enemy component.");
            }
        }
    }

    // Visualize the attack range in the editor
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}
