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
 * - Tracking whether the player is dead
 * - Disabling player movement after death
 * - Disabling camera control after death
 * - Showing the death screen after death
 * - Unlocking the mouse cursor after death
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
         * Subtract incoming damage from current health.
         */
        currentHealth -= damageAmount;

        /*
         * Keep health between zero and maximum health.
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
         * Add health.
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
     * gameplay control lockout, and death UI.
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
         * Unlock the mouse cursor so the player
         * can interact with death-screen buttons.
         */
        Cursor.lockState = CursorLockMode.None;

        /*
         * Make the mouse cursor visible.
         */
        Cursor.visible = true;

        // TODO: Add death audio.
        // TODO: Add screen fade or other death effects.
        // TODO: Add checkpoint restart support.
    }

    #endregion
}