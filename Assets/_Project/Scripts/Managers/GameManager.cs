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
         * If no GameManager instance currently exists,
         * assign this object as the active instance.
         */
        if (Instance == null)
        {
            Instance = this;

            /*
             * Keep this GameObject alive when scenes change.
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
         * Make sure the game begins in an unpaused state.
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
         * Make sure the runtime state matches
         * the game's initial unpaused state.
         */
        isPaused = false;
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
        /*
         * Resume if the game is already paused.
         */
        if (isPaused)
        {
            ResumeGame();
        }
        else
        {
            /*
             * Pause if the game is currently running.
             */
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
         * Stop gameplay time.
         *
         * Most movement, AI, cooldowns, and other systems
         * using Time.deltaTime will stop updating.
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
         * Unlock the mouse cursor so pause-menu
         * buttons can be selected.
         */
        Cursor.lockState = CursorLockMode.None;

        /*
         * Make the cursor visible.
         */
        Cursor.visible = true;
    }


    public void ResumeGame()
    {
        /*
         * Mark the game as active.
         */
        isPaused = false;

        /*
         * Restore normal gameplay time.
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
         * Lock the mouse cursor back to the center
         * for first-person camera control.
         */
        Cursor.lockState = CursorLockMode.Locked;

        /*
         * Hide the cursor during gameplay.
         */
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
         *
         * This prevents the restarted scene from
         * remaining frozen if restart is triggered
         * while the game is paused.
         */
        Time.timeScale = 1f;

        /*
         * Get the scene that is currently active.
         */
        Scene currentScene =
            SceneManager.GetActiveScene();

        /*
         * Reload the active scene using its build index.
         */
        SceneManager.LoadScene(
            currentScene.buildIndex
        );
    }

    #endregion


    // TODO: Add checkpoint support.
    // TODO: Add scene transition methods.
    // TODO: Add save and load coordination.
    // TODO: Add win and lose state handling.
}