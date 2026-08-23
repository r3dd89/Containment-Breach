using UnityEngine;

/*
 * Script Name: PlayerHealth
 * Purpose:
 * Handles the player's runtime health and death state.
 *
 * This script currently handles:
 * - Maximum health
 * - Current health
 * - Receiving damage
 * - Healing
 * - Preventing health from going below zero
 * - Preventing health from exceeding maximum health
 * - Tracking whether the player is dead
 * - Disabling player movement after death
 * - Disabling camera control after death
 *
 * Later, this script can be expanded to support:
 * - Armor
 * - Health UI
 * - Death screen
 * - Respawning
 * - Checkpoints
 * - Damage effects
 * - Healing items
 */

public class PlayerHealth : MonoBehaviour
{
    #region Component References

    /*
     * REGION: Component References
     *
     * This region stores references to player components
     * that should be disabled when the player dies.
     */

    [Header("Component References")]

    [SerializeField]
    [Tooltip("The PlayerMovement component that controls walking, sprinting, jumping, and crouching.")]
    private PlayerMovement playerMovement;

    [SerializeField]
    [Tooltip("The CameraController component that controls first-person mouse look.")]
    private CameraController cameraController;

    #endregion


    #region Health Settings

    /*
     * REGION: Health Settings
     *
     * This region contains the player's
     * main configurable health value.
     */

    [Header("Health Settings")]

    [SerializeField]
    [Min(1f)]
    [Tooltip("The maximum amount of health the player can have.")]
    private float maxHealth = 100f;

    #endregion


    #region Runtime Health

    /*
     * REGION: Runtime Health
     *
     * These values represent the player's
     * current health state during gameplay.
     */

    [Header("Runtime Health")]

    [SerializeField]
    [Tooltip("The player's current health during gameplay.")]
    private float currentHealth;

    [SerializeField]
    [Tooltip("Shows whether the player is currently dead.")]
    private bool isDead;

    #endregion


    #region Public Properties

    /*
     * REGION: Public Properties
     *
     * These properties allow other systems
     * to safely read player health information.
     */

    public float MaxHealth
    {
        get { return maxHealth; }
    }

    public float CurrentHealth
    {
        get { return currentHealth; }
    }

    public bool IsDead
    {
        get { return isDead; }
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
         * Automatically find PlayerMovement
         * if it was not assigned manually.
         */
        if (playerMovement == null)
        {
            playerMovement = GetComponent<PlayerMovement>();
        }

        /*
         * Automatically find CameraController
         * from the Player object's children.
         *
         * The Main Camera is currently a child
         * of the Player object.
         */
        if (cameraController == null)
        {
            cameraController = GetComponentInChildren<CameraController>();
        }

        /*
         * Start the player at full health.
         */
        currentHealth = maxHealth;

        /*
         * Make sure the player begins alive.
         */
        isDead = false;
    }

    #endregion


    #region Damage Methods

    /*
     * REGION: Damage Methods
     *
     * This region handles damage received by the player.
     */

    public void TakeDamage(float damageAmount)
    {
        /*
         * Do nothing if the player is already dead.
         *
         * This prevents repeated damage from
         * triggering death behavior multiple times.
         */
        if (isDead)
        {
            return;
        }

        /*
         * Ignore zero or negative damage values.
         *
         * Damage should never accidentally
         * increase the player's health.
         */
        if (damageAmount <= 0f)
        {
            return;
        }

        /*
         * Subtract the incoming damage
         * from the player's current health.
         */
        currentHealth -= damageAmount;

        /*
         * Keep current health between
         * zero and maximum health.
         */
        currentHealth = Mathf.Clamp(
            currentHealth,
            0f,
            maxHealth
        );

        /*
         * Check whether the damage
         * reduced health to zero.
         */
        if (currentHealth <= 0f)
        {
            Die();
        }
    }

    #endregion


    #region Healing Methods

    /*
     * REGION: Healing Methods
     *
     * This region handles restoring player health.
     */

    public void Heal(float healAmount)
    {
        /*
         * Dead players cannot currently be healed.
         *
         * Respawning or revival can be handled
         * by a separate system later.
         */
        if (isDead)
        {
            return;
        }

        /*
         * Ignore zero or negative healing values.
         */
        if (healAmount <= 0f)
        {
            return;
        }

        /*
         * Add the healing amount
         * to the player's current health.
         */
        currentHealth += healAmount;

        /*
         * Prevent healing from raising health
         * above the configured maximum.
         */
        currentHealth = Mathf.Clamp(
            currentHealth,
            0f,
            maxHealth
        );
    }

    #endregion


    #region Death Methods

    /*
     * REGION: Death Methods
     *
     * This region handles the player's death state
     * and disables gameplay controls.
     */

    private void Die()
    {
        /*
         * Mark the player as dead.
         */
        isDead = true;

        /*
         * Keep health exactly at zero
         * after death.
         */
        currentHealth = 0f;

        /*
         * Disable PlayerMovement.
         *
         * This prevents:
         * - Walking
         * - Sprinting
         * - Jumping
         * - Crouching
         */
        if (playerMovement != null)
        {
            playerMovement.enabled = false;
        }

        /*
         * Disable CameraController.
         *
         * This prevents mouse-look input
         * after the player dies.
         */
        if (cameraController != null)
        {
            cameraController.enabled = false;
        }

        /*
         * Additional death behavior can be added later.
         *
         * Examples:
         * - Unlock the mouse cursor
         * - Show a death screen
         * - Play death audio
         * - Fade the screen
         * - Restart from checkpoint
         * - Reload the current scene
         */
    }

    #endregion
}