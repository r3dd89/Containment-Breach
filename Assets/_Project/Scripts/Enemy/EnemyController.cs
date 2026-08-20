using UnityEngine;

/*
 * Script Name: EnemyController
 * Purpose:
 * Acts as the main runtime controller for an enemy.
 *
 * This script reads configuration values from an EnemyData asset
 * and stores the runtime state used by enemy AI behavior.
 *
 * This script currently handles:
 * - Reading enemy configuration data
 * - Tracking whether the player has been detected
 * - Tracking the current target
 * - Providing access to movement, detection, and combat values
 *
 * Actual pathfinding, attacking, and state behavior
 * will be added in later scripts.
 */

public class EnemyController : MonoBehaviour
{
    #region Data References

    /*
     * REGION: Data References
     *
     * This region stores the EnemyData asset
     * that defines this enemy type.
     */

    [Header("Enemy Data")]

    [SerializeField]
    [Tooltip("The EnemyData asset that contains this enemy type's configuration.")]
    private EnemyData enemyData;

    #endregion


    #region Target References

    /*
     * REGION: Target References
     *
     * This region stores references related
     * to the enemy's current target.
     */

    [Header("Target References")]

    [SerializeField]
    [Tooltip("The Transform currently being tracked by this enemy.")]
    private Transform currentTarget;

    #endregion


    #region Runtime State

    /*
     * REGION: Runtime State
     *
     * These values describe the enemy's
     * current gameplay state.
     */

    [Header("Runtime State")]

    [SerializeField]
    [Tooltip("Shows whether this enemy currently has a detected target.")]
    private bool hasDetectedTarget;

    #endregion


    #region Public Properties

    /*
     * REGION: Public Properties
     *
     * These properties allow other scripts
     * to safely read enemy runtime information
     * and EnemyData configuration values.
     */

    public EnemyData EnemyData
    {
        get { return enemyData; }
    }

    public Transform CurrentTarget
    {
        get { return currentTarget; }
    }

    public bool HasDetectedTarget
    {
        get { return hasDetectedTarget; }
    }

    public float MoveSpeed
    {
        get
        {
            if (enemyData != null)
            {
                return enemyData.MoveSpeed;
            }

            return 0f;
        }
    }

    public float ChaseSpeed
    {
        get
        {
            if (enemyData != null)
            {
                return enemyData.ChaseSpeed;
            }

            return 0f;
        }
    }

    public float DetectionRange
    {
        get
        {
            if (enemyData != null)
            {
                return enemyData.DetectionRange;
            }

            return 0f;
        }
    }

    public float FieldOfView
    {
        get
        {
            if (enemyData != null)
            {
                return enemyData.FieldOfView;
            }

            return 0f;
        }
    }

    public float LoseTargetRange
    {
        get
        {
            if (enemyData != null)
            {
                return enemyData.LoseTargetRange;
            }

            return 0f;
        }
    }

    public float AttackDamage
    {
        get
        {
            if (enemyData != null)
            {
                return enemyData.AttackDamage;
            }

            return 0f;
        }
    }

    public float AttackRange
    {
        get
        {
            if (enemyData != null)
            {
                return enemyData.AttackRange;
            }

            return 0f;
        }
    }

    public float AttackCooldown
    {
        get
        {
            if (enemyData != null)
            {
                return enemyData.AttackCooldown;
            }

            return 0f;
        }
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
         * Make sure an EnemyData asset
         * has been assigned.
         */

        if (enemyData == null)
        {
            Debug.LogError(
                "EnemyController on " + gameObject.name +
                " does not have an EnemyData asset assigned."
            );
        }
    }

    #endregion


    #region Target Methods

    /*
     * REGION: Target Methods
     *
     * These methods control which object
     * the enemy is currently tracking.
     */

    public void SetTarget(Transform newTarget)
    {
        /*
         * Store the new target.
         */
        currentTarget = newTarget;

        /*
         * A valid target means the enemy
         * currently has something detected.
         */
        hasDetectedTarget = currentTarget != null;
    }


    public void ClearTarget()
    {
        /*
         * Remove the current target reference.
         */
        currentTarget = null;

        /*
         * Update the runtime detection state.
         */
        hasDetectedTarget = false;
    }

    #endregion
}