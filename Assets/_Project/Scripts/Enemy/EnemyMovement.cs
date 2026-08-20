using UnityEngine;
using UnityEngine.AI;

/*
 * Script Name: EnemyMovement
 * Purpose:
 * Handles enemy navigation and movement toward the current target.
 *
 * This script uses:
 * - EnemyController for runtime target information
 * - EnemyData values through EnemyController
 * - NavMeshAgent for pathfinding
 *
 * This script currently handles:
 * - Standing still when no target is detected
 * - Chasing the current target
 * - Using chase speed from EnemyData
 * - Stopping when the target enters attack range
 * - Rotating toward the target while stopped
 *
 * This script does not perform attacks.
 * Combat behavior will be handled separately.
 */

public class EnemyMovement : MonoBehaviour
{
    #region Component References

    /*
     * REGION: Component References
     *
     * This region stores references to the components
     * required for enemy movement.
     */

    [Header("Component References")]

    [SerializeField]
    [Tooltip("The EnemyController that provides target and EnemyData information.")]
    private EnemyController enemyController;

    [SerializeField]
    [Tooltip("The NavMeshAgent used to move this enemy across the NavMesh.")]
    private NavMeshAgent navMeshAgent;

    #endregion


    #region Rotation Settings

    /*
     * REGION: Rotation Settings
     *
     * These values control how the enemy rotates
     * toward the target when standing still.
     */

    [Header("Rotation Settings")]

    [SerializeField]
    [Tooltip("How quickly the enemy rotates toward the current target.")]
    private float rotationSpeed = 8f;

    #endregion


    #region Runtime State

    /*
     * REGION: Runtime State
     *
     * These values show the current movement state
     * while the game is running.
     */

    [Header("Runtime State")]

    [SerializeField]
    [Tooltip("Shows the current distance between the enemy and its target.")]
    private float distanceToTarget;

    [SerializeField]
    [Tooltip("Shows whether the enemy is currently chasing its target.")]
    private bool isChasing;

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
         * Automatically find EnemyController if it
         * was not assigned manually in the Inspector.
         */
        if (enemyController == null)
        {
            enemyController = GetComponent<EnemyController>();
        }

        /*
         * Automatically find NavMeshAgent if it
         * was not assigned manually in the Inspector.
         */
        if (navMeshAgent == null)
        {
            navMeshAgent = GetComponent<NavMeshAgent>();
        }

        /*
         * Report an error if EnemyController is missing.
         */
        if (enemyController == null)
        {
            Debug.LogError(
                "EnemyMovement on " + gameObject.name +
                " requires an EnemyController component."
            );
        }

        /*
         * Report an error if NavMeshAgent is missing.
         */
        if (navMeshAgent == null)
        {
            Debug.LogError(
                "EnemyMovement on " + gameObject.name +
                " requires a NavMeshAgent component."
            );
        }
    }


    private void Start()
    {
        /*
         * Stop here if the NavMeshAgent is missing.
         */
        if (navMeshAgent == null)
        {
            return;
        }

        /*
         * Disable automatic rotation.
         *
         * Rotation is handled manually so the enemy
         * can face the player correctly when stopped.
         */
        navMeshAgent.updateRotation = false;
    }


    private void Update()
    {
        /*
         * Stop movement logic if required components
         * are missing.
         */
        if (enemyController == null || navMeshAgent == null)
        {
            return;
        }

        /*
         * If no target has been detected,
         * stop the enemy.
         */
        if (!enemyController.HasDetectedTarget ||
            enemyController.CurrentTarget == null)
        {
            StopMovement();

            return;
        }

        /*
         * A valid target exists.
         * Handle chasing behavior.
         */
        HandleChase();
    }

    #endregion


    #region Movement Methods

    /*
     * REGION: Movement Methods
     *
     * This region handles movement toward
     * the enemy's detected target.
     */

    private void HandleChase()
    {
        // Store the current target.
        Transform target =
            enemyController.CurrentTarget;

        /*
         * Calculate the distance from the enemy
         * to the current target.
         */
        distanceToTarget = Vector3.Distance(
            transform.position,
            target.position
        );

        /*
         * If the target is already inside attack range,
         * stop moving toward the target.
         */
        if (distanceToTarget <= enemyController.AttackRange)
        {
            StopMovement();

            /*
             * Face the target while stopped.
             *
             * This prepares the enemy for future
             * attack animations and combat behavior.
             */
            RotateTowardTarget(target.position);

            return;
        }

        /*
         * The target is outside attack range,
         * so begin or continue chasing.
         */
        isChasing = true;

        /*
         * Use the configured chase speed
         * from the assigned EnemyData asset.
         */
        navMeshAgent.speed =
            enemyController.ChaseSpeed;

        /*
         * Set the target's current position
         * as the NavMesh destination.
         */
        navMeshAgent.SetDestination(
            target.position
        );

        /*
         * Rotate toward the direction of movement.
         */
        RotateTowardMovementDirection();
    }


    private void StopMovement()
    {
        /*
         * Update the runtime movement state.
         */
        isChasing = false;

        /*
         * Stop here if the NavMeshAgent
         * is not currently placed on a NavMesh.
         */
        if (!navMeshAgent.isOnNavMesh)
        {
            return;
        }

        /*
         * Clear the current movement destination.
         */
        navMeshAgent.ResetPath();
    }

    #endregion


    #region Rotation Methods

    /*
     * REGION: Rotation Methods
     *
     * This region controls how the enemy rotates
     * while chasing or facing a target.
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
         * Use the movement velocity as the direction
         * the enemy should face.
         */
        Vector3 movementDirection =
            navMeshAgent.velocity.normalized;

        /*
         * Ignore vertical rotation.
         */
        movementDirection.y = 0f;

        /*
         * Stop if the remaining direction
         * is too small to create a valid rotation.
         */
        if (movementDirection.sqrMagnitude <= 0.001f)
        {
            return;
        }

        /*
         * Create the desired rotation.
         */
        Quaternion targetRotation =
            Quaternion.LookRotation(
                movementDirection
            );

        /*
         * Smoothly rotate toward the movement direction.
         */
        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            targetRotation,
            rotationSpeed * Time.deltaTime
        );
    }


    private void RotateTowardTarget(Vector3 targetPosition)
    {
        /*
         * Calculate the horizontal direction
         * from the enemy toward the target.
         */
        Vector3 directionToTarget =
            targetPosition - transform.position;

        /*
         * Ignore vertical difference.
         */
        directionToTarget.y = 0f;

        /*
         * Stop if the direction is too small.
         */
        if (directionToTarget.sqrMagnitude <= 0.001f)
        {
            return;
        }

        /*
         * Create the desired rotation.
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
}