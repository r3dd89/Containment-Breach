using UnityEngine;
using UnityEngine.AI;

/*
 * Script Name: EnemyHealth
 * Purpose:
 * Handles runtime enemy health and the initial enemy death state.
 *
 * This script reads maximum health from the assigned
 * EnemyData ScriptableObject.
 *
 * Current responsibilities:
 * - Initialize enemy health.
 * - Receive damage.
 * - Prevent health from dropping below zero.
 * - Detect enemy death.
 * - Stop enemy AI behavior after death.
 * - Stop NavMesh movement after death.
 *
 * TODO:
 * - Play hit reactions.
 * - Play death animations.
 * - Disable or modify collision after the death animation.
 * - Play death audio.
 * - Spawn loot when appropriate.
 * - Notify encounter systems.
 */

public class EnemyHealth : MonoBehaviour
{
    #region Data References

    [Header("Enemy Data")]

    [SerializeField]
    [Tooltip("The EnemyData asset containing this enemy type's configuration.")]
    private EnemyData enemyData;

    #endregion


    #region Component References

    [Header("Component References")]

    [SerializeField]
    [Tooltip("Controls the enemy's target and runtime combat data.")]
    private EnemyController enemyController;

    [SerializeField]
    [Tooltip("Handles visual detection of the player.")]
    private EnemyDetection enemyDetection;

    [SerializeField]
    [Tooltip("Handles NavMesh movement for enemy AI states.")]
    private EnemyMovement enemyMovement;

    [SerializeField]
    [Tooltip("Handles enemy attacks against the player.")]
    private EnemyAttack enemyAttack;

    [SerializeField]
    [Tooltip("Controls transitions between enemy AI states.")]
    private EnemyStateMachine enemyStateMachine;

    [SerializeField]
    [Tooltip("NavMeshAgent used to move the enemy through the environment.")]
    private NavMeshAgent navMeshAgent;

    #endregion


    #region Runtime Health

    [Header("Runtime Health")]

    [SerializeField]
    [Tooltip("The enemy's current health during gameplay.")]
    private float currentHealth;

    [SerializeField]
    [Tooltip("True after the enemy has entered its death state.")]
    private bool isDead;

    #endregion


    #region Public Properties

    public float CurrentHealth
    {
        get { return currentHealth; }
    }


    public float MaxHealth
    {
        get
        {
            if (enemyData != null)
            {
                return enemyData.MaxHealth;
            }

            return 0f;
        }
    }


    public bool IsDead
    {
        get { return isDead; }
    }


    public EnemyData EnemyData
    {
        get { return enemyData; }
    }

    #endregion


    #region Unity Methods

    private void Awake()
    {
        // Verify that this enemy has configuration data.
        if (enemyData == null)
        {
            Debug.LogError(
                "EnemyHealth on " +
                gameObject.name +
                " does not have an EnemyData asset assigned.");

            return;
        }

        // Find enemy components automatically if explicit Inspector
        // references were not assigned.
        if (enemyController == null)
        {
            enemyController =
                GetComponent<EnemyController>();
        }

        if (enemyDetection == null)
        {
            enemyDetection =
                GetComponent<EnemyDetection>();
        }

        if (enemyMovement == null)
        {
            enemyMovement =
                GetComponent<EnemyMovement>();
        }

        if (enemyAttack == null)
        {
            enemyAttack =
                GetComponent<EnemyAttack>();
        }

        if (enemyStateMachine == null)
        {
            enemyStateMachine =
                GetComponent<EnemyStateMachine>();
        }

        if (navMeshAgent == null)
        {
            navMeshAgent =
                GetComponent<NavMeshAgent>();
        }

        // Start this enemy alive and at full health.
        isDead = false;
        currentHealth = enemyData.MaxHealth;
    }

    #endregion


    #region Damage Methods

    public void TakeDamage(float damageAmount)
    {
        /*
         * Dead enemies cannot receive additional gameplay damage.
         *
         * This also prevents death behavior from being triggered
         * more than once.
         */
        if (isDead)
        {
            return;
        }

        // Ignore invalid damage values.
        if (damageAmount <= 0f)
        {
            return;
        }

        // Apply incoming damage.
        currentHealth -= damageAmount;

        // Keep health inside its valid range.
        currentHealth =
            Mathf.Clamp(
                currentHealth,
                0f,
                MaxHealth);

        // Enter the death state when health reaches zero.
        if (currentHealth <= 0f)
        {
            Die();
        }
    }

    #endregion


    #region Death Methods

    private void Die()
    {
        // Prevent death logic from running more than once.
        if (isDead)
        {
            return;
        }

        // Mark the enemy dead before shutting down other systems.
        isDead = true;

        /*
         * Stop NavMesh movement first.
         *
         * Clearing the current path prevents the agent from retaining
         * an active navigation destination after death.
         */
        if (navMeshAgent != null &&
            navMeshAgent.enabled)
        {
            if (navMeshAgent.isOnNavMesh)
            {
                navMeshAgent.isStopped = true;

                if (navMeshAgent.hasPath)
                {
                    navMeshAgent.ResetPath();
                }
            }

            // Disable the agent completely after movement has stopped.
            navMeshAgent.enabled = false;
        }

        /*
         * Disable the AI behavior components.
         *
         * EnemyHealth itself remains active so other gameplay systems
         * can still query the corpse's health/death state.
         */
        if (enemyAttack != null)
        {
            enemyAttack.enabled = false;
        }

        if (enemyDetection != null)
        {
            enemyDetection.enabled = false;
        }

        if (enemyMovement != null)
        {
            enemyMovement.enabled = false;
        }

        if (enemyStateMachine != null)
        {
            enemyStateMachine.enabled = false;
        }

        if (enemyController != null)
        {
            enemyController.enabled = false;
        }

        /*
         * Keep the enemy GameObject and collider in the scene for now.
         *
         * A later animation pass can:
         * - Trigger the death animation.
         * - Change corpse collision.
         * - Add ragdoll behavior if desired.
         * - Remove the corpse after an appropriate delay.
         */

        Debug.Log(
            enemyData.EnemyName +
            " has died.");
    }

    #endregion
}