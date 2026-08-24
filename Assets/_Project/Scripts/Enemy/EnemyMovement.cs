using UnityEngine;
using UnityEngine.AI;

/*
 * Script Name: EnemyMovement
 * Purpose:
 * Controls NavMesh-based enemy movement for the different AI states.
 *
 * Current supported movement:
 * - Patrol: Moves between assigned patrol points in order.
 * - Chase: Moves directly toward the detected player.
 * - Attack: Stops movement and faces the player.
 * - Search: Moves to the player's last known position.
 *
 * Future movement:
 * - Investigate noise sources.
 * - Return to patrol route intelligently.
 * - Randomized searching around the last known player position.
 */

public class EnemyMovement : MonoBehaviour
{
    #region Component References

    [Header("Component References")]

    [Tooltip("Provides access to enemy statistics and the current player target.")]
    [SerializeField] private EnemyController enemyController;

    [Tooltip("Provides detection information including the player's last known position.")]
    [SerializeField] private EnemyDetection enemyDetection;

    [Tooltip("Provides access to the enemy's current AI state.")]
    [SerializeField] private EnemyStateMachine enemyStateMachine;

    [Tooltip("NavMesh Agent used to move the enemy through the environment.")]
    [SerializeField] private NavMeshAgent navMeshAgent;

    [Tooltip("Contains the scene-specific patrol points assigned to this enemy.")]
    [SerializeField] private EnemyPatrolRoute enemyPatrolRoute;

    #endregion


    #region Patrol Settings

    [Header("Patrol Settings")]

    [Tooltip("Distance from a patrol point that counts as reaching the destination.")]
    [SerializeField] private float patrolArrivalDistance = 0.75f;

    #endregion


    #region Search Settings

    [Header("Search Settings")]

    [Tooltip("Distance from the last known player position that counts as arriving at the search destination.")]
    [SerializeField] private float searchArrivalDistance = 0.75f;

    #endregion


    #region Rotation Settings

    [Header("Rotation Settings")]

    [Tooltip("Controls how quickly the enemy turns toward its movement direction or target.")]
    [SerializeField] private float rotationSpeed = 8f;

    #endregion


    #region Runtime Debug Information

    [Header("Runtime Debug Information")]

    [Tooltip("Current distance between the enemy and its active player target.")]
    [SerializeField] private float distanceToTarget;

    [Tooltip("True while the enemy is actively chasing the player.")]
    [SerializeField] private bool isChasing;

    [Tooltip("True while the enemy has reached its search destination.")]
    [SerializeField] private bool isSearching;

    [Tooltip("Current destination used during the Search state.")]
    [SerializeField] private Vector3 searchDestination;

    [Tooltip("Current patrol destination used during the Patrol state.")]
    [SerializeField] private Vector3 patrolDestination;

    #endregion


    #region Public Properties

    public bool IsChasing
    {
        get { return isChasing; }
    }

    public bool IsSearching
    {
        get { return isSearching; }
    }

    public Vector3 SearchDestination
    {
        get { return searchDestination; }
    }

    public Vector3 PatrolDestination
    {
        get { return patrolDestination; }
    }

    #endregion


    #region Unity Methods

    private void Awake()
    {
        // Finds the EnemyController automatically if the Inspector reference was not assigned.
        if (enemyController == null)
        {
            enemyController = GetComponent<EnemyController>();
        }

        // Finds the EnemyDetection component automatically if needed.
        if (enemyDetection == null)
        {
            enemyDetection = GetComponent<EnemyDetection>();
        }

        // Finds the EnemyStateMachine component automatically if needed.
        if (enemyStateMachine == null)
        {
            enemyStateMachine = GetComponent<EnemyStateMachine>();
        }

        // Finds the NavMeshAgent component automatically if needed.
        if (navMeshAgent == null)
        {
            navMeshAgent = GetComponent<NavMeshAgent>();
        }

        // Finds the patrol route attached to this enemy if the Inspector reference was not assigned.
        if (enemyPatrolRoute == null)
        {
            enemyPatrolRoute = GetComponent<EnemyPatrolRoute>();
        }
    }


    private void Start()
    {
        // Rotation is handled manually so movement direction can be controlled consistently.
        if (navMeshAgent != null)
        {
            navMeshAgent.updateRotation = false;
        }
    }


    private void Update()
    {
        // Movement cannot continue without the required core components.
        if (enemyController == null ||
            enemyStateMachine == null ||
            navMeshAgent == null)
        {
            return;
        }

        // NavMeshAgent movement cannot be used until the enemy is standing on a baked NavMesh.
        if (!navMeshAgent.isOnNavMesh)
        {
            return;
        }

        // Runs movement behavior based on the enemy's current AI state.
        switch (enemyStateMachine.CurrentState)
        {
            case EnemyState.Patrol:
                HandlePatrol();
                break;

            case EnemyState.Investigate:
                HandleInvestigate();
                break;

            case EnemyState.Chase:
                HandleChase();
                break;

            case EnemyState.Attack:
                HandleAttackPosition();
                break;

            case EnemyState.Search:
                HandleSearch();
                break;

            case EnemyState.Return:
                HandleReturn();
                break;
        }
    }

    #endregion


    #region Patrol Movement

    private void HandlePatrol()
    {
        // Patrol cannot function without a valid patrol route.
        if (enemyPatrolRoute == null)
        {
            StopMovement();
            return;
        }

        // Patrol cannot function if no patrol points were assigned.
        if (!enemyPatrolRoute.HasPatrolPoints)
        {
            StopMovement();
            return;
        }

        // Gets the currently selected patrol point.
        Transform currentPatrolPoint = enemyPatrolRoute.CurrentPatrolPoint;

        // Protects against an empty or missing patrol point reference.
        if (currentPatrolPoint == null)
        {
            StopMovement();
            return;
        }

        // Stores the current patrol destination for Inspector debugging.
        patrolDestination = currentPatrolPoint.position;

        // Uses the normal movement speed while patrolling.
        navMeshAgent.speed = enemyController.MoveSpeed;

        // Allows the NavMesh Agent to move.
        navMeshAgent.isStopped = false;

        // Sends the enemy toward the active patrol point.
        navMeshAgent.SetDestination(patrolDestination);

        // Rotates the enemy toward its actual NavMesh movement direction.
        RotateTowardMovementDirection();

        // Calculates horizontal distance to the current patrol point.
        Vector3 flatEnemyPosition = transform.position;
        flatEnemyPosition.y = 0f;

        Vector3 flatPatrolPosition = patrolDestination;
        flatPatrolPosition.y = 0f;

        float distanceToPatrolPoint =
            Vector3.Distance(flatEnemyPosition, flatPatrolPosition);

        // Moves to the next patrol point after reaching the current point.
        if (distanceToPatrolPoint <= patrolArrivalDistance)
        {
            enemyPatrolRoute.AdvanceToNextPatrolPoint();
        }

        isChasing = false;
        isSearching = false;
    }

    #endregion


    #region Chase Movement

    private void HandleChase()
    {
        Transform target = enemyController.CurrentTarget;

        // Stops movement if the target no longer exists.
        if (target == null)
        {
            StopMovement();
            return;
        }

        // Calculates the current distance to the player.
        distanceToTarget =
            Vector3.Distance(transform.position, target.position);

        // Stops advancing once the player is inside attack range.
        if (distanceToTarget <= enemyController.AttackRange)
        {
            navMeshAgent.isStopped = true;

            FaceTarget(target.position);

            isChasing = false;

            return;
        }

        // Chase movement uses the faster chase speed from EnemyData.
        navMeshAgent.speed = enemyController.ChaseSpeed;

        // Allows NavMesh movement.
        navMeshAgent.isStopped = false;

        // Continuously updates the destination to follow the moving player.
        navMeshAgent.SetDestination(target.position);

        // Turns the enemy toward its movement direction.
        RotateTowardMovementDirection();

        isChasing = true;
        isSearching = false;
    }

    #endregion


    #region Attack Movement

    private void HandleAttackPosition()
    {
        Transform target = enemyController.CurrentTarget;

        // Stops movement while the attack system handles combat.
        StopMovement();

        // Continues facing the player while attacking.
        if (target != null)
        {
            FaceTarget(target.position);
        }
    }

    #endregion


    #region Search Movement

    private void HandleSearch()
    {
        // Search behavior requires detection memory.
        if (enemyDetection == null)
        {
            StopMovement();
            return;
        }

        // Stops if there is no remembered player location.
        if (!enemyDetection.HasLastKnownPosition)
        {
            StopMovement();
            return;
        }

        // Copies the player's last visible position.
        searchDestination = enemyDetection.LastKnownPlayerPosition;

        // Search movement uses the enemy's normal movement speed.
        navMeshAgent.speed = enemyController.MoveSpeed;

        // Calculates horizontal distance to the search location.
        Vector3 flatEnemyPosition = transform.position;
        flatEnemyPosition.y = 0f;

        Vector3 flatSearchPosition = searchDestination;
        flatSearchPosition.y = 0f;

        float distanceToSearchDestination =
            Vector3.Distance(flatEnemyPosition, flatSearchPosition);

        // Stops after reaching the remembered location.
        if (distanceToSearchDestination <= searchArrivalDistance)
        {
            navMeshAgent.isStopped = true;

            if (navMeshAgent.hasPath)
            {
                navMeshAgent.ResetPath();
            }

            isSearching = true;
            isChasing = false;

            return;
        }

        // Continues moving toward the remembered player position.
        navMeshAgent.isStopped = false;

        navMeshAgent.SetDestination(searchDestination);

        RotateTowardMovementDirection();

        isSearching = false;
        isChasing = false;
    }

    #endregion


    #region Investigate Movement

    private void HandleInvestigate()
    {
        /*
         * TODO:
         * Move toward a detected sound or environmental disturbance.
         * Investigation destinations will be supplied by the future hearing system.
         */

        StopMovement();
    }

    #endregion


    #region Return Movement

    private void HandleReturn()
    {
        /*
         * TODO:
         * Replace the temporary stopped state with navigation back to the
         * patrol route after searching is complete.
         */

        StopMovement();
    }

    #endregion


    #region Movement Utility Methods

    private void StopMovement()
    {
        if (navMeshAgent == null)
        {
            return;
        }

        navMeshAgent.isStopped = true;

        // Removes the previous NavMesh path so old destinations are not retained.
        if (navMeshAgent.hasPath)
        {
            navMeshAgent.ResetPath();
        }

        isChasing = false;
        isSearching = false;

        distanceToTarget = 0f;
    }


    private void RotateTowardMovementDirection()
    {
        // Desired velocity represents the movement direction calculated by the NavMesh Agent.
        Vector3 movementDirection = navMeshAgent.desiredVelocity;

        movementDirection.y = 0f;

        // Prevents rotation calculations when the enemy is barely moving.
        if (movementDirection.sqrMagnitude <= 0.01f)
        {
            return;
        }

        Quaternion targetRotation =
            Quaternion.LookRotation(movementDirection.normalized);

        // Smoothly rotates toward the current movement direction.
        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            targetRotation,
            rotationSpeed * Time.deltaTime);
    }


    private void FaceTarget(Vector3 targetPosition)
    {
        Vector3 directionToTarget =
            targetPosition - transform.position;

        // Prevents the enemy from tilting vertically.
        directionToTarget.y = 0f;

        if (directionToTarget.sqrMagnitude <= 0.01f)
        {
            return;
        }

        Quaternion targetRotation =
            Quaternion.LookRotation(directionToTarget.normalized);

        // Smoothly rotates toward the target.
        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            targetRotation,
            rotationSpeed * Time.deltaTime);
    }

    #endregion
}