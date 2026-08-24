using UnityEngine;
using UnityEngine.AI;

/*
 * Script Name: EnemyMovement
 * Purpose:
 * Controls NavMesh-based enemy movement for each AI state.
 *
 * Current supported movement:
 * - Patrol: Moves between assigned patrol points in order.
 * - Investigate: Moves toward a perceived noise location and scans the area.
 * - Chase: Moves directly toward the detected player.
 * - Attack: Stops movement and faces the player.
 * - Search: Moves to the player's last known position.
 * - Return: Navigates back to the active patrol route.
 *
 * TODO:
 * - Randomized searching around the last known player position.
 * - More advanced patrol route selection.
 * - More complex investigation patterns.
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

    [Tooltip("Maximum horizontal angle the enemy turns left and right while scanning.")]
    [SerializeField] private float investigationScanAngle = 60f;

    [Tooltip("Controls how quickly the enemy rotates while scanning the investigation area.")]
    [SerializeField] private float investigationScanSpeed = 1.5f;

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

    [Tooltip("Facing direction recorded when the enemy reaches the investigation point.")]
    [SerializeField] private float investigationBaseYaw;

    [Tooltip("Tracks whether the investigation scan starting direction has been recorded.")]
    [SerializeField] private bool investigationScanInitialized;

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
        // Rotation is controlled manually for more predictable enemy behavior.
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
        investigationScanInitialized = false;

        if (enemyPatrolRoute == null)
        {
            StopMovement();
            return;
        }

        if (!enemyPatrolRoute.HasPatrolPoints)
        {
            StopMovement();
            return;
        }

        Transform currentPatrolPoint = enemyPatrolRoute.CurrentPatrolPoint;

        if (currentPatrolPoint == null)
        {
            StopMovement();
            return;
        }

        patrolDestination = currentPatrolPoint.position;

        navMeshAgent.speed = enemyController.MoveSpeed;
        navMeshAgent.isStopped = false;
        navMeshAgent.SetDestination(patrolDestination);

        RotateTowardMovementDirection();

        float distanceToPatrolPoint =
            GetHorizontalDistance(
                transform.position,
                patrolDestination);

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

        if (enemyPerception == null)
        {
            StopMovement();
            return;
        }

        if (!enemyPerception.HasInvestigationTarget)
        {
            StopMovement();
            return;
        }

        investigationDestination =
            enemyPerception.InvestigationPosition;

        navMeshAgent.speed = enemyController.MoveSpeed;

        float distanceToInvestigationPosition =
            GetHorizontalDistance(
                transform.position,
                investigationDestination);

        // Continues traveling until the disturbance position is reached.
        if (distanceToInvestigationPosition > investigationArrivalDistance)
        {
            hasReachedInvestigationPosition = false;
            investigationScanInitialized = false;

            navMeshAgent.isStopped = false;
            navMeshAgent.SetDestination(investigationDestination);

            RotateTowardMovementDirection();

            isChasing = false;
            isSearching = false;

            return;
        }

        // Stops after arriving at the noise location.
        navMeshAgent.isStopped = true;

        if (navMeshAgent.hasPath)
        {
            navMeshAgent.ResetPath();
        }

        hasReachedInvestigationPosition = true;

        // Records the direction the enemy was facing upon arrival.
        if (!investigationScanInitialized)
        {
            investigationBaseYaw = transform.eulerAngles.y;
            investigationScanInitialized = true;
        }

        // Rotates left and right while the investigation timer runs.
        PerformInvestigationScan();

        isChasing = false;
        isSearching = false;
    }


    private void PerformInvestigationScan()
    {
        // PingPong creates a repeating value that moves smoothly
        // between zero and twice the configured scan angle.
        float scanOffset =
            Mathf.PingPong(
                Time.time * investigationScanSpeed * investigationScanAngle,
                investigationScanAngle * 2f)
            - investigationScanAngle;

        // Applies the scan offset relative to the direction the enemy
        // was facing when the disturbance location was reached.
        float targetYaw =
            investigationBaseYaw + scanOffset;

        Quaternion targetRotation =
            Quaternion.Euler(
                0f,
                targetYaw,
                0f);

        // Smoothly rotates toward the current scan direction.
        transform.rotation =
            Quaternion.Slerp(
                transform.rotation,
                targetRotation,
                rotationSpeed * Time.deltaTime);
    }

    #endregion


    #region Chase Movement

    private void HandleChase()
    {
        hasReturnedToPatrolRoute = false;
        hasReachedInvestigationPosition = false;
        investigationScanInitialized = false;

        Transform target = enemyController.CurrentTarget;

        if (target == null)
        {
            StopMovement();
            return;
        }

        distanceToTarget =
            Vector3.Distance(
                transform.position,
                target.position);

        if (distanceToTarget <= enemyController.AttackRange)
        {
            navMeshAgent.isStopped = true;

            FaceTarget(target.position);

            isChasing = false;

            return;
        }

        navMeshAgent.speed = enemyController.ChaseSpeed;
        navMeshAgent.isStopped = false;
        navMeshAgent.SetDestination(target.position);

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
        investigationScanInitialized = false;

        Transform target = enemyController.CurrentTarget;

        StopMovement();

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
        investigationScanInitialized = false;

        if (enemyDetection == null)
        {
            StopMovement();
            return;
        }

        if (!enemyDetection.HasLastKnownPosition)
        {
            StopMovement();
            return;
        }

        searchDestination =
            enemyDetection.LastKnownPlayerPosition;

        navMeshAgent.speed = enemyController.MoveSpeed;

        float distanceToSearchDestination =
            GetHorizontalDistance(
                transform.position,
                searchDestination);

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
        investigationScanInitialized = false;

        if (enemyPatrolRoute == null)
        {
            StopMovement();
            return;
        }

        if (!enemyPatrolRoute.HasPatrolPoints)
        {
            StopMovement();
            return;
        }

        Transform currentPatrolPoint =
            enemyPatrolRoute.CurrentPatrolPoint;

        if (currentPatrolPoint == null)
        {
            StopMovement();
            return;
        }

        returnDestination =
            currentPatrolPoint.position;

        navMeshAgent.speed =
            enemyController.MoveSpeed;

        float distanceToReturnDestination =
            GetHorizontalDistance(
                transform.position,
                returnDestination);

        if (distanceToReturnDestination <= returnArrivalDistance)
        {
            navMeshAgent.isStopped = true;

            if (navMeshAgent.hasPath)
            {
                navMeshAgent.ResetPath();
            }

            hasReturnedToPatrolRoute = true;

            isChasing = false;
            isSearching = false;

            return;
        }

        hasReturnedToPatrolRoute = false;

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

        navMeshAgent.isStopped = true;

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
        firstPosition.y = 0f;
        secondPosition.y = 0f;

        return Vector3.Distance(
            firstPosition,
            secondPosition);
    }


    private void RotateTowardMovementDirection()
    {
        Vector3 movementDirection =
            navMeshAgent.desiredVelocity;

        movementDirection.y = 0f;

        if (movementDirection.sqrMagnitude <= 0.01f)
        {
            return;
        }

        Quaternion targetRotation =
            Quaternion.LookRotation(
                movementDirection.normalized);

        transform.rotation =
            Quaternion.Slerp(
                transform.rotation,
                targetRotation,
                rotationSpeed * Time.deltaTime);
    }


    private void FaceTarget(Vector3 targetPosition)
    {
        Vector3 directionToTarget =
            targetPosition - transform.position;

        directionToTarget.y = 0f;

        if (directionToTarget.sqrMagnitude <= 0.01f)
        {
            return;
        }

        Quaternion targetRotation =
            Quaternion.LookRotation(
                directionToTarget.normalized);

        transform.rotation =
            Quaternion.Slerp(
                transform.rotation,
                targetRotation,
                rotationSpeed * Time.deltaTime);
    }

    #endregion
}