using UnityEngine;
using UnityEngine.AI;

/*
 * Script Name: EnemyMovement
 * Purpose:
 * Handles enemy navigation and movement based on
 * the current AI state.
 *
 * This script uses:
 * - EnemyController for target information and EnemyData values
 * - EnemyDetection for the player's last known position
 * - EnemyStateMachine for the current AI state
 * - NavMeshAgent for pathfinding
 *
 * This script currently handles:
 * - Standing still during states without movement behavior
 * - Chasing a detected player
 * - Using chase speed from EnemyData
 * - Stopping when the player enters attack range
 * - Moving toward the player's last known position during Search
 * - Stopping after reaching the last known position
 * - Rotating toward movement direction
 * - Rotating toward the player while inside attack range
 */

public class EnemyMovement : MonoBehaviour
{
    #region Component References

    /*
     * REGION: Component References
     *
     * This region stores references to the enemy systems
     * required for navigation and state-aware movement.
     */

    [Header("Component References")]

    [SerializeField]
    [Tooltip("The EnemyController that provides target information and EnemyData values.")]
    private EnemyController enemyController;

    [SerializeField]
    [Tooltip("The EnemyDetection component that stores the player's last known position.")]
    private EnemyDetection enemyDetection;

    [SerializeField]
    [Tooltip("The EnemyStateMachine that provides the enemy's current AI state.")]
    private EnemyStateMachine enemyStateMachine;

    [SerializeField]
    [Tooltip("The NavMeshAgent used to move the enemy across the NavMesh.")]
    private NavMeshAgent navMeshAgent;

    #endregion


    #region Movement Settings

    /*
     * REGION: Movement Settings
     *
     * This region contains additional navigation values
     * used by state-specific movement.
     */

    [Header("Movement Settings")]

    [SerializeField]
    [Min(0f)]
    [Tooltip("How close the enemy must get to a search destination before considering it reached.")]
    private float searchArrivalDistance = 0.75f;

    #endregion


    #region Rotation Settings

    /*
     * REGION: Rotation Settings
     *
     * These values control how quickly the enemy
     * rotates toward movement directions and targets.
     */

    [Header("Rotation Settings")]

    [SerializeField]
    [Tooltip("How quickly the enemy rotates toward its movement direction or current target.")]
    private float rotationSpeed = 8f;

    #endregion


    #region Runtime State

    /*
     * REGION: Runtime State
     *
     * These values expose current movement information
     * in the Inspector during gameplay.
     */

    [Header("Runtime State")]

    [SerializeField]
    [Tooltip("Shows the current distance between the enemy and its detected target.")]
    private float distanceToTarget;

    [SerializeField]
    [Tooltip("Shows whether the enemy is currently chasing its target.")]
    private bool isChasing;

    [SerializeField]
    [Tooltip("Shows whether the enemy is currently moving toward a search location.")]
    private bool isSearching;

    [SerializeField]
    [Tooltip("Shows the current destination used by Search behavior.")]
    private Vector3 searchDestination;

    #endregion


    #region Public Properties

    /*
     * REGION: Public Properties
     *
     * These properties allow other systems
     * to safely read movement state.
     */

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

    #endregion


    #region Unity Methods

    /*
     * REGION: Unity Methods
     *
     * Unity automatically calls these methods.
     */

    private void Awake()
    {
        /*
         * Automatically find EnemyController
         * if it was not assigned manually.
         */
        if (enemyController == null)
        {
            enemyController = GetComponent<EnemyController>();
        }

        /*
         * Automatically find EnemyDetection
         * if it was not assigned manually.
         */
        if (enemyDetection == null)
        {
            enemyDetection = GetComponent<EnemyDetection>();
        }

        /*
         * Automatically find EnemyStateMachine
         * if it was not assigned manually.
         */
        if (enemyStateMachine == null)
        {
            enemyStateMachine = GetComponent<EnemyStateMachine>();
        }

        /*
         * Automatically find NavMeshAgent
         * if it was not assigned manually.
         */
        if (navMeshAgent == null)
        {
            navMeshAgent = GetComponent<NavMeshAgent>();
        }
    }


    private void Start()
    {
        /*
         * Stop setup if the NavMeshAgent is missing.
         */
        if (navMeshAgent == null)
        {
            return;
        }

        /*
         * Disable NavMeshAgent automatic rotation.
         *
         * Rotation is controlled manually so different
         * AI states can control facing behavior consistently.
         */
        navMeshAgent.updateRotation = false;
    }


    private void Update()
    {
        /*
         * Stop movement logic if required
         * components are missing.
         */
        if (enemyController == null ||
            enemyStateMachine == null ||
            navMeshAgent == null)
        {
            return;
        }

        /*
         * Stop navigation logic if the agent
         * is not currently placed on a NavMesh.
         */
        if (!navMeshAgent.isOnNavMesh)
        {
            return;
        }

        /*
         * Run movement behavior based on
         * the enemy's current AI state.
         */
        switch (enemyStateMachine.CurrentState)
        {
            case EnemyState.Chase:
                HandleChase();
                break;

            case EnemyState.Attack:
                HandleAttackPosition();
                break;

            case EnemyState.Search:
                HandleSearch();
                break;

            case EnemyState.Patrol:
                StopMovement();
                break;

            case EnemyState.Investigate:
                StopMovement();
                break;

            case EnemyState.Return:
                StopMovement();
                break;
        }
    }

    #endregion


    #region Chase Methods

    /*
     * REGION: Chase Methods
     *
     * This region handles navigation toward
     * a currently detected target.
     */

    private void HandleChase()
    {
        /*
         * Stop chasing if the current target
         * is no longer available.
         */
        if (!enemyController.HasDetectedTarget ||
            enemyController.CurrentTarget == null)
        {
            StopMovement();

            return;
        }

        /*
         * Store the current target reference.
         */
        Transform target =
            enemyController.CurrentTarget;

        /*
         * Calculate the current distance
         * between the enemy and target.
         */
        distanceToTarget = Vector3.Distance(
            transform.position,
            target.position
        );

        /*
         * Stop moving once the target
         * enters attack range.
         */
        if (distanceToTarget <=
            enemyController.AttackRange)
        {
            StopMovement();

            RotateTowardTarget(
                target.position
            );

            return;
        }

        /*
         * Mark the enemy as actively chasing.
         */
        isChasing = true;
        isSearching = false;

        /*
         * Use the chase speed configured
         * in the assigned EnemyData asset.
         */
        navMeshAgent.speed =
            enemyController.ChaseSpeed;

        /*
         * Update the NavMesh destination
         * to the target's current position.
         */
        navMeshAgent.SetDestination(
            target.position
        );

        /*
         * Face the direction of travel.
         */
        RotateTowardMovementDirection();
    }

    #endregion


    #region Attack Position Methods

    /*
     * REGION: Attack Position Methods
     *
     * This region handles movement behavior
     * while the enemy is in the Attack state.
     */

    private void HandleAttackPosition()
    {
        /*
         * Stop navigation while attacking.
         */
        StopMovement();

        /*
         * Face the current target if one exists.
         */
        if (enemyController.CurrentTarget != null)
        {
            RotateTowardTarget(
                enemyController.CurrentTarget.position
            );
        }
    }

    #endregion


    #region Search Methods

    /*
     * REGION: Search Methods
     *
     * This region handles movement toward
     * the player's last confirmed position.
     */

    private void HandleSearch()
    {
        /*
         * Search movement requires a valid
         * last known player position.
         */
        if (enemyDetection == null ||
            !enemyDetection.HasLastKnownPosition)
        {
            StopMovement();

            return;
        }

        /*
         * Store the player's last confirmed position
         * as the current search destination.
         */
        searchDestination =
            enemyDetection.LastKnownPlayerPosition;

        /*
         * Calculate the horizontal distance
         * to the search destination.
         *
         * Vertical differences are ignored because
         * NavMesh navigation handles surface height.
         */
        Vector3 enemyPosition =
            transform.position;

        Vector3 flatSearchDestination =
            searchDestination;

        enemyPosition.y = 0f;
        flatSearchDestination.y = 0f;

        float distanceToSearchDestination =
            Vector3.Distance(
                enemyPosition,
                flatSearchDestination
            );

        /*
         * Stop after reaching the last
         * known player position.
         *
         * The state machine's Search timer continues,
         * allowing the enemy to remain alert before
         * transitioning to Return.
         */
        if (distanceToSearchDestination <=
            searchArrivalDistance)
        {
            StopMovement();

            isSearching = true;

            return;
        }

        /*
         * Mark the enemy as actively searching.
         */
        isChasing = false;
        isSearching = true;

        /*
         * Use normal movement speed while searching.
         *
         * Search movement is intentionally slower
         * than active pursuit.
         */
        navMeshAgent.speed =
            enemyController.MoveSpeed;

        /*
         * Navigate toward the last confirmed
         * player position.
         */
        navMeshAgent.SetDestination(
            searchDestination
        );

        /*
         * Face the current movement direction.
         */
        RotateTowardMovementDirection();
    }

    #endregion


    #region Movement Control Methods

    /*
     * REGION: Movement Control Methods
     *
     * This region handles stopping navigation
     * and clearing current movement state.
     */

    private void StopMovement()
    {
        /*
         * Stop movement-state indicators.
         */
        isChasing = false;
        isSearching = false;

        /*
         * Stop here if the NavMeshAgent
         * is not currently on a NavMesh.
         */
        if (navMeshAgent == null ||
            !navMeshAgent.isOnNavMesh)
        {
            return;
        }

        /*
         * Clear the active NavMesh path.
         */
        navMeshAgent.ResetPath();
    }

    #endregion


    #region Rotation Methods

    /*
     * REGION: Rotation Methods
     *
     * This region controls how the enemy rotates
     * while navigating or facing a target.
     */

    private void RotateTowardMovementDirection()
    {
        /*
         * Stop if the agent is barely moving.
         */
        if (navMeshAgent.velocity.sqrMagnitude <= 0.01f)
        {
            return;
        }

        /*
         * Use the current NavMesh velocity
         * as the desired facing direction.
         */
        Vector3 movementDirection =
            navMeshAgent.velocity.normalized;

        /*
         * Ignore vertical rotation.
         */
        movementDirection.y = 0f;

        /*
         * Stop if no useful horizontal
         * direction remains.
         */
        if (movementDirection.sqrMagnitude <= 0.001f)
        {
            return;
        }

        /*
         * Create the desired facing rotation.
         */
        Quaternion targetRotation =
            Quaternion.LookRotation(
                movementDirection
            );

        /*
         * Smoothly rotate toward
         * the movement direction.
         */
        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            targetRotation,
            rotationSpeed * Time.deltaTime
        );
    }


    private void RotateTowardTarget(
        Vector3 targetPosition
    )
    {
        /*
         * Calculate the horizontal direction
         * from the enemy toward the target.
         */
        Vector3 directionToTarget =
            targetPosition -
            transform.position;

        /*
         * Ignore vertical difference.
         */
        directionToTarget.y = 0f;

        /*
         * Stop if the direction is too small
         * to produce a valid rotation.
         */
        if (directionToTarget.sqrMagnitude <= 0.001f)
        {
            return;
        }

        /*
         * Create the desired facing rotation.
         */
        Quaternion targetRotation =
            Quaternion.LookRotation(
                directionToTarget.normalized
            );

        /*
         * Smoothly rotate toward the target.
         */
        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            targetRotation,
            rotationSpeed * Time.deltaTime
        );
    }

    #endregion


    // TODO: Add Patrol navigation between configured patrol points.
    // TODO: Add Investigate movement toward sound or event locations.
    // TODO: Add Return navigation back to the patrol route or home position.
    // TODO: Add randomized Search movement around the last known position.
}