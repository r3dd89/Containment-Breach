using UnityEngine;
using UnityEngine.AI;

/*
 * Script Name: EnemyMovement
 * Purpose:
 * Controls NavMesh-based enemy movement for each AI state.
 *
 * Current supported movement:
 * - Patrol: Moves between assigned patrol points in order.
 * - Investigate: Moves toward a perceived noise location.
 * - Chase: Moves directly toward the detected player.
 * - Attack: Stops movement and faces the player.
 * - Search: Moves to the player's last known position.
 * - Return: Navigates back to the active patrol route.
 *
 * Future movement:
 * - Randomized searching around the last known player position.
 * - More advanced patrol route selection.
 * - More complex investigation behavior after reaching a disturbance.
 */

public class EnemyMovement : MonoBehaviour
{
    #region Component References

    [Header("Component References")]

    [Tooltip("Provides access to enemy statistics and the current player target.")]
    [SerializeField] private EnemyController enemyController;

    [Tooltip("Provides visual detection information including the player's last known position.")]
    [SerializeField] private EnemyDetection enemyDetection;

    [Tooltip("Provides access to the enemy's current AI state.")]
    [SerializeField] private EnemyStateMachine enemyStateMachine;

    [Tooltip("Provides non-visual perception information such as investigation positions.")]
    [SerializeField] private EnemyPerception enemyPerception;

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


    #region Investigation Settings

    [Header("Investigation Settings")]

    [Tooltip("Distance from an investigation position that counts as reaching the disturbance.")]
    [SerializeField] private float investigationArrivalDistance = 0.75f;

    #endregion


    #region Search Settings

    [Header("Search Settings")]

    [Tooltip("Distance from the last known player position that counts as reaching the search destination.")]
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

    [Tooltip("True after the enemy reaches its investigation destination.")]
    [SerializeField] private bool hasReachedInvestigationPosition;

    [Tooltip("True after the enemy reaches its search destination.")]
    [SerializeField] private bool isSearching;

    [Tooltip("True after the enemy successfully reaches its patrol route during Return.")]
    [SerializeField] private bool hasReturnedToPatrolRoute;

    [Tooltip("Current destination used during the Investigate state.")]
    [SerializeField] private Vector3 investigationDestination;

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

    public bool HasReachedInvestigationPosition
    {
        get { return hasReachedInvestigationPosition; }
    }

    public bool IsSearching
    {
        get { return isSearching; }
    }

    public bool HasReturnedToPatrolRoute
    {
        get { return hasReturnedToPatrolRoute; }
    }

    public Vector3 InvestigationDestination
    {
        get { return investigationDestination; }
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
        // Finds EnemyController automatically if the Inspector reference was not assigned.
        if (enemyController == null)
        {
            enemyController = GetComponent<EnemyController>();
        }

        // Finds EnemyDetection automatically if needed.
        if (enemyDetection == null)
        {
            enemyDetection = GetComponent<EnemyDetection>();
        }

        // Finds EnemyStateMachine automatically if needed.
        if (enemyStateMachine == null)
        {
            enemyStateMachine = GetComponent<EnemyStateMachine>();
        }

        // Finds EnemyPerception automatically if needed.
        if (enemyPerception == null)
        {
            enemyPerception = GetComponent<EnemyPerception>();
        }

        // Finds the NavMeshAgent automatically if needed.
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
        // to rotate the enemy automatically.
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

        // NavMesh movement cannot be used unless the enemy is standing
        // on a baked NavMesh surface.
        if (!navMeshAgent.isOnNavMesh)
        {
            return;
        }

        // Runs movement behavior based on the current AI state.
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
        hasReturnedToPatrolRoute = false;
        hasReachedInvestigationPosition = false;

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

        // Gets the patrol point currently selected by the route.
        Transform currentPatrolPoint = enemyPatrolRoute.CurrentPatrolPoint;

        // Stops safely if the current patrol point reference is missing.
        if (currentPatrolPoint == null)
        {
            StopMovement();
            return;
        }

        // Stores the current patrol destination for Inspector debugging.
        patrolDestination = currentPatrolPoint.position;

        // Patrol uses normal movement speed.
        navMeshAgent.speed = enemyController.MoveSpeed;

        // Allows the NavMeshAgent to move.
        navMeshAgent.isStopped = false;

        // Sends the enemy toward the active patrol point.
        navMeshAgent.SetDestination(patrolDestination);

        // Rotates toward the calculated movement direction.
        RotateTowardMovementDirection();

        // Calculates horizontal distance to the patrol point.
        float distanceToPatrolPoint =
            GetHorizontalDistance(transform.position, patrolDestination);

        // Advances to the next patrol point after arriving.
        if (distanceToPatrolPoint <= patrolArrivalDistance)
        {
            enemyPatrolRoute.AdvanceToNextPatrolPoint();
        }

        isChasing = false;
        isSearching = false;
    }

    #endregion


    #region Investigation Movement

    private void HandleInvestigate()
    {
        hasReturnedToPatrolRoute = false;

        // Investigation requires an active perception component.
        if (enemyPerception == null)
        {
            StopMovement();
            return;
        }

        // Stops safely if there is no active disturbance to investigate.
        if (!enemyPerception.HasInvestigationTarget)
        {
            StopMovement();
            return;
        }

        // Copies the currently stored disturbance location.
        investigationDestination =
            enemyPerception.InvestigationPosition;

        // Investigation uses normal movement speed.
        navMeshAgent.speed = enemyController.MoveSpeed;

        // Calculates horizontal distance to the investigation location.
        float distanceToInvestigationPosition =
            GetHorizontalDistance(
                transform.position,
                investigationDestination);

        // Stops after reaching the source of the disturbance.
        if (distanceToInvestigationPosition <= investigationArrivalDistance)
        {
            navMeshAgent.isStopped = true;

            // Removes the completed NavMesh path.
            if (navMeshAgent.hasPath)
            {
                navMeshAgent.ResetPath();
            }

            // Reports arrival for debugging and future state-machine logic.
            hasReachedInvestigationPosition = true;

            isChasing = false;
            isSearching = false;

            return;
        }

        // The investigation location has not been reached yet.
        hasReachedInvestigationPosition = false;

        // Allows movement toward the disturbance.
        navMeshAgent.isStopped = false;

        // Sends the enemy toward the stored perception position.
        navMeshAgent.SetDestination(investigationDestination);

        // Rotates naturally toward the NavMesh movement direction.
        RotateTowardMovementDirection();

        isChasing = false;
        isSearching = false;
    }

    #endregion


    #region Chase Movement

    private void HandleChase()
    {
        hasReturnedToPatrolRoute = false;
        hasReachedInvestigationPosition = false;

        Transform target = enemyController.CurrentTarget;

        // Stops movement if there is no valid player target.
        if (target == null)
        {
            StopMovement();
            return;
        }

        // Calculates the distance between the enemy and player.
        distanceToTarget =
            Vector3.Distance(transform.position, target.position);

        // Stops advancing once the target is inside attack range.
        if (distanceToTarget <= enemyController.AttackRange)
        {
            navMeshAgent.isStopped = true;

            // Keeps the enemy facing the target.
            FaceTarget(target.position);

            isChasing = false;

            return;
        }

        // Chase uses the faster chase speed stored in EnemyData.
        navMeshAgent.speed = enemyController.ChaseSpeed;

        // Allows NavMesh movement.
        navMeshAgent.isStopped = false;

        // Continuously follows the moving player.
        navMeshAgent.SetDestination(target.position);

        // Rotates toward the current movement direction.
        RotateTowardMovementDirection();

        isChasing = true;
        isSearching = false;
    }

    #endregion


    #region Attack Movement

    private void HandleAttackPosition()
    {
        hasReturnedToPatrolRoute = false;
        hasReachedInvestigationPosition = false;

        Transform target = enemyController.CurrentTarget;

        // Keeps the enemy stationary during attack behavior.
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
        hasReturnedToPatrolRoute = false;
        hasReachedInvestigationPosition = false;

        // Search requires visual detection memory.
        if (enemyDetection == null)
        {
            StopMovement();
            return;
        }

        // Stops safely if no previous player position exists.
        if (!enemyDetection.HasLastKnownPosition)
        {
            StopMovement();
            return;
        }

        // Uses the player's last visible location.
        searchDestination = enemyDetection.LastKnownPlayerPosition;

        // Search uses normal movement speed.
        navMeshAgent.speed = enemyController.MoveSpeed;

        // Calculates horizontal distance to the search destination.
        float distanceToSearchDestination =
            GetHorizontalDistance(
                transform.position,
                searchDestination);

        // Stops after reaching the remembered location.
        if (distanceToSearchDestination <= searchArrivalDistance)
        {
            navMeshAgent.isStopped = true;

            // Removes the completed navigation path.
            if (navMeshAgent.hasPath)
            {
                navMeshAgent.ResetPath();
            }

            isSearching = true;
            isChasing = false;

            return;
        }

        // Continues toward the remembered player location.
        navMeshAgent.isStopped = false;

        navMeshAgent.SetDestination(searchDestination);

        RotateTowardMovementDirection();

        isSearching = false;
        isChasing = false;
    }

    #endregion


    #region Return Movement

    private void HandleReturn()
    {
        hasReachedInvestigationPosition = false;

        // Return behavior requires a patrol route.
        if (enemyPatrolRoute == null)
        {
            StopMovement();
            return;
        }

        // A patrol route without points cannot provide a return destination.
        if (!enemyPatrolRoute.HasPatrolPoints)
        {
            StopMovement();
            return;
        }

        // Uses the currently selected patrol point as the return destination.
        Transform currentPatrolPoint = enemyPatrolRoute.CurrentPatrolPoint;

        // Stops safely if the patrol point reference is missing.
        if (currentPatrolPoint == null)
        {
            StopMovement();
            return;
        }

        // Stores the destination for Inspector debugging.
        returnDestination = currentPatrolPoint.position;

        // Return uses normal movement speed.
        navMeshAgent.speed = enemyController.MoveSpeed;

        // Calculates horizontal distance to the patrol route.
        float distanceToReturnDestination =
            GetHorizontalDistance(
                transform.position,
                returnDestination);

        // Stops once the patrol route has been reached.
        if (distanceToReturnDestination <= returnArrivalDistance)
        {
            navMeshAgent.isStopped = true;

            // Removes the completed path.
            if (navMeshAgent.hasPath)
            {
                navMeshAgent.ResetPath();
            }

            // Reports successful return to EnemyStateMachine.
            hasReturnedToPatrolRoute = true;

            isChasing = false;
            isSearching = false;

            return;
        }

        // The patrol route has not been reached yet.
        hasReturnedToPatrolRoute = false;

        // Allows movement toward the patrol route.
        navMeshAgent.isStopped = false;

        navMeshAgent.SetDestination(returnDestination);

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

        // Clears any previous path still stored by the agent.
        if (navMeshAgent.hasPath)
        {
            navMeshAgent.ResetPath();
        }

        isChasing = false;
        isSearching = false;

        distanceToTarget = 0f;
    }


    private float GetHorizontalDistance(
        Vector3 firstPosition,
        Vector3 secondPosition)
    {
        // Removes vertical differences because navigation occurs across the floor plane.
        firstPosition.y = 0f;
        secondPosition.y = 0f;

        // Returns the horizontal distance between the two positions.
        return Vector3.Distance(
            firstPosition,
            secondPosition);
    }


    private void RotateTowardMovementDirection()
    {
        // Desired velocity represents the direction calculated by NavMesh pathfinding.
        Vector3 movementDirection = navMeshAgent.desiredVelocity;

        // Prevents vertical rotation.
        movementDirection.y = 0f;

        // Avoids unnecessary rotation when movement is effectively zero.
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
        // Calculates the horizontal direction toward the target.
        Vector3 directionToTarget =
            targetPosition - transform.position;

        // Prevents upward or downward tilting.
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