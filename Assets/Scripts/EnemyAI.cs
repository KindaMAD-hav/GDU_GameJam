using UnityEngine;
using UnityEngine.AI;

public class EnemyAI : MonoBehaviour
{
    public Animator animator;
    public NavMeshAgent agent;
    public Transform player;

    // Distances for behavior
    public float chaseDistance = 10f;
    public float attackDistance = 2f;

    // States
    private enum EnemyState { Idle, Chase, Attack, Dead }
    private EnemyState currentState = EnemyState.Idle;

    // For random attack selection
    private float attackCooldown = 2f;
    private float nextAttackTime = 0f;

    void Start()
    {
        // Get references
        if (!animator) animator = GetComponent<Animator>();
        if (!agent) agent = GetComponent<NavMeshAgent>();

        // Initial state
        currentState = EnemyState.Idle;
    }

    void Update()
    {
        // If dead, do nothing
        if (currentState == EnemyState.Dead) return;

        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        switch (currentState)
        {
            case EnemyState.Idle:
                animator.SetBool("IsRunning", false);

                // If player is within chase range, switch to Chase
                if (distanceToPlayer < chaseDistance)
                {
                    currentState = EnemyState.Chase;
                }
                break;

            case EnemyState.Chase:
                animator.SetBool("IsRunning", true);
                agent.isStopped = false;
                agent.SetDestination(player.position);

                // If player is close enough, switch to Attack
                if (distanceToPlayer <= attackDistance)
                {
                    currentState = EnemyState.Attack;
                    agent.isStopped = true; // Stop moving to attack
                }
                break;

            case EnemyState.Attack:
                // Face the player
                transform.LookAt(player.position);

                // If player is out of range, go back to Chase
                if (distanceToPlayer > attackDistance)
                {
                    currentState = EnemyState.Chase;
                    break;
                }

                // Attack cooldown check
                if (Time.time >= nextAttackTime)
                {
                    // Randomly choose Attack1 or Attack2
                    float rand = Random.value; // 0.0 to 1.0
                    if (rand < 0.5f)
                    {
                        animator.SetTrigger("Attack1");
                    }
                    else
                    {
                        animator.SetTrigger("Attack2");
                    }

                    // Set next attack time
                    nextAttackTime = Time.time + attackCooldown;
                }

                break;

            case EnemyState.Dead:
                // Already handled, but left here for clarity
                break;
        }
    }

    // Called externally when the enemy should die
    public void Die()
    {
        if (currentState == EnemyState.Dead) return;

        currentState = EnemyState.Dead;
        animator.SetBool("IsRunning", false);
        animator.SetTrigger("Die");

        // Optionally disable colliders, AI, etc.
        agent.isStopped = true;
        Destroy(gameObject, 5f); // Remove after 5 seconds (optional)
    }
}
