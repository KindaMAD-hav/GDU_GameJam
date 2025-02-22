using UnityEngine;
using UnityEngine.AI;

public class Enemy : MonoBehaviour
{
    [Header("Components")]
    public Animator animator;       // Drag your Animator here
    public NavMeshAgent agent;      // Drag the NavMeshAgent here
    public Transform player;        // Reference to the Player's transform

    [Header("Settings")]
    public float chaseDistance = 10f;   // Distance at which enemy starts chasing
    public float attackDistance = 2f;   // Distance at which enemy attacks
    public float attackCooldown = 2f;   // Seconds between attacks
    public float destroyDelay = 3f;     // Time after death before destroying the enemy

    private float nextAttackTime = 0f;

    // Possible enemy states
    private enum EnemyState { Idle, Chase, Attack, Dead }
    private EnemyState currentState = EnemyState.Idle;

    private void Start()
    {
        // If references aren't set in Inspector, try to find them
        if (!animator) animator = GetComponent<Animator>();
        if (!agent) agent = GetComponent<NavMeshAgent>();
    }

    private void Update()
    {
        // If already dead, do nothing
        if (currentState == EnemyState.Dead) return;

        // Calculate distance to the player
        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        switch (currentState)
        {
            case EnemyState.Idle:
                // Play Idle animation
                animator.SetBool("IsRunning", false);

                // If player is within chase range, switch to chase
                if (distanceToPlayer < chaseDistance)
                {
                    currentState = EnemyState.Chase;
                }
                break;

            case EnemyState.Chase:
                // Play Run animation
                animator.SetBool("IsRunning", true);

                // Move towards the player
                agent.isStopped = false;
                agent.SetDestination(player.position);

                // If close enough, switch to attack
                if (distanceToPlayer <= attackDistance)
                {
                    currentState = EnemyState.Attack;
                    agent.isStopped = true;
                }
                break;

            case EnemyState.Attack:
                // Face the player
                transform.LookAt(player.position);

                // If player is out of range, go back to chase
                if (distanceToPlayer > attackDistance)
                {
                    currentState = EnemyState.Chase;
                    break;
                }

                // Attack if cooldown has passed
                if (Time.time >= nextAttackTime)
                {
                    // Randomly choose Attack1 or Attack2
                    if (Random.value < 0.5f)
                        animator.SetTrigger("Attack1");
                    else
                        animator.SetTrigger("Attack2");

                    // Set next time we can attack
                    nextAttackTime = Time.time + attackCooldown;
                }
                break;
        }
    }

    /// <summary>
    /// Kills the enemy: triggers death animation and destroys the GameObject.
    /// Call this when the enemy's health reaches 0, for example.
    /// </summary>
    public void Kill()
    {
        // Prevent double-death
        if (currentState == EnemyState.Dead) return;

        // Switch to Dead state
        currentState = EnemyState.Dead;

        // Stop movement and running animation
        animator.SetBool("IsRunning", false);
        agent.isStopped = true;

        // Trigger death animation
        animator.SetTrigger("Die");

        // Optionally destroy after delay (so death animation can play)
        Destroy(gameObject, destroyDelay);
    }
}
