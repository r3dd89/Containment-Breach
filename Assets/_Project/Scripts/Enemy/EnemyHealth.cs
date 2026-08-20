using UnityEngine;

/*
 * Script Name: EnemyHealth
 * Purpose:
 * Handles the runtime health of an enemy.
 *
 * This script reads the enemy's maximum health
 * from an EnemyData ScriptableObject.
 *
 * It currently handles:
 * - Loading starting health from EnemyData
 * - Tracking current health
 * - Receiving damage
 * - Detecting when the enemy dies
 *
 * Later, this script can be expanded to:
 * - Play hit reactions
 * - Play death animations
 * - Drop loot
 * - Notify the enemy AI system
 * - Notify encounter managers or facility systems
 */

public class EnemyHealth : MonoBehaviour
{
    #region Data References

    /*
     * REGION: Data References
     *
     * This region stores references to data assets
     * that describe what kind of enemy this is.
     */

    [Header("Enemy Data")]

    [SerializeField]
    [Tooltip("The EnemyData asset that contains this enemy type's configuration.")]
    private EnemyData enemyData;

    #endregion


    #region Runtime Health

    /*
     * REGION: Runtime Health
     *
     * These values represent the enemy's health
     * while the game is running.
     */

    [Header("Runtime Health")]

    [SerializeField]
    [Tooltip("The enemy's current health during gameplay.")]
    private float currentHealth;

    #endregion


    #region Public Properties

    /*
     * REGION: Public Properties
     *
     * These properties allow other scripts
     * to safely read health information.
     */

    public float CurrentHealth
    {
        get { return currentHealth; }
    }

    public float MaxHealth
    {
        get
        {
            // Return the maximum health stored
            // inside the assigned EnemyData asset.
            if (enemyData != null)
            {
                return enemyData.MaxHealth;
            }

            // Return zero if no EnemyData asset exists.
            return 0f;
        }
    }

    public bool IsDead
    {
        get
        {
            // The enemy is considered dead
            // when its health reaches zero.
            return currentHealth <= 0f;
        }
    }

    public EnemyData EnemyData
    {
        get { return enemyData; }
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
        // Make sure an EnemyData asset
        // has been assigned in the Inspector.
        if (enemyData == null)
        {
            Debug.LogError(
                "EnemyHealth on " + gameObject.name +
                " does not have an EnemyData asset assigned."
            );

            return;
        }

        // Start the enemy at full health.
        currentHealth = enemyData.MaxHealth;
    }

    #endregion


    #region Damage Methods

    /*
     * REGION: Damage Methods
     *
     * This region handles damage received by the enemy.
     */

    public void TakeDamage(float damageAmount)
    {
        /*
         * Do nothing if the enemy is already dead.
         *
         * This prevents multiple damage events
         * from repeatedly triggering death behavior.
         */
        if (IsDead)
        {
            return;
        }

        /*
         * Ignore zero or negative damage values.
         *
         * Damage should never accidentally
         * increase the enemy's health.
         */
        if (damageAmount <= 0f)
        {
            return;
        }

        // Subtract the incoming damage.
        currentHealth -= damageAmount;

        // Prevent health from dropping below zero
        // or exceeding the enemy's maximum health.
        currentHealth = Mathf.Clamp(
            currentHealth,
            0f,
            MaxHealth
        );

        // Check whether this damage killed the enemy.
        if (currentHealth <= 0f)
        {
            Die();
        }
    }

    #endregion


    #region Death Methods

    /*
     * REGION: Death Methods
     *
     * This region handles what happens
     * when the enemy reaches zero health.
     */

    private void Die()
    {
        /*
         * For now, this message confirms
         * that enemy death was detected correctly.
         *
         * The GameObject is not destroyed yet.
         *
         * Later this method can trigger:
         * - A death animation
         * - AI shutdown
         * - Collider changes
         * - Loot drops
         * - Audio
         * - Encounter notifications
         */

        Debug.Log(
            enemyData.EnemyName +
            " has died."
        );
    }

    #endregion
}