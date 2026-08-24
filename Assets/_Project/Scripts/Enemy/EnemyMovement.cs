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
 * - Return: Navigates back to the active patrol route.
 *
 * Future movement:
 * - Investigate noise sources.
 * - Randomized searching around the last known player position.
 * - More advanced patrol route selection.
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


    #region Return Settings

    [Header("Return Settings")]

    [Tooltip("Distance from the patrol route that counts as successfully returning.")]
    [SerializeField] private float returnArrivalDistance = 0.75f;

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

    [Tooltip("True after the enemy reaches its search destination.")]
    [SerializeField] private bool isSearching;

    [Tooltip("True after the enemy successfully reaches its patrol route during Return.")]
    [SerializeField] private bool hasReturnedToPatrolRoute;

    [Tooltip("Current destination used during the Search state.")]
    [SerializeField] private Vector3 searchDestination;

    [Tooltip("Current patrol destination used during the Patrol state.")]
    [SerializeField] private Vector3 patrolDestination;

    [Tooltip("Current destination used during the Return state.")]
    [SerializeField] private Vector3 returnDestination;

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

    public bool HasReturnedToPatrolRoute
    {
        get { return hasReturnedToPatrolRoute; }
    }

    public Vector3 SearchDestination
    {
        get { return searchDestination; }
    }

    public Vector3 PatrolDestination
    {
        get { return patrolDestination; }
    }

    public Vector3 ReturnDestination
    {
        get { return returnDestination; }
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

        // Finds the patrol route attached to this enemy if needed.
        if (enemyPatrolRoute == null)
        {
            enemyPatrolRoute = GetComponent<EnemyPatrolRoute>();
        }
    }


    private void Start()
    {
        // Rotation is handled manually instead of allowing the NavMeshAgent
        // to automatically rotate the enemy.
        if (navMeshAgent != null)
        {
            navMeshAgent.updateRotation = false;
        }
    }


    private void Update()
    {
        // Movement cannot function without these required components.
        if (enemyController == null ||
            enemyStateMachine == null ||
            navMeshAgent == null)
        {
            return;
        }

        // NavMesh movement cannot be used unless the enemy is currently
        // positioned on a baked NavMesh surface.
        if (!navMeshAgent.isOnNavMesh)
        {
            return;
        }

        // Runs the appropriate movement behavior for the current AI state.
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
        // The enemy is no longer considered to be in the process of returning
        // once normal patrol behavior begins.
        hasReturnedToPatrolRoute = false;

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

        // Gets the patrol point currently selected by the patrol route.
        Transform currentPatrolPoint = enemyPatrolRoute.CurrentPatrolPoint;

        // Stops safely if the current patrol point reference is missing.
        if (currentPatrolPoint == null)
        {
            StopMovement();
            return;
        }

        // Stores the destination for Inspector debugging.
        patrolDestination = currentPatrolPoint.position;

        // Patrol uses the enemy's standard movement speed.
        navMeshAgent.speed = enemyController.MoveSpeed;

        // Allows the NavMeshAgent to move.
        navMeshAgent.isStopped = false;

        // Sends the enemy toward the active patrol point.
        navMeshAgent.SetDestination(patrolDestination);

        // Rotates the enemy toward the direction calculated by the NavMesh.
        RotateTowardMovementDirection();

        // Calculates horizontal distance to the patrol point.
        Vector3 flatEnemyPosition = transform.position;
        flatEnemyPosition.y = 0f;

        Vector3 flatPatrolPosition = patrolDestination;
        flatPatrolPosition.y = 0f;

        float distanceToPatrolPoint =
            Vector3.Distance(flatEnemyPosition, flatPatrolPosition);

        // Advances to the next point after reaching the current point.
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
        // Return completion must be cleared if the enemy begins chasing again.
        hasReturnedToPatrolRoute = false;

        Transform target = enemyController.CurrentTarget;

        // Stops movement if there is no longer a valid player target.
        if (target == null)
        {
            StopMovement();
            return;
        }

        // Calculates the current distance between the enemy and the player.
        distanceToTarget =
            Vector3.Distance(transform.position, target.position);

        // Stops advancing after the target reaches attack range.
        if (distanceToTarget <= enemyController.AttackRange)
        {
            navMeshAgent.isStopped = true;

            // Keeps the enemy facing the player while preparing to attack.
            FaceTarget(target.position);

            isChasing = false;

            return;
        }

        // Chase uses the faster chase speed stored in EnemyData.
        navMeshAgent.speed = enemyController.ChaseSpeed;

        // Allows movement.
        navMeshAgent.isStopped = false;

        // Continuously updates the destination because the player can move.
        navMeshAgent.SetDestination(target.position);

        // Rotates toward the active NavMesh movement direction.
        RotateTowardMovementDirection();

        isChasing = true;
        isSearching = false;
    }

    #endregion


    #region Attack Movement

    private void HandleAttackPosition()
    {
        hasReturnedToPatrolRoute = false;

        Transform target = enemyController.CurrentTarget;

        // Attack behavior remains stationary while EnemyAttack handles damage.
        StopMovement();

        // Keeps the enemy facing the player while attacking.
        if (target != null)
        {
            FaceTarget(target.position);
        }
    }

    #endregion


    #region Search Movement

    private void HandleSearch()
    {
        hasReturnedToPatrolRoute = false;

        // Search behavior requires detection memory.
        if (enemyDetection == null)
        {
            StopMovement();
            return;
        }

        // Stops safely if the enemy has never recorded a player location.
        if (!enemyDetection.HasLastKnownPosition)
        {
            StopMovement();
            return;
        }

        // Uses the last location where the player was visible.
        searchDestination = enemyDetection.LastKnownPlayerPosition;

        // Search movement uses normal movement speed.
        navMeshAgent.speed = enemyController.MoveSpeed;

        // Calculates horizontal distance to the remembered location.
        Vector3 flatEnemyPosition = transform.position;
        flatEnemyPosition.y = 0f;

        Vector3 flatSearchPosition = searchDestination;
        flatSearchPosition.y = 0f;

        float distanceToSearchDestination =
            Vector3.Distance(flatEnemyPosition, flatSearchPosition);

        // Stops when the remembered player position has been reached.
        if (distanceToSearchDestination <= searchArrivalDistance)
        {
            navMeshAgent.isStopped = true;

            // Clears the completed navigation path.
            if (navMeshAgent.hasPath)
            {
                navMeshAgent.ResetPath();
            }

            isSearching = true;
            isChasing = false;

            return;
        }

        // Continues moving toward the last known player position.
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

        hasReturnedToPatrolRoute = false;

        StopMovement();
    }

    #endregion


    #region Return Movement

    private void HandleReturn()
    {
        // Return behavior requires a valid patrol route.
        if (enemyPatrolRoute == null)
        {
            StopMovement();
            return;
        }

        // A route with no assigned points cannot provide a return destination.
        if (!enemyPatrolRoute.HasPatrolPoints)
        {
            StopMovement();
            return;
        }

        // Uses the currently selected patrol point as the return destination.
        Transform currentPatrolPoint = enemyPatrolRoute.CurrentPatrolPoint;

        // Stops safely if the current patrol point reference is missing.
        if (currentPatrolPoint == null)
        {
            StopMovement();
            return;
        }

        // Stores the destination for Inspector debugging.
        returnDestination = currentPatrolPoint.position;

        // Return movement uses the enemy's normal movement speed.
        navMeshAgent.speed = enemyController.MoveSpeed;

        // Calculates horizontal distance to the route.
        Vector3 flatEnemyPosition = transform.position;
        flatEnemyPosition.y = 0f;

        Vector3 flatReturnPosition = returnDestination;
        flatReturnPosition.y = 0f;

        float distanceToReturnDestination =
            Vector3.Distance(flatEnemyPosition, flatReturnPosition);

        // Stops once the active patrol point has been reached.
        if (distanceToReturnDestination <= returnArrivalDistance)
        {
            navMeshAgent.isStopped = true;

            // Removes the completed path from the NavMeshAgent.
            if (navMeshAgent.hasPath)
            {
                navMeshAgent.ResetPath();
            }

            // Reports successful arrival to the EnemyStateMachine.
            hasReturnedToPatrolRoute = true;

            isChasing = false;
            isSearching = false;

            return;
        }

        // The enemy has not reached the patrol route yet.
        hasReturnedToPatrolRoute = false;

        // Allows the NavMeshAgent to begin moving.
        navMeshAgent.isStopped = false;

        // Navigates back toward the currently active patrol point.
        navMeshAgent.SetDestination(returnDestination);

        // Rotates naturally toward the calculated movement direction.
        RotateTowardMovementDirection();

        isChasing = false;
        isSearching = false;
    }

    #endregion


    #region Movement Utility Methods

    private void StopMovement()
    {
        if (navMeshAgent == null)
        {
            return;
        }

        // Prevents the NavMeshAgent from continuing along its current route.
        navMeshAgent.isStopped = true;

        // Clears any previous navigation path that is still stored.
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
        // Desired velocity represents the movement direction calculated
        // by the NavMeshAgent after pathfinding.
        Vector3 movementDirection = navMeshAgent.desiredVelocity;

        // Removes vertical rotation.
        movementDirection.y = 0f;

        // Prevents unnecessary rotation calculations when barely moving.
        if (movementDirection.sqrMagnitude <= 0.01f)
        {
            return;
        }

        Quaternion targetRotation =
            Quaternion.LookRotation(movementDirection.normalized);

        // Smoothly rotates the enemy toward its current movement direction.
        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            targetRotation,
            rotationSpeed * Time.deltaTime);
    }


    private void FaceTarget(Vector3 targetPosition)
    {
        // Calculates the direction from the enemy toward the target.
        Vector3 directionToTarget =
            targetPosition - transform.position;

        // Prevents the enemy from tilting upward or downward.
        directionToTarget.y = 0f;

        // Avoids creating a rotation from an effectively zero direction.
        if (directionToTarget.sqrMagnitude <= 0.01f)
        {
            return;
        }

        Quaternion targetRotation =
            Quaternion.LookRotation(directionToTarget.normalized);

        // Smoothly rotates the enemy toward the target.
        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            targetRotation,
            rotationSpeed * Time.deltaTime);
    }

    #endregion
}