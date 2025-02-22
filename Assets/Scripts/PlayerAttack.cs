using UnityEngine;
using System.Collections;

public class PlayerAttack : MonoBehaviour
{
    [Header("Attack Settings")]
    public float attackRange = 2f;
    public float attackDelay = 0.3f;
    public LayerMask enemyLayer;

    [Header("Animation")]
    public Animator animator;
    public float attackAnimationLength = 0.8f;

    private bool isAttacking = false;

    private void Start()
    {
        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }

        if (animator == null)
        {
            Debug.LogError("No Animator found on player! Please assign an Animator in the inspector.");
        }
    }

    private void Update()
    {
        if (Input.GetMouseButtonDown(0) && !isAttacking)
        {
            StartCoroutine(PerformAttack());
        }
    }

    private IEnumerator PerformAttack()
    {
        isAttacking = true;
        animator.SetBool("isAttacking", true);

        // Ensure animation starts before dealing damage
        yield return new WaitForSeconds(0.1f);

        // Check for hit only if animation is playing
        if (animator.GetCurrentAnimatorStateInfo(0).IsName("Attack"))
        {
            yield return new WaitForSeconds(attackDelay / 2);
            OnAttackPoint();
        }

        // Wait for the full animation to complete before allowing another attack
        yield return new WaitForSeconds(attackAnimationLength - (attackDelay / 2));

        isAttacking = false;
        animator.SetBool("isAttacking", false);
    }

    public void OnAttackPoint()
    {
        if (!animator.GetCurrentAnimatorStateInfo(0).IsName("Attack")) return;

        Ray ray = Camera.main.ScreenPointToRay(new Vector3(Screen.width / 2, Screen.height / 2, 0));
        if (Physics.Raycast(ray, out RaycastHit hitInfo, attackRange, enemyLayer))
        {
            Enemy enemy = hitInfo.collider.GetComponent<Enemy>();
            if (enemy != null)
            {
                enemy.Kill();
            }
        }
    }
}
