using UnityEngine;
using UnityEngine.SceneManagement;

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
             *
             * This allows the same GameManager instance
             * to persist throughout the game.
             */
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            /*
             * A GameManager instance already exists.
             *
             * Destroy this duplicate so only one
             * GameManager controls the game.
             */
            Destroy(gameObject);
        }
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
         * Get the scene that is currently active.
         */
        Scene currentScene =
            SceneManager.GetActiveScene();

        /*
         * Reload the active scene using its build index.
         *
         * Reloading the scene resets scene-based runtime state,
         * including:
         * - Player position
         * - Player health
         * - Enemy positions
         * - Enemy runtime states
         * - Scene-based UI
         */
        SceneManager.LoadScene(
            currentScene.buildIndex
        );
    }

    #endregion


    // TODO: Add pause-state handling.
    // TODO: Add checkpoint support.
    // TODO: Add scene transition methods.
    // TODO: Add save and load coordination.
    // TODO: Add win and lose state handling.
}