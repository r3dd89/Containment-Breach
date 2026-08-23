using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

/*
 * Script Name: GameManager
 * Purpose:
 * Controls high-level game state and scene-level actions.
 *
 * This script currently handles:
 * - Creating a globally accessible GameManager instance
 * - Preventing duplicate GameManager objects
 * - Keeping the GameManager alive between scenes
 * - Restarting the current gameplay scene
 * - Respawning the player at the active checkpoint
 * - Pausing the game
 * - Resuming the game
 * - Showing and hiding the pause menu
 * - Managing the mouse cursor during pause and gameplay
 */

public class GameManager : MonoBehaviour
{
    #region Singleton

    /*
     * REGION: Singleton
     *
     * This region provides a single global reference
     * to the active GameManager instance.
     *
     * Other scripts can access this manager through:
     *
     * GameManager.Instance
     */

    public static GameManager Instance { get; private set; }

    #endregion


    #region Player References

    /*
     * REGION: Player References
     *
     * This region stores references required
     * for player respawning.
     */

    [Header("Player References")]

    [SerializeField]
    [Tooltip("The Player object that can be moved to an active checkpoint.")]
    private Transform playerTransform;

    [SerializeField]
    [Tooltip("The PlayerHealth component used to restore the player after respawning.")]
    private PlayerHealth playerHealth;

    #endregion


    #region User Interface References

    /*
     * REGION: User Interface References
     *
     * This region stores references to UI objects
     * controlled by the GameManager.
     */

    [Header("User Interface References")]

    [SerializeField]
    [Tooltip("The panel that becomes visible while the game is paused.")]
    private GameObject pausePanel;

    #endregion


    #region Runtime State

    /*
     * REGION: Runtime State
     *
     * This region stores game-state information
     * that changes while the game is running.
     */

    [Header("Runtime State")]

    [SerializeField]
    [Tooltip("Shows whether the game is currently paused.")]
    private bool isPaused;

    #endregion


    #region Public Properties

    /*
     * REGION: Public Properties
     *
     * These properties allow other systems
     * to safely read GameManager state.
     */

    public bool IsPaused
    {
        get { return isPaused; }
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
         * Assign this object as the active GameManager
         * if no instance currently exists.
         */
        if (Instance == null)
        {
            Instance = this;

            /*
             * Keep the GameManager alive
             * when scenes change.
             */
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            /*
             * Destroy duplicate GameManager objects.
             */
            Destroy(gameObject);

            return;
        }

        /*
         * Make sure gameplay begins at normal speed.
         */
        Time.timeScale = 1f;

        /*
         * Make sure the pause menu begins hidden.
         */
        if (pausePanel != null)
        {
            pausePanel.SetActive(false);
        }

        /*
         * Make sure the runtime pause state
         * matches the initial gameplay state.
         */
        isPaused = false;

        /*
         * Automatically find the PlayerHealth component
         * if it was not assigned manually.
         */
        if (playerHealth == null)
        {
            playerHealth = FindFirstObjectByType<PlayerHealth>();
        }

        /*
         * Use the PlayerHealth GameObject as the player Transform
         * if a separate player reference was not assigned.
         */
        if (playerTransform == null &&
            playerHealth != null)
        {
            playerTransform = playerHealth.transform;
        }
    }


    private void Update()
    {
        /*
         * Stop if Unity cannot detect a keyboard.
         */
        if (Keyboard.current == null)
        {
            return;
        }

        /*
         * Escape toggles the pause state.
         */
        if (Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            TogglePause();
        }
    }

    #endregion


    #region Pause Methods

    /*
     * REGION: Pause Methods
     *
     * This region controls pausing and resuming gameplay.
     */

    public void TogglePause()
    {
        if (isPaused)
        {
            ResumeGame();
        }
        else
        {
            PauseGame();
        }
    }


    public void PauseGame()
    {
        /*
         * Mark the game as paused.
         */
        isPaused = true;

        /*
         * Stop time-based gameplay systems.
         */
        Time.timeScale = 0f;

        /*
         * Show the pause menu.
         */
        if (pausePanel != null)
        {
            pausePanel.SetActive(true);
        }

        /*
         * Unlock and show the cursor
         * for pause-menu interaction.
         */
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }


    public void ResumeGame()
    {
        /*
         * Mark the game as active.
         */
        isPaused = false;

        /*
         * Restore normal gameplay speed.
         */
        Time.timeScale = 1f;

        /*
         * Hide the pause menu.
         */
        if (pausePanel != null)
        {
            pausePanel.SetActive(false);
        }

        /*
         * Restore first-person cursor behavior.
         */
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    #endregion


    #region Respawn Methods

    /*
     * REGION: Respawn Methods
     *
     * This region handles returning the player
     * to the currently active checkpoint.
     */

    public void RespawnPlayer()
    {
        /*
         * Make sure normal gameplay time is active.
         */
        Time.timeScale = 1f;

        /*
         * Check whether a valid checkpoint exists.
         *
         * If no checkpoint exists, restart the scene
         * as the fallback behavior.
         */
        if (CheckpointManager.Instance == null ||
            !CheckpointManager.Instance.HasActiveCheckpoint)
        {
            RestartCurrentScene();

            return;
        }

        /*
         * Make sure the required player references exist.
         */
        if (playerTransform == null ||
            playerHealth == null)
        {
            RestartCurrentScene();

            return;
        }

        /*
         * Temporarily disable the CharacterController.
         *
         * Directly changing the Transform while a
         * CharacterController is enabled can cause
         * unwanted collision behavior.
         */
        CharacterController characterController =
            playerTransform.GetComponent<CharacterController>();

        if (characterController != null)
        {
            characterController.enabled = false;
        }

        /*
         * Move the player to the active checkpoint.
         */
        playerTransform.position =
            CheckpointManager.Instance.ActiveRespawnPosition;

        /*
         * Restore the checkpoint's facing direction.
         */
        playerTransform.rotation =
            CheckpointManager.Instance.ActiveRespawnRotation;

        /*
         * Re-enable the CharacterController
         * after repositioning the player.
         */
        if (characterController != null)
        {
            characterController.enabled = true;
        }

        /*
         * Restore player health, controls,
         * camera input, and death UI state.
         */
        playerHealth.Respawn();

        /*
         * Restore first-person cursor behavior.
         */
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    #endregion


    #region Scene Management Methods

    /*
     * REGION: Scene Management Methods
     *
     * This region handles scene-level game actions.
     */

    public void RestartCurrentScene()
    {
        /*
         * Restore normal time before reloading.
         */
        Time.timeScale = 1f;

        /*
         * Get the currently active scene.
         */
        Scene currentScene =
            SceneManager.GetActiveScene();

        /*
         * Reload the currently active scene.
         */
        SceneManager.LoadScene(
            currentScene.buildIndex
        );
    }

    #endregion


    // TODO: Add checkpoint persistence to saved games.
    // TODO: Add scene transition methods.
    // TODO: Add save and load coordination.
    // TODO: Add win and lose state handling.
}