using UnityEngine;

/*
 * Script Name: EnemyAttack
 * Purpose:
 * Handles the basic attack behavior for an enemy.
 *
 * This script reads combat values from EnemyController,
 * which receives those values from the assigned EnemyData asset.
 *
 * This script currently handles:
 * - Checking whether a target exists
 * - Checking whether the target is inside attack range
 * - Enforcing attack cooldown timing
 * - Applying damage to PlayerHealth
 *
 * Later, this script can be expanded to support:
 * - Attack animations
 * - Attack sound effects
 * - Hit timing
 * - Screen effects
 * - Enemy-specific attack behavior
 */

public class EnemyAttack : MonoBehaviour
{
    #region Component References

    /*
     * REGION: Component References
     *
     * This region stores references to the components
     * required by the attack system.
     */

    [Header("Component References")]

    [SerializeField]
    [Tooltip("The EnemyController that provides target information and combat values.")]
    private EnemyController enemyController;

    #endregion


    #region Runtime Attack State

    /*
     * REGION: Runtime Attack State
     *
     * These values track the enemy's current attack status
     * while the game is running.
     */

    [Header("Runtime Attack State")]

    [SerializeField]
    [Tooltip("Shows the current distance between the enemy and its target.")]
    private float distanceToTarget;

    [SerializeField]
    [Tooltip("Shows whether the current target is inside attack range.")]
    private bool targetInAttackRange;

    [SerializeField]
    [Tooltip("Shows the remaining time before another attack is allowed.")]
    private float attackCooldownTimer;

    #endregion


    #region Public Properties

    /*
     * REGION: Public Properties
     *
     * These properties allow other systems
     * to safely read attack-related runtime information.
     */

    public bool TargetInAttackRange
    {
        get { return targetInAttackRange; }
    }

    public float AttackCooldownTimer
    {
        get { return attackCooldownTimer; }
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

        /*
         * Report an error if EnemyController is missing.
         *
         * This diagnostic should be reviewed
         * before the final production build.
         */
        if (enemyController == null)
        {
            UnityEngine.Debug.LogError(
                "EnemyAttack on " + gameObject.name +
                " requires an EnemyController component."
            );
        }
    }


    private void Update()
    {
        /*
         * Stop attack logic if EnemyController is missing.
         */
        if (enemyController == null)
        {
            return;
        }

        /*
         * Reduce the cooldown timer every frame
         * until it reaches zero.
         */
        UpdateAttackCooldown();

        /*
         * If no valid target exists,
         * reset attack state and stop.
         */
        if (!enemyController.HasDetectedTarget ||
            enemyController.CurrentTarget == null)
        {
            ResetAttackState();

            return;
        }

        /*
         * A valid target exists.
         * Check whether the target is close enough to attack.
         */
        CheckAttackRange();
    }

    #endregion


    #region Attack Range Methods

    /*
     * REGION: Attack Range Methods
     *
     * This region determines whether the current target
     * is inside the configured attack range.
     */

    private void CheckAttackRange()
    {
        /*
         * Calculate the distance between the enemy
         * and the current target.
         */
        distanceToTarget = Vector3.Distance(
            transform.position,
            enemyController.CurrentTarget.position
        );

        /*
         * Compare the distance to the attack range
         * stored in EnemyData.
         */
        targetInAttackRange =
            distanceToTarget <= enemyController.AttackRange;

        /*
         * Attempt an attack if the target
         * is close enough.
         */
        if (targetInAttackRange)
        {
            TryAttack();
        }
    }

    #endregion


    #region Attack Methods

    /*
     * REGION: Attack Methods
     *
     * This region controls attack timing
     * and applies damage to the current target.
     */

    private void TryAttack()
    {
        /*
         * An attack cannot occur while
         * the cooldown timer is still active.
         */
        if (attackCooldownTimer > 0f)
        {
            return;
        }

        /*
         * Perform the attack.
         */
        PerformAttack();

        /*
         * Restart the cooldown using the value
         * configured in EnemyData.
         */
        attackCooldownTimer =
            enemyController.AttackCooldown;
    }


    private void PerformAttack()
    {
        /*
         * Stop if the current target reference
         * is no longer valid.
         */
        if (enemyController.CurrentTarget == null)
        {
            return;
        }

        /*
         * Try to find PlayerHealth on the target object.
         *
         * The Player object currently contains
         * the PlayerHealth component.
         */
        PlayerHealth playerHealth =
            enemyController.CurrentTarget.GetComponent<PlayerHealth>();

        /*
         * Stop if the target does not contain PlayerHealth.
         *
         * This prevents attack logic from failing
         * if another type of target is used later.
         */
        if (playerHealth == null)
        {
            return;
        }

        /*
         * Do not apply additional damage
         * after the player has died.
         */
        if (playerHealth.IsDead)
        {
            return;
        }

        /*
         * Apply the attack damage configured
         * in the enemy's EnemyData asset.
         *
         * Example:
         * EnemyData_Brute currently uses 35 damage.
         */
        playerHealth.TakeDamage(
            enemyController.AttackDamage
        );

        /*
         * Attack animations, sound effects,
         * hit timing, and other feedback
         * can be connected here later.
         */
    }

    #endregion


    #region Cooldown Methods

    /*
     * REGION: Cooldown Methods
     *
     * This region manages the time between attacks.
     */

    private void UpdateAttackCooldown()
    {
        /*
         * Stop if the cooldown has already reached zero.
         */
        if (attackCooldownTimer <= 0f)
        {
            attackCooldownTimer = 0f;

            return;
        }

        /*
         * Reduce the cooldown timer using frame time.
         */
        attackCooldownTimer -= Time.deltaTime;

        /*
         * Prevent the timer from becoming negative.
         */
        if (attackCooldownTimer < 0f)
        {
            attackCooldownTimer = 0f;
        }
    }

    #endregion


    #region Reset Methods

    /*
     * REGION: Reset Methods
     *
     * This region clears runtime attack information
     * when no valid target is available.
     */

    private void ResetAttackState()
    {
        distanceToTarget = 0f;
        targetInAttackRange = false;
    }

    #endregion
}