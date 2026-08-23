using UnityEngine;

/*
 * Script Name: PlayerHealth
 * Purpose:
 * Handles the player's runtime health, death state,
 * and respawn recovery.
 *
 * This script currently handles:
 * - Maximum health
 * - Current health
 * - Receiving damage
 * - Healing
 * - Tracking whether the player is dead
 * - Disabling player controls after death
 * - Showing the death screen after death
 * - Unlocking the cursor after death
 * - Restoring the player after checkpoint respawn
 */

public class PlayerHealth : MonoBehaviour
{
    #region Component References

    /*
     * REGION: Component References
     *
     * This region stores references to player components
     * that are disabled during death and restored during respawn.
     */

    [Header("Component References")]

    [SerializeField]
    [Tooltip("The PlayerMovement component that controls walking, sprinting, jumping, and crouching.")]
    private PlayerMovement playerMovement;

    [SerializeField]
    [Tooltip("The CameraController component that controls first-person mouse look.")]
    private CameraController cameraController;

    #endregion


    #region User Interface References

    /*
     * REGION: User Interface References
     *
     * This region stores references to user interface
     * objects related to player health and death.
     */

    [Header("User Interface References")]

    [SerializeField]
    [Tooltip("The panel that becomes visible when the player dies.")]
    private GameObject deathPanel;

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

        /*
         * Make sure the death panel begins hidden.
         */
        if (deathPanel != null)
        {
            deathPanel.SetActive(false);
        }
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
         * Ignore damage if the player is already dead.
         */
        if (isDead)
        {
            return;
        }

        /*
         * Ignore zero or negative damage values.
         */
        if (damageAmount <= 0f)
        {
            return;
        }

        /*
         * Subtract incoming damage.
         */
        currentHealth -= damageAmount;

        /*
         * Keep health inside valid limits.
         */
        currentHealth = Mathf.Clamp(
            currentHealth,
            0f,
            maxHealth
        );

        /*
         * Trigger death when health reaches zero.
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
         * Add the healing amount.
         */
        currentHealth += healAmount;

        /*
         * Prevent health from exceeding maximum health.
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
     * This region handles the player's death state,
     * control lockout, and death interface.
     */

    private void Die()
    {
        /*
         * Mark the player as dead.
         */
        isDead = true;

        /*
         * Keep health exactly at zero.
         */
        currentHealth = 0f;

        /*
         * Disable movement controls.
         */
        if (playerMovement != null)
        {
            playerMovement.enabled = false;
        }

        /*
         * Disable first-person camera controls.
         */
        if (cameraController != null)
        {
            cameraController.enabled = false;
        }

        /*
         * Show the death screen.
         */
        if (deathPanel != null)
        {
            deathPanel.SetActive(true);
        }

        /*
         * Unlock the cursor so death-screen
         * buttons can be selected.
         */
        Cursor.lockState = CursorLockMode.None;

        /*
         * Make the cursor visible.
         */
        Cursor.visible = true;

        // TODO: Add death audio.
        // TODO: Add death screen effects.
    }

    #endregion


    #region Respawn Methods

    /*
     * REGION: Respawn Methods
     *
     * This region restores the player's gameplay state
     * after the GameManager moves the player
     * to an active checkpoint.
     */

    public void Respawn()
    {
        /*
         * Restore the player's health.
         */
        currentHealth = maxHealth;

        /*
         * Mark the player as alive again.
         */
        isDead = false;

        /*
         * Restore movement controls.
         */
        if (playerMovement != null)
        {
            playerMovement.enabled = true;
        }

        /*
         * Restore first-person camera controls.
         */
        if (cameraController != null)
        {
            cameraController.enabled = true;
        }

        /*
         * Hide the death screen.
         */
        if (deathPanel != null)
        {
            deathPanel.SetActive(false);
        }

        /*
         * Restore first-person cursor behavior.
         */
        Cursor.lockState = CursorLockMode.Locked;

        /*
         * Hide the cursor during gameplay.
         */
        Cursor.visible = false;

        // TODO: Reset player animation state after respawn.
        // TODO: Add temporary respawn invulnerability if needed.
        // TODO: Add respawn visual and audio feedback.
    }

    #endregion
}