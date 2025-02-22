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
    private float nextDebugTime = 0f;    // Timer for debug logging
    private const float DEBUG_INTERVAL = 1f;  // Log every second

    // Possible enemy states
    private enum EnemyState { Idle, Chase, Attack, Dead }
    private EnemyState currentState = EnemyState.Idle;

    private void Start()
    {
        // If references aren't set in Inspector, try to find them
        if (!animator) animator = GetComponent<Animator>();
        if (!agent) agent = GetComponent<NavMeshAgent>();

        // Ensure initial state is correctly set
        UpdateAnimationState();
    }

    private void Update()
    {
        // Debug logging every second
        if (Time.time >= nextDebugTime)
        {
            Debug.Log($"[{gameObject.name}] IsRunning: {animator.GetBool("IsRunning")}, Current State: {currentState}, Agent Velocity: {agent.velocity.magnitude}");
            nextDebugTime = Time.time + DEBUG_INTERVAL;
        }

        // If already dead, do nothing
        if (currentState == EnemyState.Dead) return;

        // Calculate distance to the player
        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        EnemyState previousState = currentState;

        switch (currentState)
        {
            case EnemyState.Idle:
                // If player is within chase range, switch to chase
                if (distanceToPlayer < chaseDistance)
                {
                    currentState = EnemyState.Chase;
                    Debug.Log($"[{gameObject.name}] Transitioning from Idle to Chase. Distance to player: {distanceToPlayer}");
                }
                break;

            case EnemyState.Chase:
                // Move towards the player
                agent.isStopped = false;
                agent.SetDestination(player.position);

                // If close enough, switch to attack
                if (distanceToPlayer <= attackDistance)
                {
                    currentState = EnemyState.Attack;
                    agent.isStopped = true;
                    Debug.Log($"[{gameObject.name}] Transitioning from Chase to Attack. Distance to player: {distanceToPlayer}");
                }
                break;

            case EnemyState.Attack:
                // Face the player
                transform.LookAt(player.position);

                // If player is out of range, go back to chase
                if (distanceToPlayer > attackDistance)
                {
                    currentState = EnemyState.Chase;
                    Debug.Log($"[{gameObject.name}] Transitioning from Attack back to Chase. Distance to player: {distanceToPlayer}");
                    break;
                }

                // Attack if cooldown has passed
                if (Time.time >= nextAttackTime)
                {
                    // Randomly choose Attack1 or Attack2
                    bool isAttack1 = Random.value < 0.5f;
                    animator.SetTrigger(isAttack1 ? "Attack1" : "Attack2");
                    Debug.Log($"[{gameObject.name}] Performing {(isAttack1 ? "Attack1" : "Attack2")}");

                    // Set next time we can attack
                    nextAttackTime = Time.time + attackCooldown;
                }
                break;
        }

        // If state changed, update animation
        if (previousState != currentState)
        {
            UpdateAnimationState();
        }
    }

    private void UpdateAnimationState()
    {
        // Reset all animation states
        animator.SetBool("IsRunning", false);

        // Set appropriate animation for current state
        switch (currentState)
        {
            case EnemyState.Chase:
                animator.SetBool("IsRunning", true);
                break;
            case EnemyState.Attack:
                animator.SetBool("IsRunning", false);
                break;
            case EnemyState.Idle:
                animator.SetBool("IsRunning", false);
                break;
            case EnemyState.Dead:
                animator.SetBool("IsRunning", false);
                break;
        }
    }

    public void Kill()
    {
        // Prevent double-death
        if (currentState == EnemyState.Dead) return;

        // Switch to Dead state
        currentState = EnemyState.Dead;
        Debug.Log($"[{gameObject.name}] Enemy killed");

        // Update animation state
        UpdateAnimationState();

        // Stop movement
        agent.isStopped = true;

        // Trigger death animation
        animator.SetTrigger("Die");

        // Optionally destroy after delay (so death animation can play)
        Destroy(gameObject, destroyDelay);
    }
}