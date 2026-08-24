using UnityEngine;

/*
 * Script Name: EnemyDetection
 * Purpose:
 * Handles player detection for an enemy.
 *
 * This script uses values from EnemyData through EnemyController.
 *
 * This script currently handles:
 * - Checking detection range
 * - Checking field of view
 * - Checking line of sight
 * - Assigning the player as the current target
 * - Keeping the target while within lose-target range
 * - Clearing the target when the player moves too far away
 * - Storing the player's last known position
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
     * so the current detection state can be monitored.
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

    [SerializeField]
    [Tooltip("Stores the last position where the enemy had confirmed visual contact with the player.")]
    private Vector3 lastKnownPlayerPosition;

    [SerializeField]
    [Tooltip("Shows whether a valid last known player position has been recorded.")]
    private bool hasLastKnownPosition;

    #endregion


    #region Public Properties

    /*
     * REGION: Public Properties
     *
     * These properties allow other enemy systems
     * to safely read detection information.
     */

    public bool HasLineOfSight
    {
        get { return hasLineOfSight; }
    }

    public Vector3 LastKnownPlayerPosition
    {
        get { return lastKnownPlayerPosition; }
    }

    public bool HasLastKnownPosition
    {
        get { return hasLastKnownPosition; }
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
         * Automatically find EnemyController if the reference
         * was not assigned manually in the Inspector.
         */
        if (enemyController == null)
        {
            enemyController = GetComponent<EnemyController>();
        }
    }


    private void Update()
    {
        /*
         * Stop detection if a required reference is missing.
         */
        if (enemyController == null ||
            playerTarget == null)
        {
            return;
        }

        /*
         * Update the current distance to the player.
         */
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
         * The player must first be inside
         * the configured detection range.
         */
        if (distanceToPlayer >
            enemyController.DetectionRange)
        {
            ResetDetectionState();

            return;
        }

        /*
         * Check whether the player is inside
         * the enemy's horizontal field of view.
         */
        playerInsideFieldOfView =
            IsPlayerInsideFieldOfView();

        if (!playerInsideFieldOfView)
        {
            hasLineOfSight = false;

            return;
        }

        /*
         * Check whether an obstacle blocks
         * the enemy's view of the player.
         */
        hasLineOfSight =
            CheckLineOfSight();

        /*
         * Detection succeeds only when
         * line of sight is confirmed.
         */
        if (hasLineOfSight)
        {
            /*
             * Store the player's position
             * as the most recent confirmed location.
             */
            UpdateLastKnownPlayerPosition();

            /*
             * Register the player as the active target.
             */
            enemyController.SetTarget(
                playerTarget
            );
        }
    }


    private void HandleExistingTarget()
    {
        /*
         * Clear the target if the player moves
         * beyond the configured lose-target range.
         */
        if (distanceToPlayer >
            enemyController.LoseTargetRange)
        {
            enemyController.ClearTarget();

            ResetDetectionState();

            return;
        }

        /*
         * Continue checking field of view
         * and line of sight while tracking the player.
         */
        playerInsideFieldOfView =
            IsPlayerInsideFieldOfView();

        hasLineOfSight =
            CheckLineOfSight();

        /*
         * Update the last known position only
         * while visual contact is actually confirmed.
         *
         * Once line of sight is lost,
         * the stored value remains at the last
         * position where the player was seen.
         */
        if (hasLineOfSight)
        {
            UpdateLastKnownPlayerPosition();
        }
    }


    private bool IsPlayerInsideFieldOfView()
    {
        /*
         * Calculate the direction from the enemy
         * toward the player.
         */
        Vector3 directionToPlayer =
            playerTarget.position -
            transform.position;

        /*
         * Ignore vertical difference for
         * the horizontal field-of-view check.
         */
        directionToPlayer.y = 0f;

        /*
         * Avoid calculating an unnecessary angle
         * if the player is extremely close.
         */
        if (directionToPlayer.sqrMagnitude <= 0.001f)
        {
            return true;
        }

        /*
         * Normalize before comparing directions.
         */
        directionToPlayer.Normalize();

        /*
         * Calculate the horizontal angle between
         * the enemy's forward direction
         * and the direction toward the player.
         */
        float angleToPlayer =
            Vector3.Angle(
                transform.forward,
                directionToPlayer
            );

        /*
         * FieldOfView represents the full cone,
         * so half of that value exists on each side.
         */
        float halfFieldOfView =
            enemyController.FieldOfView / 2f;

        return angleToPlayer <= halfFieldOfView;
    }


    private bool CheckLineOfSight()
    {
        /*
         * Create an approximate eye position
         * for the enemy.
         */
        Vector3 enemyEyePosition =
            transform.position +
            (Vector3.up * eyeHeight);

        /*
         * Create an approximate target point
         * on the player.
         */
        Vector3 playerAimPosition =
            playerTarget.position +
            (Vector3.up * playerTargetHeight);

        /*
         * Calculate direction and distance
         * from enemy eye position to the player.
         */
        Vector3 directionToPlayer =
            playerAimPosition -
            enemyEyePosition;

        float rayDistance =
            directionToPlayer.magnitude;

        directionToPlayer.Normalize();

        /*
         * Cast a ray toward the player.
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
             * Visual contact succeeds if the ray
             * reaches the player hierarchy first.
             */
            if (hit.transform == playerTarget ||
                hit.transform.IsChildOf(playerTarget))
            {
                return true;
            }

            /*
             * Another collider blocked the view.
             */
            return false;
        }

        /*
         * If nothing was hit,
         * the path is considered unobstructed.
         */
        return true;
    }


    private void UpdateLastKnownPlayerPosition()
    {
        /*
         * Store the player's current world position.
         */
        lastKnownPlayerPosition =
            playerTarget.position;

        /*
         * Mark the stored position as valid.
         */
        hasLastKnownPosition = true;
    }


    private void ResetDetectionState()
    {
        /*
         * Reset current visual detection information.
         *
         * The last known position is intentionally
         * preserved so Search behavior can use it.
         */
        playerInsideFieldOfView = false;
        hasLineOfSight = false;
    }

    #endregion


    // TODO: Add hearing-based detection.
    // TODO: Add investigation positions from environmental sounds.
    // TODO: Add configurable detection update intervals for optimization.
}