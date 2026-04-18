using UnityEngine;

public enum EnemyState
{
    Patrol,
    Chase
}

public class EnemyAI : MonoBehaviour
{
    public Transform pointA;
    public Transform pointB;
    public float patrolSpeed = 2f;
    public float chaseSpeed = 4.5f;

    public float detectionRange = 8f;
    public float loseRange = 12f;
    public float arrivalDistance = 0.5f;

    private EnemyState currentState = EnemyState.Patrol;
    private Transform currentPatrolTarget;
    private Animator animator;
    private Transform player;

    void Start()
    {
        currentPatrolTarget = pointB;
        animator = GetComponentInChildren<Animator>();

        if (animator is null)
            Debug.LogWarning("EnemyAI: No Animator component found on " + gameObject.name);

        GameObject playerObject = GameObject.FindWithTag("Player");

        if (playerObject != null)
            player = playerObject.transform;
        else
            Debug.LogWarning("EnemyAI: No GameObject with tag 'Player' found in the scene.");
    }

    void Update()
    {
        // Can't do anything without a player or patrol points
        if (player == null || pointA == null || pointB == null)
        {
            if (animator != null) animator.SetFloat("Speed", 0f);
            return;
        }

        // How far is the player right now?
        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        // THE STATE MACHINE — run different code depending on current state
        switch (currentState)
        {
            case EnemyState.Patrol:
                Patrol();

                // Check: should we switch to Chase?
                if (distanceToPlayer <= detectionRange)
                {
                    currentState = EnemyState.Chase;
                    Debug.Log($"{gameObject.name}: Player detected! Switching to CHASE!");
                }
                break;

            case EnemyState.Chase:
                Chase();

                // Check: should we switch back to Patrol?
                if (distanceToPlayer > loseRange)
                {
                    currentState = EnemyState.Patrol;
                    Debug.Log($"{gameObject.name}: Lost the player. Back to PATROL.");
                }
                break;
        }
    }

    /// <summary>
    /// PATROL state: Walk between Point A and Point B.
    /// This is the same logic from last week's NPCPatrol script!
    /// </summary>
    private void Patrol()
    {
        // Calculate direction to the current patrol target
        Vector3 direction = currentPatrolTarget.position - transform.position;
        direction.y = 0;  // Stay flat — no flying!
        direction.Normalize();  // Make it a unit vector (just direction, length = 1)

        // Move toward the patrol target
        transform.position += direction * patrolSpeed * Time.deltaTime;

        // Smoothly rotate to face the direction we're walking
        if (direction.magnitude > 0.01f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetRotation,
                5f * Time.deltaTime
            );
        }

        // Check if we arrived at the patrol point
        float distanceToTarget = Vector3.Distance(transform.position, currentPatrolTarget.position);
        if (distanceToTarget < arrivalDistance)
        {
            // Swap targets! (ternary operator from last week)
            currentPatrolTarget = (currentPatrolTarget == pointA) ? pointB : pointA;
        }

        // Update animation — walk speed
        if (animator != null)
        {
            animator.SetFloat("Speed", patrolSpeed, 0.1f, Time.deltaTime);
        }
    }

    /// <summary>
    /// CHASE state: Run directly at the player!
    /// Instead of following patrol points, the enemy targets the player's position.
    /// </summary>
    private void Chase()
    {
        // Calculate direction to the PLAYER (not a patrol point!)
        Vector3 direction = player.position - transform.position;
        direction.y = 0;  // Stay flat

        // Only move if we're not already on top of the player
        float distToPlayer = direction.magnitude;
        if (distToPlayer > 0.5f)
        {
            direction.Normalize();

            // Move toward the player at chase speed (faster than patrol!)
            transform.position += direction * chaseSpeed * Time.deltaTime;

            // Smoothly rotate to face the player
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetRotation,
                8f * Time.deltaTime   // Turn faster during chase (8 vs 5)!
            );
        }

        // Update animation — chase speed (makes the enemy look like it's running!)
        if (animator != null)
        {
            animator.SetFloat("Speed", distToPlayer > 0.5f ? chaseSpeed : 0f, 0.1f, Time.deltaTime);
        }
    }
}
