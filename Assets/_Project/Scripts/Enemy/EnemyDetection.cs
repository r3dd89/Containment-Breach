using UnityEngine;

/*
 * Script Name: EnemyDetection
 * Purpose:
 * Handles player detection for an enemy.
 *
 * This script uses values from EnemyData through EnemyController.
 *
 * It currently handles:
 * - Checking detection range
 * - Checking field of view
 * - Checking line of sight
 * - Assigning the player as the current target
 * - Keeping the target while within lose-target range
 * - Clearing the target when the player moves too far away
 *
 * This script does NOT move the enemy.
 * Movement and chasing will be handled by another system.
 */

public class EnemyDetection : MonoBehaviour
{
    #region Component References

    /*
     * REGION: Component References
     *
     * This region stores references to the components
     * required by the detection system.
     */

    [Header("Component References")]

    [SerializeField]
    [Tooltip("The EnemyController that stores this enemy's runtime state and EnemyData values.")]
    private EnemyController enemyController;

    #endregion


    #region Player Detection References

    /*
     * REGION: Player Detection References
     *
     * This region stores information used to locate
     * and identify the player.
     */

    [Header("Player Detection References")]

    [SerializeField]
    [Tooltip("The Transform of the player that this enemy can detect.")]
    private Transform playerTarget;

    #endregion


    #region Line Of Sight Settings

    /*
     * REGION: Line Of Sight Settings
     *
     * These settings control which objects can block
     * the enemy's view of the player.
     */

    [Header("Line Of Sight Settings")]

    [SerializeField]
    [Tooltip("Layers that can block line of sight between the enemy and the player.")]
    private LayerMask obstacleLayerMask = ~0;

    [SerializeField]
    [Tooltip("Vertical offset used as the enemy's approximate eye position.")]
    private float eyeHeight = 1.5f;

    [SerializeField]
    [Tooltip("Vertical offset used as the player's approximate target point.")]
    private float playerTargetHeight = 1f;

    #endregion


    #region Runtime Detection State

    /*
     * REGION: Runtime Detection State
     *
     * These values are visible in the Inspector
     * for checking what the detection system is doing.
     */

    [Header("Runtime Detection State")]

    [SerializeField]
    [Tooltip("Shows the current distance between the enemy and the player.")]
    private float distanceToPlayer;

    [SerializeField]
    [Tooltip("Shows whether the player is currently inside the enemy's field of view.")]
    private bool playerInsideFieldOfView;

    [SerializeField]
    [Tooltip("Shows whether the enemy currently has an unobstructed view of the player.")]
    private bool hasLineOfSight;

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
         * Automatically find EnemyController if the reference
         * was not assigned manually in the Inspector.
         */
        if (enemyController == null)
        {
            enemyController = GetComponent<EnemyController>();
        }

        /*
         * Report an error if EnemyController is still missing.
         */
        if (enemyController == null)
        {
            Debug.LogError(
                "EnemyDetection on " + gameObject.name +
                " requires an EnemyController component."
            );
        }
    }


    private void Update()
    {
        /*
         * Stop detection if a required reference is missing.
         */
        if (enemyController == null || playerTarget == null)
        {
            return;
        }

        // Update the current distance to the player.
        distanceToPlayer = Vector3.Distance(
            transform.position,
            playerTarget.position
        );

        /*
         * If a target is already detected,
         * handle whether that target should remain active.
         */
        if (enemyController.HasDetectedTarget)
        {
            HandleExistingTarget();

            return;
        }

        /*
         * If no target is currently detected,
         * attempt to detect the player.
         */
        TryDetectPlayer();
    }

    #endregion


    #region Detection Methods

    /*
     * REGION: Detection Methods
     *
     * This region contains the main logic used
     * to determine whether the player can be detected.
     */

    private void TryDetectPlayer()
    {
        /*
         * The player must first be inside the enemy's
         * configured detection range.
         */
        if (distanceToPlayer > enemyController.DetectionRange)
        {
            ResetDetectionState();

            return;
        }

        /*
         * Check whether the player is inside
         * the enemy's horizontal field of view.
         */
        playerInsideFieldOfView = IsPlayerInsideFieldOfView();

        if (!playerInsideFieldOfView)
        {
            hasLineOfSight = false;

            return;
        }

        /*
         * If the player is inside the field of view,
         * check whether a wall or other obstacle
         * blocks the enemy's view.
         */
        hasLineOfSight = CheckLineOfSight();

        /*
         * Detection succeeds only when:
         *
         * - The player is within detection range
         * - The player is inside the field of view
         * - The enemy has line of sight
         */
        if (hasLineOfSight)
        {
            enemyController.SetTarget(playerTarget);
        }
    }


    private void HandleExistingTarget()
    {
        /*
         * Once the player has been detected,
         * the enemy can keep tracking the player
         * beyond the original detection range.
         *
         * LoseTargetRange controls how far the player
         * must get before the enemy completely gives up.
         */
        if (distanceToPlayer > enemyController.LoseTargetRange)
        {
            enemyController.ClearTarget();

            ResetDetectionState();

            return;
        }

        /*
         * Continue updating field-of-view and line-of-sight
         * information while the target remains tracked.
         *
         * These values can later be used by search,
         * investigate, chase, and attack states.
         */
        playerInsideFieldOfView = IsPlayerInsideFieldOfView();

        hasLineOfSight = CheckLineOfSight();
    }


    private bool IsPlayerInsideFieldOfView()
    {
        /*
         * Calculate the direction from the enemy
         * toward the player.
         */
        Vector3 directionToPlayer =
            playerTarget.position - transform.position;

        /*
         * Ignore vertical difference for the field-of-view check.
         *
         * This keeps the check focused on horizontal vision.
         */
        directionToPlayer.y = 0f;

        /*
         * If the player is directly on top of the enemy,
         * avoid calculating an unnecessary angle.
         */
        if (directionToPlayer.sqrMagnitude <= 0.001f)
        {
            return true;
        }

        // Normalize the direction before comparing angles.
        directionToPlayer.Normalize();

        /*
         * Calculate the horizontal angle between:
         *
         * - The enemy's forward direction
         * - The direction toward the player
         */
        float angleToPlayer = Vector3.Angle(
            transform.forward,
            directionToPlayer
        );

        /*
         * FieldOfView represents the entire viewing cone.
         *
         * Half of that value exists on each side
         * of the enemy's forward direction.
         */
        float halfFieldOfView =
            enemyController.FieldOfView / 2f;

        // Return true if the player is inside the viewing cone.
        return angleToPlayer <= halfFieldOfView;
    }


    private bool CheckLineOfSight()
    {
        /*
         * Create an approximate eye position for the enemy.
         */
        Vector3 enemyEyePosition =
            transform.position +
            (Vector3.up * eyeHeight);

        /*
         * Create an approximate center point on the player.
         */
        Vector3 playerAimPosition =
            playerTarget.position +
            (Vector3.up * playerTargetHeight);

        /*
         * Calculate the direction from the enemy's eye
         * toward the player's target point.
         */
        Vector3 directionToPlayer =
            playerAimPosition - enemyEyePosition;

        // Calculate the total ray distance.
        float rayDistance = directionToPlayer.magnitude;

        /*
         * Normalize the direction before using it
         * in the physics raycast.
         */
        directionToPlayer.Normalize();

        /*
         * Cast a ray toward the player.
         *
         * The ray checks all layers currently included
         * in obstacleLayerMask.
         */
        if (Physics.Raycast(
            enemyEyePosition,
            directionToPlayer,
            out RaycastHit hit,
            rayDistance,
            obstacleLayerMask,
            QueryTriggerInteraction.Ignore
        ))
        {
            /*
             * The ray may hit the player's own collider.
             *
             * Detection succeeds if the object hit
             * belongs to the playerTarget hierarchy.
             */
            if (hit.transform == playerTarget ||
                hit.transform.IsChildOf(playerTarget))
            {
                return true;
            }

            /*
             * Something else was hit first,
             * so line of sight is blocked.
             */
            return false;
        }

        /*
         * If the ray did not hit anything,
         * line of sight is considered clear.
         *
         * This is useful if the player's transform
         * does not currently have a collider.
         */
        return true;
    }


    private void ResetDetectionState()
    {
        /*
         * Reset runtime detection information
         * when the player is outside detection conditions.
         */
        playerInsideFieldOfView = false;
        hasLineOfSight = false;
    }

    #endregion
}